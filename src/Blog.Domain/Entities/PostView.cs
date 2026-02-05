using Blog.Domain.Common;

namespace Blog.Domain.Entities;

/// <summary>
/// Tracks individual post views for analytics purposes
/// </summary>
public class PostView : BaseEntity
{
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    // Nullable for anonymous viewers
    public string? ViewerId { get; set; }
    public virtual ApplicationUser? Viewer { get; set; }

    // Track IP and User Agent for analytics
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
