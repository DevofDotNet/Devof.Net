using System.ComponentModel.DataAnnotations;
using Blog.Domain.Common;

namespace Blog.Domain.Entities;

public class Comment : BaseEntity
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    public string? RenderedContent { get; set; } // HTML rendered if markdown support
    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false; // Soft delete for nested comments

    // Mentions - stores JSON array of mentioned user IDs
    public string? MentionedUserIds { get; set; }

    // Author
    public string AuthorId { get; set; } = string.Empty;
    public virtual ApplicationUser Author { get; set; } = null!;

    // Post
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    // Self-referencing for nested comments (replies)
    public int? ParentCommentId { get; set; }
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

    // Reports
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
