using System.Security.Claims;
using Blog.Application.DTOs;
using Blog.Application.Services;
using Blog.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Settings;

[Authorize]
public class DraftsModel : PageModel
{
    private readonly IPostService _postService;

    public DraftsModel(IPostService postService)
    {
        _postService = postService;
    }

    public PagedResult<PostDto> Posts { get; set; } = new();

    public async Task OnGetAsync(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            Posts = await _postService.GetByAuthorAsync(userId, page, 12, PostStatus.Draft, userId);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        await _postService.DeleteAsync(id, userId);
        return RedirectToPage();
    }
}
