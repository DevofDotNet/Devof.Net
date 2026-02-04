using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Blog.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ITagService _tagService;

    public IndexModel(IPostService postService, ITagService tagService)
    {
        _postService = postService;
        _tagService = tagService;
    }

    public PagedResult<PostDto> Posts { get; set; } = new();
    public List<PostDto> TrendingPosts { get; set; } = new();
    public List<TagDto> PopularTags { get; set; } = new();
    public string Feed { get; set; } = "latest";

    public async Task OnGetAsync(int page = 1, string feed = "latest")
    {
        Feed = feed;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (feed == "trending")
        {
            var trendingPosts = await _postService.GetTrendingAsync(20, currentUserId);
            Posts = new PagedResult<PostDto>
            {
                Items = trendingPosts.Skip((page - 1) * 10).Take(10).ToList(),
                TotalCount = trendingPosts.Count,
                Page = page,
                PageSize = 10
            };
        }
        else
        {
            Posts = await _postService.GetLatestAsync(page, 10, currentUserId);
        }

        TrendingPosts = await _postService.GetTrendingAsync(5, currentUserId);
        PopularTags = await _tagService.GetPopularAsync(10);
    }
}
