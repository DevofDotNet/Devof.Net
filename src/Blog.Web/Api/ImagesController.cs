using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Blog.Web.Api;

[ApiController]
[Route("api/images")]
[AllowAnonymous]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImagesController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpGet("{**objectName}")]
    [OutputCache(Duration = 86400)] // Cache for 24 hours
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
}
