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
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public ChangePasswordInputModel ChangePasswordInput { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public bool HasPassword { get; set; }

    public string ActiveTab { get; set; } = "profile";

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

        [Display(Name = "Profile Image URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Upload Profile Image")]
        public IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        HasPassword = await _userManager.HasPasswordAsync(user);

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

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            HasPassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        if (Input.AvatarFile != null)
        {
            var validation = ValidateImageFile(Input.AvatarFile);
            if (!validation.IsValid)
            {
                ModelState.AddModelError(string.Empty, validation.ErrorMessage!);
                HasPassword = await _userManager.HasPasswordAsync(user);
                return Page();
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
            {
                DeleteOldAvatar(user.AvatarUrl);
            }

            var avatarPath = await SaveUploadedAvatar(Input.AvatarFile, user.Id);
            user.AvatarUrl = avatarPath;
        }
        else if (!string.IsNullOrEmpty(Input.AvatarUrl))
        {
            user.AvatarUrl = Input.AvatarUrl;
        }

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
            HasPassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        ActiveTab = "account";
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            HasPassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, ChangePasswordInput.OldPassword, ChangePasswordInput.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            HasPassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your password has been changed.";

        return RedirectToPage();
    }

    private (bool IsValid, string? ErrorMessage) ValidateImageFile(IFormFile file)
    {
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return (false, "File size must be less than 5MB");
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
        }
    }
}
