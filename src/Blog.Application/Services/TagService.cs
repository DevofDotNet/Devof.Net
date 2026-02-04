using Blog.Application.DTOs;
using Blog.Domain.Interfaces;

namespace Blog.Application.Services;

public interface ITagService
{
    Task<List<TagDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TagDto>> GetAllWithCountsAsync(CancellationToken cancellationToken = default);
    Task<List<TagDto>> GetPopularAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<TagDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;

    public TagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TagDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _unitOfWork.Tags.GetAllAsync(cancellationToken);
        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            Color = t.Color,
            PostCount = t.PostTags?.Count ?? 0
        }).ToList();
    }

    public async Task<List<TagDto>> GetAllWithCountsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _unitOfWork.Tags.GetAllWithCountsAsync(cancellationToken);
        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            Color = t.Color,
            PostCount = t.PostTags?.Count ?? 0
        }).ToList();
    }

    public async Task<List<TagDto>> GetPopularAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var tags = await _unitOfWork.Tags.GetPopularAsync(count, cancellationToken);
        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            Color = t.Color,
            PostCount = t.PostTags?.Count ?? 0
        }).ToList();
    }

    public async Task<TagDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tag = await _unitOfWork.Tags.GetBySlugAsync(slug, cancellationToken);
        if (tag == null) return null;

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            Description = tag.Description,
            Color = tag.Color,
            PostCount = tag.PostTags?.Count ?? 0
        };
    }
}
