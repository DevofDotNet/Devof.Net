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
        // Generate unique filename
        var extension = Path.GetExtension(fileName);
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

        var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
        var filePath = Path.Combine(_uploadPath, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public string GetImageUrl(string fileName)
    {
        // Use relative URL path to work regardless of domain/port
        return $"/uploads/{fileName}";
    }
}
