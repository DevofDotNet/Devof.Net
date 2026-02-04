using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Blog.Web.Pages.Post;

public class DetailsModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IEngagementService _engagementService;

    public DetailsModel(
        IPostService postService,
        ICommentService commentService,
        IEngagementService engagementService)
    {
        _postService = postService;
        _commentService = commentService;
        _engagementService = engagementService;
    }

    public PostDetailDto? Post { get; set; }
    public List<CommentDto> Comments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Post = await _postService.GetBySlugAsync(slug, userId);

        if (Post == null)
            return Page();

        // Increment view count
        await _postService.IncrementViewAsync(Post.Id);

        Comments = await _commentService.GetByPostIdAsync(Post.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostLikeAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { returnUrl = $"/post/{slug}" });

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
            return NotFound();

        if (post.IsLiked)
            await _engagementService.UnlikePostAsync(post.Id, userId);
        else
            await _engagementService.LikePostAsync(post.Id, userId);

        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostBookmarkAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { returnUrl = $"/post/{slug}" });

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
            return NotFound();

        if (post.IsBookmarked)
            await _engagementService.UnbookmarkPostAsync(post.Id, userId);
        else
            await _engagementService.BookmarkPostAsync(post.Id, userId);

        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostCommentAsync(string slug, string content)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { returnUrl = $"/post/{slug}" });

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(content))
            return RedirectToPage(new { slug });

        await _commentService.CreateAsync(new CreateCommentDto
        {
            PostId = post.Id,
            Content = content
        }, userId);

        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostLikeJsonAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, redirectUrl = $"/Account/Login?returnUrl=/post/{slug}" });

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
            return new JsonResult(new { success = false, error = "Post not found" });

        bool isLiked;
        if (post.IsLiked)
        {
            await _engagementService.UnlikePostAsync(post.Id, userId);
            isLiked = false;
        }
        else
        {
            await _engagementService.LikePostAsync(post.Id, userId);
            isLiked = true;
        }

        var newCount = isLiked ? post.LikeCount + 1 : post.LikeCount - 1;
        return new JsonResult(new { success = true, isLiked, count = newCount });
    }

    public async Task<IActionResult> OnPostBookmarkJsonAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, redirectUrl = $"/Account/Login?returnUrl=/post/{slug}" });

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
            return new JsonResult(new { success = false, error = "Post not found" });

        bool isBookmarked;
        if (post.IsBookmarked)
        {
            await _engagementService.UnbookmarkPostAsync(post.Id, userId);
            isBookmarked = false;
        }
        else
        {
            await _engagementService.BookmarkPostAsync(post.Id, userId);
            isBookmarked = true;
        }

        return new JsonResult(new { success = true, isBookmarked });
    }
}
