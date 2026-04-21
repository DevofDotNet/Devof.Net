using Blog.Domain.Entities;
using Blog.Domain.Enums;

namespace Blog.Domain.Interfaces;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetAllPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetByAuthorIdAsync(string authorId, int page, int pageSize, PostStatus? status = null, CancellationToken cancellationToken = default);
    Task<int> GetCountByAuthorIdAsync(string authorId, PostStatus? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetByTagAsync(string tagSlug, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByTagAsync(string tagSlug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetRelatedByTagsAsync(int postId, IEnumerable<int> tagIds, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetTrendingAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetSearchCountAsync(string query, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> CountPublishedAsync(CancellationToken cancellationToken = default);
    Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default);
    Task UpdateAsync(Post post, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);
    Task IncrementViewCountAsync(int postId, CancellationToken cancellationToken = default);
}

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetPopularAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetAllWithCountsAsync(CancellationToken cancellationToken = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken = default);
}

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetRepliesAsync(int parentCommentId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ILikeRepository
{
    Task<Like?> GetAsync(string userId, int postId, CancellationToken cancellationToken = default);
    Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, int postId, CancellationToken cancellationToken = default);
    Task<Like> AddAsync(Like like, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, int postId, CancellationToken cancellationToken = default);
}

public interface IBookmarkRepository
{
    Task<Bookmark?> GetAsync(string userId, int postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bookmark>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, int postId, CancellationToken cancellationToken = default);
    Task<Bookmark> AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, int postId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

public interface IFollowRepository
{
    Task<Follow?> GetAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Follow>> GetFollowersAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Follow>> GetFollowingAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowerCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowingCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<Follow> AddAsync(Follow follow, CancellationToken cancellationToken = default);
    Task RemoveAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
}

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);
    Task<Report> AddAsync(Report report, CancellationToken cancellationToken = default);
    Task UpdateAsync(Report report, CancellationToken cancellationToken = default);
}

public interface IPostViewRepository
{
    Task<PostView> AddAsync(PostView postView, CancellationToken cancellationToken = default);
    Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<int> GetUniqueViewCountByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostView>> GetByPostIdAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostView>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> HasViewedAsync(int postId, string? userId, string ipAddress, CancellationToken cancellationToken = default);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ITagFollowRepository
{
    Task<TagFollow?> GetAsync(string userId, int tagId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagFollow>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagFollow>> GetByTagIdAsync(int tagId, CancellationToken cancellationToken = default);
    Task<int> GetFollowerCountAsync(int tagId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(string userId, int tagId, CancellationToken cancellationToken = default);
    Task<TagFollow> AddAsync(TagFollow tagFollow, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, int tagId, CancellationToken cancellationToken = default);
}

public interface ICookieConsentRepository
{
    Task<CookieConsent?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<CookieConsent?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<CookieConsent> AddAsync(CookieConsent consent, CancellationToken cancellationToken = default);
    Task UpdateAsync(CookieConsent consent, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<ApplicationUser>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    IPostRepository Posts { get; }
    ITagRepository Tags { get; }
    ICommentRepository Comments { get; }
    ILikeRepository Likes { get; }
    IBookmarkRepository Bookmarks { get; }
    IFollowRepository Follows { get; }
    IFollowRepository Followers { get; } // Alias for Follows
    IReportRepository Reports { get; }
    IUserRepository Users { get; }
    IPostViewRepository PostViews { get; }
    INotificationRepository Notifications { get; }
    ITagFollowRepository TagFollows { get; }
    ICookieConsentRepository CookieConsents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

