using Blog.Domain.Common;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

/// <summary>
/// Tracks user cookie consent for GDPR compliance
/// </summary>
public class CookieConsent : BaseEntity
{
    public string? UserId { get; set; } // Nullable for anonymous users
    public virtual ApplicationUser? User { get; set; }

    public ConsentType ConsentType { get; set; }
    public bool HasConsented { get; set; }

    public string IpAddress { get; set; } = string.Empty;
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
}
