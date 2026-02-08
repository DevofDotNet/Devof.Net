using System.ComponentModel.DataAnnotations;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Settings;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment environment,
        ILogger<ProfileModel> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
        _logger = logger;
        _configuration = configuration;
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
        [StringLength(160, ErrorMessage = "Bio cannot exceed 160 characters.")]
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

        [Display(Name = "Avatar URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? AvatarUrl { get; set; }

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
            DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
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
        _logger.LogInformation("=== OnPostAsync called ===");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", _userManager.GetUserId(User));
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        _logger.LogInformation("User found: {UserName}, ModelState.IsValid: {IsValid}", user.UserName, ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
                }
            }
            return Page();
        }

        // Handle file upload
        if (Input.AvatarFile != null)
        {
            var validation = ValidateImageFile(Input.AvatarFile);
            if (!validation.IsValid)
            {
                ModelState.AddModelError(string.Empty, validation.ErrorMessage!);
                return Page();
            }

            // Delete old avatar if it was uploaded
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
            {
                DeleteOldAvatar(user.AvatarUrl);
            }

            user.AvatarUrl = await SaveUploadedAvatar(Input.AvatarFile, user.Id);
        }
        else if (!string.IsNullOrEmpty(Input.AvatarUrl))
        {
            user.AvatarUrl = Input.AvatarUrl;
        }

        // Update profile fields
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
                _logger.LogError("Update error: {Error}", error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        _logger.LogInformation("Profile updated successfully for user: {UserName}", user.UserName);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    private (bool IsValid, string? ErrorMessage) ValidateImageFile(IFormFile file)
    {
        var maxFileSizeMB = _configuration.GetValue<long>("AppSettings:MaxUploadSizeMB");
        var maxFileSize = (maxFileSizeMB > 0 ? maxFileSizeMB : 5) * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            return (false, $"File size must be less than {maxFileSizeMB}MB");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return (false, "Only JPG, PNG, and GIF images are allowed");
        }

        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return (false, "Invalid image format");
        }

        return (true, null);
    }

    private async Task<string> SaveUploadedAvatar(IFormFile file, string userId)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
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
            // Ignore deletion errors
        }
    }
}
