using Blog.Domain.Common;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

public class Post : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Markdown content
    public string? RenderedContent { get; set; } // HTML rendered content
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }

    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // Status & Metrics
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; } = 0;
    public int ReadingTimeMinutes { get; set; } = 1;

    // Trending score (calculated by algorithm)
    public double TrendingScore { get; set; } = 0;
    public DateTime? TrendingScoreUpdatedAt { get; set; }

    // Mentions - stores JSON array of mentioned user IDs
    public string? MentionedUserIds { get; set; }

    // Author
    public string AuthorId { get; set; } = string.Empty;
    public virtual ApplicationUser? Author { get; set; }

    // Navigation Properties
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<PostView> PostViews { get; set; } = new List<PostView>();
}
