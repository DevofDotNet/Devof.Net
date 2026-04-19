using Blog.Application.Services;
using Oci.Common.Auth;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Requests;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text;

namespace Blog.Infrastructure.Services;

public class OciStorageOptions
{
    public string Namespace { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string TenancyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string? Passphrase { get; set; }
}

public class OciObjectStorageImageService : IImageService, IDisposable
{
    private readonly OciStorageOptions _options;
    private readonly ILogger<OciObjectStorageImageService> _logger;
    private readonly string _tempKeyFile;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public OciObjectStorageImageService(OciStorageOptions options, ILogger<OciObjectStorageImageService> logger)
    {
        _options = options;
        _logger = logger;

        // Write key to a persistent temp file (deleted on Dispose)
        _tempKeyFile = Path.Combine(Path.GetTempPath(), $"oci_key_{Guid.NewGuid()}.pem");
        File.WriteAllText(_tempKeyFile, _options.PrivateKey, Encoding.UTF8);
    }

    public void Dispose()
    {
        try { File.Delete(_tempKeyFile); } catch { }
    }

    private ObjectStorageClient CreateClient()
    {
        var passphrase = new SecureString();
        foreach (var c in _options.Passphrase ?? "")
            passphrase.AppendChar(c);
        passphrase.MakeReadOnly();

        var provider = new SimpleAuthenticationDetailsProvider
        {
            TenantId = _options.TenancyId,
            UserId = _options.UserId,
            Fingerprint = _options.Fingerprint,
            Region = Oci.Common.Region.FromRegionId(_options.Region),
            PrivateKeySupplier = new FilePrivateKeySupplier(_tempKeyFile, passphrase)
        };

        return new ObjectStorageClient(provider);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new ArgumentException($"Invalid file extension. Allowed: {string.Join(", ", AllowedExtensions)}");

        if (string.IsNullOrEmpty(contentType) || !AllowedContentTypes.Contains(contentType))
            throw new ArgumentException($"Invalid content type. Allowed: {string.Join(", ", AllowedContentTypes)}");

        if (fileStream.CanSeek && fileStream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds maximum of {MaxFileSizeBytes / (1024 * 1024)}MB");

        var objectName = $"uploads/{Guid.NewGuid()}{extension}";

        using var client = CreateClient();
        var request = new PutObjectRequest
        {
            NamespaceName = _options.Namespace,
            BucketName = _options.BucketName,
            ObjectName = objectName,
            PutObjectBody = fileStream,
            ContentType = contentType
        };

        await client.PutObject(request);
        _logger.LogInformation("Uploaded image to OCI: {ObjectName}", objectName);

        return GetImageUrl(objectName);
    }

    public async Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return;

        try
        {
            var objectName = ExtractObjectName(imageUrl);
            if (string.IsNullOrEmpty(objectName))
                return;

            using var client = CreateClient();
            var request = new DeleteObjectRequest
            {
                NamespaceName = _options.Namespace,
                BucketName = _options.BucketName,
                ObjectName = objectName
            };

            await client.DeleteObject(request);
            _logger.LogInformation("Deleted image from OCI: {ObjectName}", objectName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image from OCI: {Url}", imageUrl);
        }
    }

    public async Task<(Stream Stream, string ContentType)?> GetStreamAsync(string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateClient();
            var request = new GetObjectRequest
            {
                NamespaceName = _options.Namespace,
                BucketName = _options.BucketName,
                ObjectName = objectName
            };

            var response = await client.GetObject(request);
            var ms = new MemoryStream();
            await response.InputStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            return (ms, response.ContentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get image from OCI: {ObjectName}", objectName);
            return null;
        }
    }

    public string GetImageUrl(string objectName)
    {
        // Return local proxy URL so images work with private buckets
        return $"/api/images/{objectName}";
    }

    private string? ExtractObjectName(string imageUrl)
    {
        // Handle proxy URLs: /api/images/{objectName}
        const string proxyMarker = "/api/images/";
        var proxyIdx = imageUrl.IndexOf(proxyMarker, StringComparison.OrdinalIgnoreCase);
        if (proxyIdx >= 0)
            return Uri.UnescapeDataString(imageUrl[(proxyIdx + proxyMarker.Length)..]);

        // Handle legacy direct OCI URLs
        var marker = $"/b/{_options.BucketName}/o/";
        var idx = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var encoded = imageUrl[(idx + marker.Length)..];
        return Uri.UnescapeDataString(encoded);
    }
}
