using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Blog.Web.Pages.Post;

[Authorize]
public class EditModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IImageService _imageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IPostService postService, IImageService imageService, IConfiguration configuration, ILogger<EditModel> logger)
    {
        _postService = postService;
        _imageService = imageService;
        _configuration = configuration;
        _logger = logger;
    }

    [BindProperty]
    public int PostId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? TagsInput { get; set; }

    public string? CurrentSlug { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(50)]
        public string Content { get; set; } = string.Empty;

        public string? CoverImageUrl { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? slug, int? id)
    {
        _logger.LogInformation("Edit page requested for slug: {Slug}, id: {Id}", slug, id);
        
        // Support both slug (preferred) and id for backward compatibility
        string? actualSlug = slug;
        int? actualId = id;
        
        if (string.IsNullOrEmpty(actualSlug) && actualId.HasValue)
        {
            // If id is provided but not slug, fetch the post to get the slug
            var postById = await _postService.GetByIdAsync(actualId.Value, null);
            if (postById == null)
            {
                _logger.LogWarning("Post not found by id: {Id}", actualId);
                return NotFound();
            }
            actualSlug = postById.Slug;
        }

        if (string.IsNullOrEmpty(actualSlug) && !actualId.HasValue)
        {
            _logger.LogWarning("No slug or id provided");
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User ID: {UserId}", userId ?? "null");
        
        var post = await _postService.GetBySlugAsync(actualSlug!, userId);

        if (post == null)
        {
            _logger.LogWarning("Post not found by slug: {Slug}", actualSlug);
            return NotFound();
        }

        _logger.LogInformation("Post found: {PostId}, AuthorId: {AuthorId}", post.Id, post.Author.Id);
        
        // Check if user is the author or admin
        if (post.Author.Id != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("User {UserId} is not authorized to edit post {PostId}. Author: {AuthorId}", 
                userId, post.Id, post.Author.Id);
            return Forbid();
        }

        PostId = post.Id;
        CurrentSlug = post.Slug;
        Input = new InputModel
        {
            Title = post.Title,
            Content = (post as PostDetailDto)?.Content ?? string.Empty,
            CoverImageUrl = post.CoverImageUrl,
            MetaTitle = (post as PostDetailDto)?.MetaTitle,
            MetaDescription = (post as PostDetailDto)?.MetaDescription,
            MetaKeywords = (post as PostDetailDto)?.MetaKeywords
        };
        TagsInput = string.Join(", ", post.Tags.Select(t => t.Name));

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool publish, IFormFile? coverImage)
    {
        _logger.LogInformation("OnPostAsync called for PostId: {PostId}, publish: {Publish}", PostId, publish);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState invalid");
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User not authenticated");
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Verify ownership
            var existingPost = await _postService.GetByIdAsync(PostId, userId);
            if (existingPost == null)
            {
                _logger.LogWarning("Post not found: {PostId}", PostId);
                return NotFound();
            }

            if (existingPost.Author.Id != userId && !User.IsInRole("Admin"))
            {
                _logger.LogWarning("User not authorized to edit post: {PostId}", PostId);
                return Forbid();
            }

            string? coverImageUrl = Input.CoverImageUrl;
            if (coverImage != null && coverImage.Length > 0)
            {
                var maxFileSizeMB = _configuration.GetValue<long>("AppSettings:MaxUploadSizeMB");
                if (maxFileSizeMB > 0 && coverImage.Length > maxFileSizeMB * 1024 * 1024)
                {
                    ErrorMessage = $"The file is too large. Maximum size is {maxFileSizeMB} MB.";
                    return Page();
                }

                using var stream = coverImage.OpenReadStream();
                coverImageUrl = await _imageService.UploadAsync(stream, coverImage.FileName, coverImage.ContentType);
            }

            var tags = string.IsNullOrWhiteSpace(TagsInput)
                ? new List<string>()
                : TagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).Take(5).ToList();

            var updateDto = new UpdatePostDto
            {
                Id = PostId,
                Title = Input.Title,
                Content = Input.Content,
                CoverImageUrl = coverImageUrl,
                MetaTitle = Input.MetaTitle,
                MetaDescription = Input.MetaDescription,
                MetaKeywords = Input.MetaKeywords,
                Tags = tags,
                Publish = publish
            };

            var updatedPost = await _postService.UpdateAsync(updateDto, userId);
            _logger.LogInformation("Post updated successfully: {PostId}", updatedPost.Id);
            CurrentSlug = updatedPost.Slug;

            if (publish)
                return RedirectToPage("/Post/Details", new { slug = updatedPost.Slug });

            SuccessMessage = "Post saved successfully!";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post: {PostId}", PostId);
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        try
        {
            var existingPost = await _postService.GetByIdAsync(PostId, userId);
            if (existingPost == null)
                return NotFound();

            if (existingPost.Author.Id != userId && !User.IsInRole("Admin"))
                return Forbid();

            await _postService.DeleteAsync(PostId, userId);
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
