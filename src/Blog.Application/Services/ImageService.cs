using System.IO;

namespace Blog.Application.Services;

public interface IImageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default);
    string GetImageUrl(string fileName);
    Task<(Stream Stream, string ContentType)?> GetStreamAsync(string objectName, CancellationToken cancellationToken = default);
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
        // Support subdirectory prefixes (e.g., "avatars/filename.jpg")
        var subDir = Path.GetDirectoryName(fileName) ?? "";
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var targetDir = Path.Combine(_uploadPath, subDir);
        
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var filePath = Path.Combine(targetDir, uniqueFileName);

        using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        var relativePath = string.IsNullOrEmpty(subDir) ? uniqueFileName : $"{subDir}/{uniqueFileName}";
        return GetImageUrl(relativePath);
    }

    public Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return Task.CompletedTask;

        try
        {
            // Parse the URL to get the full relative path including subdirectory
            // Handle both absolute URLs and relative paths (e.g., "/uploads/avatars/filename.jpg")
            string fileName;
            
            if (imageUrl.StartsWith("/"))
            {
                // Relative path like "/uploads/avatars/filename.jpg" - keep subdirectory prefix
                var relativePath = imageUrl.TrimStart('/'); // "uploads/avatars/filename.jpg"
                fileName = relativePath.StartsWith("uploads/")
                    ? relativePath.Substring("uploads/".Length)  // "avatars/filename.jpg" or "filename.jpg"
                    : Path.GetFileName(relativePath);
            }
            else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            {
                // Absolute URL: extract path after /uploads/
                var relativePath = absoluteUri.AbsolutePath; // e.g., "/uploads/avatars/guid.jpg"
                fileName = relativePath.StartsWith("/uploads/") 
                    ? relativePath.Substring("/uploads/".Length)  // "avatars/guid.jpg"
                    : Path.GetFileName(absoluteUri.LocalPath);
            }
            else
            {
                // Plain filename or invalid - use as-is
                fileName = Path.GetFileName(imageUrl);
            }

            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                
                // Clean up empty parent directories after deleting file
                var dirPath = Path.GetDirectoryName(filePath);
                while (dirPath != null && dirPath.Length > _uploadPath.Length && Directory.Exists(dirPath))
                {
                    if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                    {
                        Directory.Delete(dirPath);
                        dirPath = Path.GetDirectoryName(dirPath);
                    }
                    else
                    {
                        break;
                    }
                }
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

    public Task<(Stream Stream, string ContentType)?> GetStreamAsync(string objectName, CancellationToken cancellationToken = default)
    {
        // Local files are served directly by static files middleware; this is a no-op
        return Task.FromResult<(Stream Stream, string ContentType)?>(null);
    }
}