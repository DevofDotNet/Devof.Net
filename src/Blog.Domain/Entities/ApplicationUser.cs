using Microsoft.AspNetCore.Identity;

namespace Blog.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    // Profile Information
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Location { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; } = false;
    public string? BanReason { get; set; }
    
    // Navigation Properties
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    
    // Followers (users following this user)
    public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
    // Following (users this user follows)
    public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
    
    public virtual ICollection<Report> ReportsSubmitted { get; set; } = new List<Report>();
    public virtual ICollection<Report> ReportsReceived { get; set; } = new List<Report>();
}
