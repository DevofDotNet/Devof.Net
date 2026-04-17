using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface IAnalyticsService
{
    Task TrackPostViewAsync(int postId, string? userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthorStatsDto> GetAuthorStatsAsync(string authorId, CancellationToken cancellationToken = default);
    Task<PostAnalyticsDto> GetPostAnalyticsAsync(int postId, CancellationToken cancellationToken = default);
    Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetTopPostsByViewsAsync(int count = 10, CancellationToken cancellationToken = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task TrackPostViewAsync(int postId, string? userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        // Only track once per user per post (or once per IP for anonymous)
        var hasViewed = await _unitOfWork.PostViews.HasViewedAsync(postId, userId, ipAddress, cancellationToken);
        if (hasViewed)
        {
            return; // Already tracked this view
        }

        var postView = new PostView
        {
            PostId = postId,
            ViewerId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ViewedAt = DateTime.UtcNow
        };

        await _unitOfWork.PostViews.AddAsync(postView, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Also increment the post's ViewCount for display/analytics
        await _unitOfWork.Posts.IncrementViewCountAsync(postId, cancellationToken);
    }

    public async Task<AuthorStatsDto> GetAuthorStatsAsync(string authorId, CancellationToken cancellationToken = default)
    {
        var totalPosts = await _unitOfWork.Posts.GetCountByAuthorIdAsync(authorId, cancellationToken: cancellationToken);
        var publishedPosts = await _unitOfWork.Posts.GetCountByAuthorIdAsync(authorId, Domain.Enums.PostStatus.Published, cancellationToken);
        var posts = await _unitOfWork.Posts.GetByAuthorIdAsync(authorId, 1, 1000, cancellationToken: cancellationToken);

        var totalViews = posts.Sum(p => p.ViewCount);
        var totalLikes = posts.Sum(p => p.Likes.Count);
        var followerCount = await _unitOfWork.Follows.GetFollowerCountAsync(authorId, cancellationToken);

        return new AuthorStatsDto
        {
            TotalPosts = totalPosts,
            PublishedPosts = publishedPosts,
            TotalViews = totalViews,
            TotalLikes = totalLikes,
            TotalFollowers = followerCount
        };
    }

    public async Task<PostAnalyticsDto> GetPostAnalyticsAsync(int postId, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new InvalidOperationException($"Post with ID {postId} not found");
        }

        var viewCount = await _unitOfWork.PostViews.GetCountByPostIdAsync(postId, cancellationToken);
        var uniqueViews = await _unitOfWork.PostViews.GetUniqueViewCountByPostIdAsync(postId, cancellationToken);
        var likeCount = await _unitOfWork.Likes.GetCountByPostIdAsync(postId, cancellationToken);

        var engagementRate = viewCount > 0 ? (double)(likeCount + post.Comments.Count) / viewCount * 100 : 0;

        return new PostAnalyticsDto
        {
            PostId = postId,
            Title = post.Title,
            ViewCount = viewCount,
            UniqueViews = uniqueViews,
            LikeCount = likeCount,
            CommentCount = post.Comments.Count,
            EngagementRate = engagementRate,
            PublishedAt = post.PublishedAt
        };
    }

    public async Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _unitOfWork.Users.CountAsync(cancellationToken);
        var totalPosts = await _unitOfWork.Posts.GetTotalCountAsync(cancellationToken);
        var totalComments = await _unitOfWork.Comments.CountAsync(cancellationToken);
        var pendingReports = await _unitOfWork.Reports.GetPendingCountAsync(cancellationToken);

        var popularTags = await _unitOfWork.Tags.GetPopularAsync(10, cancellationToken);

        return new AdminDashboardStatsDto
        {
            TotalUsers = totalUsers,
            TotalPosts = totalPosts,
            TotalComments = totalComments,
            PendingReports = pendingReports,
            PopularTags = popularTags.Select(t => new TagStatsDto
            {
                TagName = t.Name,
                PostCount = t.PostTags.Count
            }).ToList()
        };
    }

    public async Task<IEnumerable<Post>> GetTopPostsByViewsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var allPublishedPosts = await _unitOfWork.Posts.GetAllPublishedAsync(1, 1000, cancellationToken);
        return allPublishedPosts.OrderByDescending(p => p.ViewCount).Take(count);
    }
}

// DTOs
public class AuthorStatsDto
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int TotalViews { get; set; }
    public int TotalLikes { get; set; }
    public int TotalFollowers { get; set; }
}

public class PostAnalyticsDto
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int UniqueViews { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public double EngagementRate { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int PendingReports { get; set; }
    public List<TagStatsDto> PopularTags { get; set; } = new();
}

public class TagStatsDto
{
    public string TagName { get; set; } = string.Empty;
    public int PostCount { get; set; }
}
