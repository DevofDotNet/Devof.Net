namespace Blog.Domain.Entities;

public class Like
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
}

public class Bookmark
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
}

public class Follow
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // The user who is following
    public string FollowerId { get; set; } = string.Empty;
    public virtual ApplicationUser Follower { get; set; } = null!;
    
    // The user being followed
    public string FollowingId { get; set; } = string.Empty;
    public virtual ApplicationUser Following { get; set; } = null!;
}
