using Blog.Application.DTOs;
using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Blog.Web.Pages.Post;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IImageService _imageService;
    private readonly IConfiguration _configuration;

    public CreateModel(IPostService postService, IImageService imageService, IConfiguration configuration)
    {
        _postService = postService;
        _imageService = imageService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? TagsInput { get; set; }

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(50)]
        public string Content { get; set; } = string.Empty;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }

    public void OnGet()
    {
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
            string? coverImageUrl = null;
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

            var createDto = new CreatePostDto
            {
                Title = Input.Title,
                Content = Input.Content,
                CoverImageUrl = coverImageUrl,
                MetaTitle = Input.MetaTitle,
                MetaDescription = Input.MetaDescription,
                MetaKeywords = Input.MetaKeywords,
                Tags = tags,
                Publish = publish
            };

            var post = await _postService.CreateAsync(createDto, userId);

            if (publish)
                return RedirectToPage("/Post/Details", new { slug = post.Slug });
            else
                return RedirectToPage("/Settings/Drafts");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
