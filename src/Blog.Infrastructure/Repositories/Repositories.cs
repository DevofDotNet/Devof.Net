using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _context;

    public PostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .Include(p => p.Bookmarks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return null;
        }

        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .Include(p => p.Bookmarks)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetAllPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetByAuthorIdAsync(string authorId, int page, int pageSize, PostStatus? status = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(authorId))
        {
            return Enumerable.Empty<Post>();
        }

        var baseQuery = _context.Posts.Where(p => p.AuthorId == authorId);

        if (status.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.Status == status.Value);
        }

        return await baseQuery
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByAuthorIdAsync(string authorId, PostStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Posts.Where(p => p.AuthorId == authorId);
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }
        return await query.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetByTagAsync(string tagSlug, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tagSlug))
        {
            return Enumerable.Empty<Post>();
        }

        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published && p.PostTags.Any(pt => pt.Tag.Slug == tagSlug))
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTagAsync(string tagSlug, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published && p.PostTags.Any(pt => pt.Tag.Slug == tagSlug))
            .CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetRelatedByTagsAsync(int postId, IEnumerable<int> tagIds, int count, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published && p.Id != postId && p.PostTags.Any(pt => tagIds.Contains(pt.TagId)))
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetTrendingAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.TrendingScore)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Post>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var searchQuery = $"%{query}%";
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published &&
                       (EF.Functions.Like(p.Title, searchQuery) ||
                        EF.Functions.Like(p.Content, searchQuery)))
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetSearchCountAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchQuery = $"%{query}%";
        return await _context.Posts
            .Where(p => p.Status == PostStatus.Published &&
                       (EF.Functions.Like(p.Title, searchQuery) ||
                        EF.Functions.Like(p.Content, searchQuery)))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Posts.Where(p => p.Status == PostStatus.Published).CountAsync(cancellationToken);
    }

    public async Task<int> CountPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Posts.Where(p => p.Status == PostStatus.Published).CountAsync(cancellationToken);
    }

    public async Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _context.Posts.AddAsync(post, cancellationToken);
        return post;
    }

    public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        _context.Posts.Update(post);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts.FindAsync(new object[] { id }, cancellationToken);
        if (post != null)
        {
            _context.Posts.Remove(post);
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .AnyAsync(p => p.Slug == slug && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken);
    }

    public async Task IncrementViewCountAsync(int postId, CancellationToken cancellationToken = default)
    {
        await _context.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);
    }

    public async Task<int> GetTotalViewsByAuthorAsync(string authorId, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.AuthorId == authorId)
            .SumAsync(p => p.ViewCount, cancellationToken);
    }

    public async Task<int> GetTotalLikesByAuthorAsync(string authorId, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Where(p => p.AuthorId == authorId)
            .SelectMany(p => p.Likes)
            .CountAsync(cancellationToken);
    }
}

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Tags.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return null;
        }

        return await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetPopularAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .OrderByDescending(t => t.PostTags.Count)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetAllWithCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Include(t => t.PostTags)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
        return tag;
    }

    public Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        _context.Tags.Update(tag);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags.FindAsync(new object[] { id }, cancellationToken);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
        }
    }

    public async Task<IEnumerable<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken = default)
    {
        var result = new List<Tag>();
        foreach (var name in tagNames)
        {
            var slug = GenerateSlug(name);
            var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
            if (existingTag != null)
            {
                result.Add(existingTag);
            }
            else
            {
                var newTag = new Tag { Name = name.Trim(), Slug = slug };
                await _context.Tags.AddAsync(newTag, cancellationToken);
                result.Add(newTag);
            }
        }
        return result;
    }

    private static string GenerateSlug(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');
    }
}

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => !r.IsDeleted)).ThenInclude(r => r.Author)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => !r.IsDeleted)).ThenInclude(r => r.Author)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .CountAsync(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetRepliesAsync(int parentCommentId, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Comments.CountAsync(c => !c.IsDeleted, cancellationToken);
    }

    public async Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
        return comment;
    }

    public Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        comment.IsEdited = true;
        _context.Comments.Update(comment);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments.FindAsync(new object[] { id }, cancellationToken);
        if (comment != null)
        {
            comment.IsDeleted = true;
            comment.Content = "[deleted]";
            comment.RenderedContent = "[deleted]";
        }
    }
}

