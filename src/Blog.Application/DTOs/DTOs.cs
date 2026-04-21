namespace Blog.Application.DTOs;

// User DTOs
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Location { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFollowing { get; set; }
}

public class UserProfileUpdateDto
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Location { get; set; }
}

// Post DTOs
public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ViewCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int BookmarkCount { get; set; }
    public UserDto? Author { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public bool IsLiked { get; set; }
    public bool IsBookmarked { get; set; }
}

public class PostDetailDto : PostDto
{
    public string Content { get; set; } = string.Empty;
    public string RenderedContent { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
}

public class CreatePostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool Publish { get; set; } = false;
}

public class UpdatePostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool Publish { get; set; } = false;
}

// Tag DTOs
public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int PostCount { get; set; }
}

// Comment DTOs
public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? RenderedContent { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto? Author { get; set; }
    public int? ParentCommentId { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}

public class CreateCommentDto
{
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}

// Pagination
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// Report DTOs
public class CreateReportDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
    public string? UserId { get; set; }
}

public class ReportDto
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public UserDto Reporter { get; set; } = null!;
    public PostDto? ReportedPost { get; set; }
    public CommentDto? ReportedComment { get; set; }
    public UserDto? ReportedUser { get; set; }
}

// Search
public class SearchResultDto
{
    public List<PostDto> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public string Query { get; set; } = string.Empty;
}
