using System.Text.RegularExpressions;
using Blog.Application.DTOs;
using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface IPostService
{
    Task<PostDetailDto?> GetByIdAsync(int id, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PostDetailDto?> GetBySlugAsync(string slug, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> GetLatestAsync(int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<List<PostDto>> GetTrendingAsync(int count, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> GetByTagAsync(string tagSlug, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> SearchAsync(string query, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<List<PostDto>> GetRelatedAsync(int postId, int count, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> GetByAuthorAsync(string authorId, int page, int pageSize, PostStatus? status = null, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PostDto>> GetBookmarkedPostsAsync(int page, int pageSize, string userId, CancellationToken cancellationToken = default);
    Task<PostDetailDto> CreateAsync(CreatePostDto dto, string authorId, CancellationToken cancellationToken = default);
    Task<PostDetailDto> UpdateAsync(UpdatePostDto dto, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task PublishAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task UnpublishAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task IncrementViewAsync(int postId, CancellationToken cancellationToken = default);
    Task UpdateTrendingScoresAsync(CancellationToken cancellationToken = default);
}

public class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMarkdownService _markdownService;

    public PostService(IUnitOfWork unitOfWork, IMarkdownService markdownService)
    {
        _unitOfWork = unitOfWork;
        _markdownService = markdownService;
    }

    public async Task<PostDetailDto?> GetByIdAsync(int id, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(id, cancellationToken);
        return post == null ? null : await MapToDetailDtoAsync(post, currentUserId, cancellationToken);
    }

    public async Task<PostDetailDto?> GetBySlugAsync(string slug, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetBySlugAsync(slug, cancellationToken);
        return post == null ? null : await MapToDetailDtoAsync(post, currentUserId, cancellationToken);
    }

    public async Task<PagedResult<PostDto>> GetLatestAsync(int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _unitOfWork.Posts.GetAllPublishedAsync(page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Posts.GetTotalCountAsync(cancellationToken);

        var items = new List<PostDto>();
        foreach (var post in posts)
        {
            items.Add(await MapToDtoAsync(post, currentUserId, cancellationToken));
        }

        return new PagedResult<PostDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<PostDto>> GetTrendingAsync(int count, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _unitOfWork.Posts.GetTrendingAsync(count, cancellationToken);
        var result = new List<PostDto>();
        foreach (var post in posts)
        {
            result.Add(await MapToDtoAsync(post, currentUserId, cancellationToken));
        }
        return result;
    }

    public async Task<PagedResult<PostDto>> GetByTagAsync(string tagSlug, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _unitOfWork.Posts.GetByTagAsync(tagSlug, page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Posts.GetCountByTagAsync(tagSlug, cancellationToken);
        var items = new List<PostDto>();
        foreach (var post in posts)
        {
            items.Add(await MapToDtoAsync(post, currentUserId, cancellationToken));
        }

        return new PagedResult<PostDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PostDto>> SearchAsync(string query, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _unitOfWork.Posts.SearchAsync(query, page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Posts.GetSearchCountAsync(query, cancellationToken);
        var items = new List<PostDto>();
        foreach (var post in posts)
        {
            items.Add(await MapToDtoAsync(post, currentUserId, cancellationToken));
        }

        return new PagedResult<PostDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<PostDto>> GetRelatedAsync(int postId, int count, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        // Get the post first to know its tags
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null || !post.PostTags.Any())
            return new List<PostDto>();

        var tagIds = post.PostTags.Select(pt => pt.TagId).ToList();
        var relatedPosts = await _unitOfWork.Posts.GetRelatedByTagsAsync(postId, tagIds, count, cancellationToken);

        var result = new List<PostDto>();
        foreach (var related in relatedPosts)
        {
            result.Add(await MapToDtoAsync(related, currentUserId, cancellationToken));
        }

        return result;
    }

    public async Task<PagedResult<PostDto>> GetByAuthorAsync(string authorId, int page, int pageSize, PostStatus? status = null, string? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var posts = await _unitOfWork.Posts.GetByAuthorIdAsync(authorId, page, pageSize, status, cancellationToken);
        var totalCount = await _unitOfWork.Posts.GetCountByAuthorIdAsync(authorId, status, cancellationToken);

        var items = new List<PostDto>();
        foreach (var post in posts)
        {
            items.Add(await MapToDtoAsync(post, currentUserId, cancellationToken));
        }

        return new PagedResult<PostDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PostDto>> GetBookmarkedPostsAsync(int page, int pageSize, string userId, CancellationToken cancellationToken = default)
    {
        var bookmarks = await _unitOfWork.Bookmarks.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Bookmarks.GetCountByUserIdAsync(userId, cancellationToken);

        var items = new List<PostDto>();
        foreach (var bookmark in bookmarks)
        {
            if (bookmark.Post != null)
            {
                items.Add(await MapToDtoAsync(bookmark.Post, userId, cancellationToken));
            }
        }

        return new PagedResult<PostDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PostDetailDto> CreateAsync(CreatePostDto dto, string authorId, CancellationToken cancellationToken = default)
    {
        var slug = await GenerateUniqueSlugAsync(dto.Title, null, cancellationToken);
        var renderedContent = _markdownService.RenderToHtml(dto.Content);
        var excerpt = _markdownService.GenerateExcerpt(dto.Content);
        var readingTime = _markdownService.CalculateReadingTime(dto.Content);

        var post = new Post
        {
            Title = dto.Title,
            Slug = slug,
            Content = dto.Content,
            RenderedContent = renderedContent,
            Excerpt = excerpt,
            CoverImageUrl = dto.CoverImageUrl,
            MetaTitle = dto.MetaTitle ?? dto.Title,
            MetaDescription = dto.MetaDescription ?? excerpt,
            MetaKeywords = dto.MetaKeywords,
            Status = dto.Publish ? PostStatus.Published : PostStatus.Draft,
            PublishedAt = dto.Publish ? DateTime.UtcNow : null,
            ReadingTimeMinutes = readingTime,
            AuthorId = authorId,
            CreatedBy = authorId
        };

        // Handle tags
        if (dto.Tags.Any())
        {
            var tags = await _unitOfWork.Tags.GetOrCreateTagsAsync(dto.Tags, cancellationToken);
            foreach (var tag in tags)
            {
                post.PostTags.Add(new PostTag { Post = post, Tag = tag });
            }
        }

        await _unitOfWork.Posts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload post with navigation properties to avoid null reference in mapping
        var savedPost = await _unitOfWork.Posts.GetByIdAsync(post.Id, cancellationToken);
        if (savedPost == null)
            throw new InvalidOperationException($"Failed to retrieve post with ID {post.Id} after creation.");
        return await MapToDetailDtoAsync(savedPost, authorId, cancellationToken);
    }

    public async Task<PostDetailDto> UpdateAsync(UpdatePostDto dto, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(dto.Id, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != authorId && !isAdmin)
            throw new UnauthorizedAccessException("You are not the author of this post");

        // Regenerate slug if title changed
        if (post.Title != dto.Title)
        {
            post.Slug = await GenerateUniqueSlugAsync(dto.Title, post.Id, cancellationToken);
        }

        post.Title = dto.Title;
        post.Content = dto.Content;
        post.RenderedContent = _markdownService.RenderToHtml(dto.Content);
        post.Excerpt = _markdownService.GenerateExcerpt(dto.Content);
        post.ReadingTimeMinutes = _markdownService.CalculateReadingTime(dto.Content);
        post.CoverImageUrl = dto.CoverImageUrl;
        post.MetaTitle = dto.MetaTitle ?? dto.Title;
        post.MetaDescription = dto.MetaDescription ?? post.Excerpt;
        post.MetaKeywords = dto.MetaKeywords;
        post.UpdatedBy = authorId;

        if (dto.Publish && post.Status == PostStatus.Draft)
        {
            post.Status = PostStatus.Published;
            post.PublishedAt = DateTime.UtcNow;
        }

        // Update tags
        post.PostTags.Clear();
        if (dto.Tags.Any())
        {
            var tags = await _unitOfWork.Tags.GetOrCreateTagsAsync(dto.Tags, cancellationToken);
            foreach (var tag in tags)
            {
                post.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
            }
        }

        await _unitOfWork.Posts.UpdateAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload post with navigation properties to avoid null reference in Tag mapping
        var savedPost = await _unitOfWork.Posts.GetByIdAsync(post.Id, cancellationToken);
        if (savedPost == null)
            throw new InvalidOperationException($"Failed to retrieve post with ID {post.Id} after update.");
        return await MapToDetailDtoAsync(savedPost, authorId, cancellationToken);
    }

    public async Task PublishAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != authorId && !isAdmin)
            throw new UnauthorizedAccessException("You are not the author of this post");

        post.Status = PostStatus.Published;
        post.PublishedAt = DateTime.UtcNow;
        post.UpdatedBy = authorId;

        await _unitOfWork.Posts.UpdateAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnpublishAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != authorId && !isAdmin)
            throw new UnauthorizedAccessException("You are not the author of this post");

        post.Status = PostStatus.Unpublished;
        post.UpdatedBy = authorId;

        await _unitOfWork.Posts.UpdateAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int postId, string authorId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != authorId && !isAdmin)
            throw new UnauthorizedAccessException("You are not the author of this post");

        await _unitOfWork.Posts.DeleteAsync(postId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementViewAsync(int postId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Posts.IncrementViewCountAsync(postId, cancellationToken);
    }

    public async Task UpdateTrendingScoresAsync(CancellationToken cancellationToken = default)
    {
        // Trending score algorithm: (likes × 2 + views × 0.5 + comments × 3) / (hoursAge + 2)^1.5
        var posts = await _unitOfWork.Posts.GetAllAsync(cancellationToken);
        var publishedPosts = posts.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue).ToList();

        foreach (var post in publishedPosts)
        {
            var hoursAge = (DateTime.UtcNow - post.PublishedAt!.Value).TotalHours;
            var likeCount = post.Likes?.Count ?? 0;
            var commentCount = post.Comments?.Count ?? 0;
            var viewCount = post.ViewCount;

            var score = (likeCount * 2 + viewCount * 0.5 + commentCount * 3) / Math.Pow(hoursAge + 2, 1.5);
            post.TrendingScore = double.IsFinite(score) ? score : 0;
            post.TrendingScoreUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Posts.UpdateAsync(post, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, int? excludeId, CancellationToken cancellationToken)
    {
        var baseSlug = GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        while (await _unitOfWork.Posts.SlugExistsAsync(slug, excludeId, cancellationToken))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        // Fallback for empty slugs (e.g., titles with only special characters)
        if (string.IsNullOrEmpty(slug))
        {
            slug = $"post-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        // Trim after truncation to handle edge case where truncation cuts after a dash
        if (slug.Length > 200)
        {
            slug = slug.Substring(0, 200).Trim('-');
        }
        return slug;
    }

    private async Task<PostDto> MapToDtoAsync(Post post, string? currentUserId, CancellationToken cancellationToken)
    {
        var isLiked = currentUserId != null && await _unitOfWork.Likes.ExistsAsync(currentUserId, post.Id, cancellationToken);
        var isBookmarked = currentUserId != null && await _unitOfWork.Bookmarks.ExistsAsync(currentUserId, post.Id, cancellationToken);

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
            IsLiked = isLiked,
            IsBookmarked = isBookmarked,
            Author = post.Author != null ? MapUserToDto(post.Author) : null,
            Tags = post.PostTags?.Where(pt => pt.Tag != null).Select(pt => new TagDto
            {
                Id = pt.Tag!.Id,
                Name = pt.Tag.Name,
                Slug = pt.Tag.Slug,
                Color = pt.Tag.Color
            }).ToList() ?? new List<TagDto>()
        };
    }

    private async Task<PostDetailDto> MapToDetailDtoAsync(Post post, string? currentUserId, CancellationToken cancellationToken)
    {
        var baseDto = await MapToDtoAsync(post, currentUserId, cancellationToken);

        return new PostDetailDto
        {
            Id = baseDto.Id,
            Title = baseDto.Title,
            Slug = baseDto.Slug,
            Excerpt = baseDto.Excerpt,
            CoverImageUrl = baseDto.CoverImageUrl,
            Status = baseDto.Status,
            PublishedAt = baseDto.PublishedAt,
            CreatedAt = baseDto.CreatedAt,
            ViewCount = baseDto.ViewCount,
            ReadingTimeMinutes = baseDto.ReadingTimeMinutes,
            LikeCount = baseDto.LikeCount,
            CommentCount = baseDto.CommentCount,
            BookmarkCount = baseDto.BookmarkCount,
            IsLiked = baseDto.IsLiked,
            IsBookmarked = baseDto.IsBookmarked,
            Author = baseDto.Author,
            Tags = baseDto.Tags,
            Content = post.Content,
            RenderedContent = !string.IsNullOrEmpty(post.RenderedContent)
                ? post.RenderedContent
                : _markdownService.RenderToHtml(post.Content),
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            MetaKeywords = post.MetaKeywords
        };
    }

    private static UserDto MapUserToDto(ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            WebsiteUrl = user.WebsiteUrl,
            GitHubUrl = user.GitHubUrl,
            TwitterUrl = user.TwitterUrl,
            LinkedInUrl = user.LinkedInUrl,
            Location = user.Location,
            CreatedAt = user.CreatedAt
        };
    }
}
