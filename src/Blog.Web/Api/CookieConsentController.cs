using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Blog.Web.Api;

[ApiController]
[Route("api/cookie-consent")]
public class CookieConsentController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CookieConsentController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        CookieConsent? consent = null;

        if (!string.IsNullOrEmpty(userId))
        {
            consent = await _unitOfWork.CookieConsents.GetByUserIdAsync(userId);
        }
        else
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            if (!string.IsNullOrEmpty(ip))
                consent = await _unitOfWork.CookieConsents.GetByIpAddressAsync(ip);
        }

        return Ok(new { hasConsented = consent?.HasConsented ?? false });
    }

    [HttpPost("accept")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept([FromForm] string consentType = "Essential")
    {
        if (!Enum.TryParse<ConsentType>(consentType, true, out var type))
            type = ConsentType.Essential;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

        CookieConsent? existing = null;

        if (!string.IsNullOrEmpty(userId))
            existing = await _unitOfWork.CookieConsents.GetByUserIdAsync(userId);
        else if (!string.IsNullOrEmpty(ip))
            existing = await _unitOfWork.CookieConsents.GetByIpAddressAsync(ip);

        if (existing != null)
        {
            existing.HasConsented = true;
            existing.ConsentType = type;
            existing.ConsentedAt = DateTime.UtcNow;
            await _unitOfWork.CookieConsents.UpdateAsync(existing);
        }
        else
        {
            var consent = new CookieConsent
            {
                UserId = string.IsNullOrEmpty(userId) ? null : userId,
                ConsentType = type,
                HasConsented = true,
                IpAddress = ip,
                ConsentedAt = DateTime.UtcNow
            };
            await _unitOfWork.CookieConsents.AddAsync(consent);
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
