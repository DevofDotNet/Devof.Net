using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Tag;

public class IndexModel : PageModel
{
    private readonly ITagService _tagService;

    public IndexModel(ITagService tagService)
    {
        _tagService = tagService;
    }

    public List<TagDto> Tags { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tags = await _tagService.GetAllWithCountsAsync();
    }
}
