using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Blog.Web.Pages.Post;

[Authorize]
public class EditModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IImageService _imageService;

    public EditModel(IPostService postService, IImageService imageService)
    {
        _postService = postService;
        _imageService = imageService;
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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var post = await _postService.GetBySlugAsync(slug, userId);

        if (post == null)
            return NotFound();

        // Check if user is the author or admin
        if (post.Author.Id != userId && !User.IsInRole("Admin"))
            return Forbid();

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
        if (!ModelState.IsValid)
            return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        try
        {
            // Verify ownership
            var existingPost = await _postService.GetByIdAsync(PostId, userId);
            if (existingPost == null)
                return NotFound();

            if (existingPost.Author.Id != userId && !User.IsInRole("Admin"))
                return Forbid();

            string? coverImageUrl = Input.CoverImageUrl;
            if (coverImage != null && coverImage.Length > 0)
            {
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
            CurrentSlug = updatedPost.Slug;

            if (publish)
                return RedirectToPage("/Post/Details", new { slug = updatedPost.Slug });

            SuccessMessage = "Post saved successfully!";
            return Page();
        }
        catch (Exception ex)
        {
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
