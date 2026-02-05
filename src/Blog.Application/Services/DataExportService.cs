using System.Text.Json;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface IDataExportService
{
    Task<string> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default);
}

public class DataExportService : IDataExportService
{
    private readonly IUnitOfWork _unitOfWork;

    public DataExportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var posts = await _unitOfWork.Posts.GetByAuthorIdAsync(userId, 1, 10000, cancellationToken: cancellationToken);
        var bookmarks = await _unitOfWork.Bookmarks.GetByUserIdAsync(userId, 1, 10000, cancellationToken);
        var tagFollows = await _unitOfWork.TagFollows.GetByUserIdAsync(userId, cancellationToken);

        var exportData = new
        {
            User = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.DisplayName,
                user.Bio,
                user.Location,
                user.WebsiteUrl,
                user.CreatedAt
            },
            Posts = posts.Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.Slug,
                p.Status,
                p.PublishedAt,
                p.CreatedAt,
                Tags = p.PostTags.Select(pt => pt.Tag.Name).ToList()
            }).ToList(),
            Bookmarks = bookmarks.Select(b => new
            {
                PostTitle = b.Post.Title,
                PostSlug = b.Post.Slug,
                BookmarkedAt = b.CreatedAt
            }).ToList(),
            FollowedTags = tagFollows.Select(tf => tf.Tag.Name).ToList(),
            ExportedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
