using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface INotificationService
{
    Task CreateMentionNotificationAsync(string mentionedUserId, string content, int? postId, int? commentId, string mentionerName, CancellationToken cancellationToken = default);
    Task CreateReplyNotificationAsync(string userId, string content, int commentId, string replierName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateMentionNotificationAsync(string mentionedUserId, string content, int? postId, int? commentId, string mentionerName, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = mentionedUserId,
            Type = NotificationType.Mention,
            Content = $"{mentionerName} mentioned you: {TruncateContent(content, 100)}",
            RelatedPostId = postId,
            RelatedCommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateReplyNotificationAsync(string userId, string content, int commentId, string replierName, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.Reply,
            Content = $"{replierName} replied to your comment: {TruncateContent(content, 100)}",
            RelatedCommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Notifications.GetByUserIdAsync(userId, unreadOnly, page, pageSize, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Notifications.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Notifications.MarkAsReadAsync(notificationId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content.Substring(0, maxLength) + "...";
    }
}
