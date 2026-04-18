using System.IO;

namespace Blog.Application.Services;

public interface IImageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default);
    string GetImageUrl(string fileName);
}

public class LocalImageService : IImageService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;

    // Whitelist of allowed image extensions
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"
    };

    // Whitelist of allowed content types
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"
    };

    // Maximum file size: 10MB
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public LocalImageService(string uploadPath, string baseUrl)
    {
        _uploadPath = uploadPath;
        _baseUrl = baseUrl;

        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Validate file extension
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Invalid file extension. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        // Validate content type
        if (string.IsNullOrEmpty(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException($"Invalid content type. Allowed: {string.Join(", ", AllowedContentTypes)}");
        }

        // Check file size (need to buffer to check length)
        if (!fileStream.CanSeek || fileStream.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum of {MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        // Generate unique filename with original extension preserved
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        return GetImageUrl(uniqueFileName);
    }

    public Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return Task.CompletedTask;

        try
        {
            var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (ArgumentException)
        {
            // Invalid URL, ignore
        }

        return Task.CompletedTask;
    }

    public string GetImageUrl(string fileName)
    {
        // Use relative URL path to work regardless of domain/port
        return $"/uploads/{fileName}";
    }
}