public class LikeRepository : ILikeRepository
{
    private readonly ApplicationDbContext _context;

    public LikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Like?> GetAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId, cancellationToken);
    }

    public async Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Likes.CountAsync(l => l.PostId == postId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Likes.AnyAsync(l => l.UserId == userId && l.PostId == postId, cancellationToken);
    }

    public async Task<Like> AddAsync(Like like, CancellationToken cancellationToken = default)
    {
        await _context.Likes.AddAsync(like, cancellationToken);
        return like;
    }

    public async Task RemoveAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        var like = await GetAsync(userId, postId, cancellationToken);
        if (like != null)
        {
            _context.Likes.Remove(like);
        }
    }
}

public class BookmarkRepository : IBookmarkRepository
{
    private readonly ApplicationDbContext _context;

    public BookmarkRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Bookmark?> GetAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId, cancellationToken);
    }

    public async Task<IEnumerable<Bookmark>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Bookmarks
            .Where(b => b.UserId == userId)
            .Include(b => b.Post).ThenInclude(p => p.Author)
            .Include(b => b.Post).ThenInclude(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookmarks.AnyAsync(b => b.UserId == userId && b.PostId == postId, cancellationToken);
    }

    public async Task<Bookmark> AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        await _context.Bookmarks.AddAsync(bookmark, cancellationToken);
        return bookmark;
    }

    public async Task RemoveAsync(string userId, int postId, CancellationToken cancellationToken = default)
    {
        var bookmark = await GetAsync(userId, postId, cancellationToken);
        if (bookmark != null)
        {
            _context.Bookmarks.Remove(bookmark);
        }
    }

    public async Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Bookmarks.CountAsync(b => b.UserId == userId, cancellationToken);
    }
}

public class FollowRepository : IFollowRepository
{
    private readonly ApplicationDbContext _context;

    public FollowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Follow?> GetAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, cancellationToken);
    }

    public async Task<IEnumerable<Follow>> GetFollowersAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .Where(f => f.FollowingId == userId)
            .Include(f => f.Follower)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Follow>> GetFollowingAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Following)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetFollowerCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows.CountAsync(f => f.FollowingId == userId, cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows.CountAsync(f => f.FollowerId == userId, cancellationToken);
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        return await _context.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, cancellationToken);
    }

    public async Task<Follow> AddAsync(Follow follow, CancellationToken cancellationToken = default)
    {
        await _context.Follows.AddAsync(follow, cancellationToken);
        return follow;
    }

    public async Task RemoveAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        var follow = await GetAsync(followerId, followingId, cancellationToken);
        if (follow != null)
        {
            _context.Follows.Remove(follow);
        }
    }
}

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReportedPost)
            .Include(r => r.ReportedComment)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Report>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Reports
            .Where(r => r.Status == ReportStatus.Pending)
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReportedPost)
            .Include(r => r.ReportedComment)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken);
    }

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken);
    }

    public async Task<Report> AddAsync(Report report, CancellationToken cancellationToken = default)
    {
        await _context.Reports.AddAsync(report, cancellationToken);
        return report;
    }

    public Task UpdateAsync(Report report, CancellationToken cancellationToken = default)
    {
        _context.Reports.Update(report);
        return Task.CompletedTask;
    }
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);
    }

    public async Task<IEnumerable<ApplicationUser>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(cancellationToken);
    }
}

public class PostViewRepository : IPostViewRepository
{
    private readonly ApplicationDbContext _context;

    public PostViewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostView> AddAsync(PostView postView, CancellationToken cancellationToken = default)
    {
        await _context.PostViews.AddAsync(postView, cancellationToken);
        return postView;
    }

