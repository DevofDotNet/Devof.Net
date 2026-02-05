using Blog.Domain.Common;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

/// <summary>
/// Represents user notifications for mentions, replies, follows, etc.
/// </summary>
public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Content { get; set; } = string.Empty;

    // Related entity - can be Post, Comment, or User
    public string? RelatedEntityType { get; set; } // "Post", "Comment", "User"
    public string? RelatedEntityId { get; set; }

    // Optional: Direct link to specific entities
    public int? RelatedPostId { get; set; }
    public virtual Post? RelatedPost { get; set; }

    public int? RelatedCommentId { get; set; }
    public virtual Comment? RelatedComment { get; set; }

    public string? RelatedUserId { get; set; }
    public virtual ApplicationUser? RelatedUser { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
