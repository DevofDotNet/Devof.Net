using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Blog.Web.Pages.Post;

public class DetailsModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IEngagementService _engagementService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IPostService postService,
        ICommentService commentService,
        IEngagementService engagementService,
        ILogger<DetailsModel> logger)
    {
        _postService = postService;
        _commentService = commentService;
        _engagementService = engagementService;
        _logger = logger;
    }

    public PostDetailDto? Post { get; set; }
    public PagedResult<CommentDto> Comments { get; set; } = new();
    public List<PostDto> RelatedPosts { get; set; } = new();
    
    // Authorization properties - calculated after Post is loaded
    public bool CanEdit => Post != null && User.Identity?.IsAuthenticated == true && IsCurrentUserAuthor();
    public bool CanDelete => Post != null && User.Identity?.IsAuthenticated == true && (IsCurrentUserAuthor() || User.IsInRole("Admin"));
    
    private bool IsCurrentUserAuthor()
    {
        if (Post?.Author == null) return false;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) && Post.Author.Id == userId;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int page = 1)
    {
        // Load post WITHOUT userId first to get clean author data
        var postWithoutUser = await _postService.GetBySlugAsync(slug, null);
        
        // Now check if current user can edit/delete
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var isAuthor = isAuthenticated && postWithoutUser?.Author?.Id == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        
        // Load post WITH userId for like/bookmark status
        var userId = isAuthenticated ? currentUserId : null;
        Post = await _postService.GetBySlugAsync(slug, userId);

        if (Post == null)
            return Page();

        // Increment view count
        await _postService.IncrementViewAsync(Post.Id);

        Comments = await _commentService.GetByPostIdAsync(Post.Id, page);
        RelatedPosts = await _postService.GetRelatedAsync(Post.Id, 3, userId);

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
        _logger.LogInformation("OnPostLikeJsonAsync called for slug: {Slug}, userId: {UserId}", slug, userId ?? "null");
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User not authenticated, redirecting to login");
            return new JsonResult(new { success = false, redirectUrl = $"/Account/Login?returnUrl=/post/{slug}" });
        }

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
        {
            _logger.LogWarning("Post not found: {Slug}", slug);
            return new JsonResult(new { success = false, error = "Post not found" });
        }

        _logger.LogInformation("Post found: {PostId}, IsLiked: {IsLiked}", post.Id, post.IsLiked);
        
        bool isLiked;
        try {
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
        } catch (Exception ex) {
            _logger.LogError(ex, "Error toggling like for post {PostId}", post.Id);
            return new JsonResult(new { success = false, error = ex.Message });
        }

        var newCount = isLiked ? post.LikeCount + 1 : post.LikeCount - 1;
        _logger.LogInformation("Like toggled successfully, isLiked: {IsLiked}, newCount: {NewCount}", isLiked, newCount);
        return new JsonResult(new { success = true, isLiked, count = newCount });
    }

    public async Task<IActionResult> OnPostBookmarkJsonAsync(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("OnPostBookmarkJsonAsync called for slug: {Slug}, userId: {UserId}", slug, userId ?? "null");
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User not authenticated, redirecting to login");
            return new JsonResult(new { success = false, redirectUrl = $"/Account/Login?returnUrl=/post/{slug}" });
        }

        var post = await _postService.GetBySlugAsync(slug, userId);
        if (post == null)
        {
            _logger.LogWarning("Post not found: {Slug}", slug);
            return new JsonResult(new { success = false, error = "Post not found" });
        }

        _logger.LogInformation("Post found: {PostId}, IsBookmarked: {IsBookmarked}", post.Id, post.IsBookmarked);
        
        bool isBookmarked;
        try {
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
        } catch (Exception ex) {
            _logger.LogError(ex, "Error toggling bookmark for post {PostId}", post.Id);
            return new JsonResult(new { success = false, error = ex.Message });
        }

        _logger.LogInformation("Bookmark toggled successfully, isBookmarked: {IsBookmarked}", isBookmarked);
        return new JsonResult(new { success = true, isBookmarked });
    }
}
