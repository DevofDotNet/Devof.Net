using System.ComponentModel.DataAnnotations;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Settings;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public class InputModel
    {
        [Required]
        [Display(Name = "Display Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string DisplayName { get; set; } = string.Empty;

        [Display(Name = "Bio")]
        [StringLength(160, ErrorMessage = "Bio cannot exceed 160 characters.")] // Twitter style
        public string? Bio { get; set; }

        [Display(Name = "Location")]
        [StringLength(100)]
        public string? Location { get; set; }

        [Display(Name = "Website URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? WebsiteUrl { get; set; }

        [Display(Name = "GitHub Profile")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? GitHubUrl { get; set; }

        [Display(Name = "Twitter Profile")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? TwitterUrl { get; set; }

        [Display(Name = "LinkedIn Profile")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "Profile Image URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Upload Profile Image")]
        public IFormFile? AvatarFile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        Input = new InputModel
        {
            DisplayName = user.DisplayName ?? string.Empty,
            Bio = user.Bio,
            Location = user.Location,
            WebsiteUrl = user.WebsiteUrl,
            GitHubUrl = user.GitHubUrl,
            TwitterUrl = user.TwitterUrl,
            LinkedInUrl = user.LinkedInUrl,
            AvatarUrl = user.AvatarUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        // Handle file upload if provided
        if (Input.AvatarFile != null)
        {
            // Validate file
            var validation = ValidateImageFile(Input.AvatarFile);
            if (!validation.IsValid)
            {
                ModelState.AddModelError(string.Empty, validation.ErrorMessage!);
                return Page();
            }

            // Delete old avatar if it's an uploaded file (not a URL)
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
            {
                DeleteOldAvatar(user.AvatarUrl);
            }

            // Save new avatar
            var avatarPath = await SaveUploadedAvatar(Input.AvatarFile, user.Id);
            user.AvatarUrl = avatarPath;
        }
        else if (!string.IsNullOrEmpty(Input.AvatarUrl))
        {
            // Use URL if no file uploaded
            user.AvatarUrl = Input.AvatarUrl;
        }

        // Update fields
        user.DisplayName = Input.DisplayName;
        user.Bio = Input.Bio;
        user.Location = Input.Location;
        user.WebsiteUrl = Input.WebsiteUrl;
        user.GitHubUrl = Input.GitHubUrl;
        user.TwitterUrl = Input.TwitterUrl;
        user.LinkedInUrl = Input.LinkedInUrl;

        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    private (bool IsValid, string? ErrorMessage) ValidateImageFile(IFormFile file)
    {
        // Check file size (5MB max)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return (false, "File size must be less than 5MB");
        }

        // Check file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return (false, "Only JPG, PNG, and GIF images are allowed");
        }

        // Check MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return (false, "Invalid image format");
        }

        return (true, null);
    }

    private async Task<string> SaveUploadedAvatar(IFormFile file, string userId)
    {
        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";

        // Ensure uploads directory exists
        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        // Save file
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path for web
        return $"/uploads/avatars/{fileName}";
    }

    private void DeleteOldAvatar(string avatarUrl)
    {
        try
        {
            var fileName = Path.GetFileName(avatarUrl);
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "avatars", fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch
        {
            // Silently fail - old file cleanup is not critical
        }
    }
}
