using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages;

public class SearchModel : PageModel
{
    private readonly IPostService _postService;

    public SearchModel(IPostService postService)
    {
        _postService = postService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public PagedResult<PostDto> Results { get; set; } = new();

    public async Task OnGetAsync(int page = 1)
    {
        if (string.IsNullOrWhiteSpace(Q))
        {
            Results = new PagedResult<PostDto>();
            return;
        }

        Results = await _postService.SearchAsync(Q, page, 12);
    }
}