    public async Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.PostViews.CountAsync(pv => pv.PostId == postId, cancellationToken);
    }

    public async Task<int> GetUniqueViewCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        // Count unique viewers (both authenticated and by IP for anonymous)
        var authenticatedViews = await _context.PostViews
            .Where(pv => pv.PostId == postId && pv.ViewerId != null)
            .Select(pv => pv.ViewerId)
            .Distinct()
            .CountAsync(cancellationToken);

        var anonymousViews = await _context.PostViews
            .Where(pv => pv.PostId == postId && pv.ViewerId == null)
            .Select(pv => pv.IpAddress)
            .Distinct()
            .CountAsync(cancellationToken);

        return authenticatedViews + anonymousViews;
    }

    public async Task<IEnumerable<PostView>> GetByPostIdAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.PostViews
            .Where(pv => pv.PostId == postId)
            .Include(pv => pv.Viewer)
            .Include(pv => pv.Post)
            .OrderByDescending(pv => pv.ViewedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PostView>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.PostViews
            .Where(pv => pv.ViewerId == userId)
            .Include(pv => pv.Post).ThenInclude(p => p.Author)
            .OrderByDescending(pv => pv.ViewedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasViewedAsync(int postId, string? userId, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            return await _context.PostViews.AnyAsync(pv => pv.PostId == postId && pv.ViewerId == userId, cancellationToken);
        }

        return await _context.PostViews.AnyAsync(pv => pv.PostId == postId && pv.IpAddress == ipAddress, cancellationToken);
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Notifications
            .Include(n => n.User)
            .Include(n => n.RelatedPost)
            .Include(n => n.RelatedComment)
            .Include(n => n.RelatedUser)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .Include(n => n.RelatedPost)
            .Include(n => n.RelatedComment)
            .Include(n => n.RelatedUser)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        return notification;
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { notificationId }, cancellationToken);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
        }
    }
}

public class TagFollowRepository : ITagFollowRepository
{
    private readonly ApplicationDbContext _context;

    public TagFollowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TagFollow?> GetAsync(string userId, int tagId, CancellationToken cancellationToken = default)
    {
        return await _context.TagFollows
            .FirstOrDefaultAsync(tf => tf.UserId == userId && tf.TagId == tagId, cancellationToken);
    }

    public async Task<IEnumerable<TagFollow>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.TagFollows
            .Where(tf => tf.UserId == userId)
            .Include(tf => tf.Tag)
            .OrderByDescending(tf => tf.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TagFollow>> GetByTagIdAsync(int tagId, CancellationToken cancellationToken = default)
    {
        return await _context.TagFollows
            .Where(tf => tf.TagId == tagId)
            .Include(tf => tf.User)
            .OrderByDescending(tf => tf.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetFollowerCountAsync(int tagId, CancellationToken cancellationToken = default)
    {
        return await _context.TagFollows.CountAsync(tf => tf.TagId == tagId, cancellationToken);
    }

    public async Task<bool> IsFollowingAsync(string userId, int tagId, CancellationToken cancellationToken = default)
    {
        return await _context.TagFollows.AnyAsync(tf => tf.UserId == userId && tf.TagId == tagId, cancellationToken);
    }

    public async Task<TagFollow> AddAsync(TagFollow tagFollow, CancellationToken cancellationToken = default)
    {
        await _context.TagFollows.AddAsync(tagFollow, cancellationToken);
        return tagFollow;
    }

    public async Task RemoveAsync(string userId, int tagId, CancellationToken cancellationToken = default)
    {
        var tagFollow = await GetAsync(userId, tagId, cancellationToken);
        if (tagFollow != null)
        {
            _context.TagFollows.Remove(tagFollow);
        }
    }
}

public class CookieConsentRepository : ICookieConsentRepository
{
    private readonly ApplicationDbContext _context;

    public CookieConsentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CookieConsent?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.CookieConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CookieConsent?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return await _context.CookieConsents
            .Where(c => c.IpAddress == ipAddress && c.UserId == null)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CookieConsent> AddAsync(CookieConsent consent, CancellationToken cancellationToken = default)
    {
        await _context.CookieConsents.AddAsync(consent, cancellationToken);
        return consent;
    }

    public Task UpdateAsync(CookieConsent consent, CancellationToken cancellationToken = default)
    {
        _context.CookieConsents.Update(consent);
        return Task.CompletedTask;
    }
}
