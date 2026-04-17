using Blog.Application.DTOs;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface ICommentService
{
    Task<PagedResult<CommentDto>> GetByPostIdAsync(int postId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<CommentDto> CreateAsync(CreateCommentDto dto, string authorId, CancellationToken cancellationToken = default);
    Task<CommentDto> UpdateAsync(int commentId, string content, string authorId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int commentId, string authorId, CancellationToken cancellationToken = default);
}

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMarkdownService _markdownService;

    public CommentService(IUnitOfWork unitOfWork, IMarkdownService markdownService)
    {
        _unitOfWork = unitOfWork;
        _markdownService = markdownService;
    }

    public async Task<PagedResult<CommentDto>> GetByPostIdAsync(int postId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var totalCount = await _unitOfWork.Comments.GetCountByPostIdAsync(postId, cancellationToken);
        var comments = await _unitOfWork.Comments.GetByPostIdAsync(postId, page, pageSize, cancellationToken);
        
        return new PagedResult<CommentDto>
        {
            Items = comments.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CommentDto> CreateAsync(CreateCommentDto dto, string authorId, CancellationToken cancellationToken = default)
    {
        // Validate post exists
        var post = await _unitOfWork.Posts.GetByIdAsync(dto.PostId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException($"Post with ID {dto.PostId} not found");

        var comment = new Comment
        {
            Content = dto.Content,
            RenderedContent = _markdownService.RenderToHtml(dto.Content),
            PostId = dto.PostId,
            AuthorId = authorId,
            ParentCommentId = dto.ParentCommentId
        };

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch with author info
        var created = await _unitOfWork.Comments.GetByIdAsync(comment.Id, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<CommentDto> UpdateAsync(int commentId, string content, string authorId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
            throw new InvalidOperationException("Comment not found");
        
        if (comment.AuthorId != authorId)
            throw new UnauthorizedAccessException("You are not the author of this comment");

        comment.Content = content;
        comment.RenderedContent = _markdownService.RenderToHtml(content);

        await _unitOfWork.Comments.UpdateAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(comment);
    }

    public async Task DeleteAsync(int commentId, string authorId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
            throw new InvalidOperationException("Comment not found");
        
        if (comment.AuthorId != authorId)
            throw new UnauthorizedAccessException("You are not the author of this comment");

        await _unitOfWork.Comments.SoftDeleteAsync(commentId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private CommentDto MapToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            RenderedContent = comment.RenderedContent,
            IsEdited = comment.IsEdited,
            IsDeleted = comment.IsDeleted,
            CreatedAt = comment.CreatedAt,
            ParentCommentId = comment.ParentCommentId,
            Author = new UserDto
            {
                Id = comment.Author.Id,
                UserName = comment.Author.UserName ?? string.Empty,
                DisplayName = comment.Author.DisplayName,
                AvatarUrl = comment.Author.AvatarUrl
            },
            Replies = comment.Replies?.Select(MapToDto).ToList() ?? new List<CommentDto>()
        };
    }
}
