using System.Security.Claims;
using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Account;

[Authorize]
public class BookmarksModel : PageModel
{
    private readonly IPostService _postService;

    public BookmarksModel(IPostService postService)
    {
        _postService = postService;
    }

    public PagedResult<PostDto> Posts { get; set; } = new();

    public async Task OnGetAsync(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            Posts = await _postService.GetBookmarkedPostsAsync(page, 12, userId);
        }
    }
}
