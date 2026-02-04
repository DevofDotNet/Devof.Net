using Blog.Domain.Common;

namespace Blog.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? Color { get; set; } // Hex color for tag display
    
    // Navigation Properties
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}

public class PostTag
{
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
    
    public int TagId { get; set; }
    public virtual Tag Tag { get; set; } = null!;
}
