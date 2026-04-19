using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Blog.Web.Api;

[ApiController]
[AllowAnonymous]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImagesController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpGet("api/images/{**objectName}")]
    [OutputCache(Duration = 86400)]
    public async Task<IActionResult> Get(string objectName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(objectName))
            return NotFound();

        var result = await _imageService.GetStreamAsync(objectName, cancellationToken);
        if (result is null)
            return NotFound();

        var (stream, contentType) = result.Value;
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(stream, contentType);
    }

    /// <summary>
    /// Fallback for old /uploads/ URLs stored in the database before OCI migration.
    /// Static files middleware serves the file if it exists on disk; this catches the rest.
    /// </summary>
    [HttpGet("uploads/{**fileName}")]
    [OutputCache(Duration = 86400)]
    public async Task<IActionResult> LegacyUploads(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(fileName))
            return NotFound();

        // Old URLs: /uploads/guid.jpg → OCI object: uploads/guid.jpg
        var objectName = $"uploads/{fileName}";
        var result = await _imageService.GetStreamAsync(objectName, cancellationToken);
        if (result is null)
            return NotFound();

        var (stream, contentType) = result.Value;
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(stream, contentType);
    }
}
