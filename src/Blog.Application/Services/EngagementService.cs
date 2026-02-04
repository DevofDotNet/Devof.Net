using Blog.Application.DTOs;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface IEngagementService
{
    Task<bool> LikePostAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UnlikePostAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<bool> BookmarkPostAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UnbookmarkPostAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> GetBookmarkedPostsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> FollowAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
}

public class EngagementService : IEngagementService
{
    private readonly IUnitOfWork _unitOfWork;

    public EngagementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> LikePostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Likes.ExistsAsync(userId, postId, cancellationToken))
            return false;

        var like = new Like
        {
            UserId = userId,
            PostId = postId
        };

        await _unitOfWork.Likes.AddAsync(like, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlikePostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Likes.ExistsAsync(userId, postId, cancellationToken))
            return false;

        await _unitOfWork.Likes.RemoveAsync(userId, postId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> BookmarkPostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Bookmarks.ExistsAsync(userId, postId, cancellationToken))
            return false;

        var bookmark = new Bookmark
        {
            UserId = userId,
            PostId = postId
        };

        await _unitOfWork.Bookmarks.AddAsync(bookmark, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnbookmarkPostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Bookmarks.ExistsAsync(userId, postId, cancellationToken))
            return false;

        await _unitOfWork.Bookmarks.RemoveAsync(userId, postId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<PostDto>> GetBookmarkedPostsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var bookmarks = await _unitOfWork.Bookmarks.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        
        return new PagedResult<PostDto>
        {
            Items = bookmarks.Select(b => MapPostToDto(b.Post, userId)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = bookmarks.Count()
        };
    }

    public async Task<bool> FollowAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        if (followerId == followingId)
            return false; // Can't follow yourself

        if (await _unitOfWork.Follows.IsFollowingAsync(followerId, followingId, cancellationToken))
            return false;

        var follow = new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId
        };

        await _unitOfWork.Follows.AddAsync(follow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnfollowAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Follows.IsFollowingAsync(followerId, followingId, cancellationToken))
            return false;

        await _unitOfWork.Follows.RemoveAsync(followerId, followingId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Follows.IsFollowingAsync(followerId, followingId, cancellationToken);
    }

    private static PostDto MapPostToDto(Post post, string userId)
    {
        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.Excerpt,
            CoverImageUrl = post.CoverImageUrl,
            Status = post.Status.ToString(),
            PublishedAt = post.PublishedAt,
            CreatedAt = post.CreatedAt,
            ViewCount = post.ViewCount,
            ReadingTimeMinutes = post.ReadingTimeMinutes,
            LikeCount = post.Likes?.Count ?? 0,
            CommentCount = post.Comments?.Count ?? 0,
            BookmarkCount = post.Bookmarks?.Count ?? 0,
            IsBookmarked = true, // Since we're getting bookmarked posts
            Author = new UserDto
            {
                Id = post.Author.Id,
                UserName = post.Author.UserName ?? string.Empty,
                DisplayName = post.Author.DisplayName,
                AvatarUrl = post.Author.AvatarUrl
            },
            Tags = post.PostTags?.Select(pt => new TagDto
            {
                Id = pt.Tag.Id,
                Name = pt.Tag.Name,
                Slug = pt.Tag.Slug,
                Color = pt.Tag.Color
            }).ToList() ?? new List<TagDto>()
        };
    }
}
