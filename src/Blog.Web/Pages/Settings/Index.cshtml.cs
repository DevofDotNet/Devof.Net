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

    public IndexModel(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
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

        // Update fields
        user.DisplayName = Input.DisplayName;
        user.Bio = Input.Bio;
        user.Location = Input.Location;
        user.WebsiteUrl = Input.WebsiteUrl;
        user.GitHubUrl = Input.GitHubUrl;
        user.TwitterUrl = Input.TwitterUrl;
        user.LinkedInUrl = Input.LinkedInUrl;
        user.AvatarUrl = Input.AvatarUrl;
        
        user.UpdatedAt = DateTime.UtcNow;

        // Using UserManager to update is safer for IdentityUser, but UnitOfWork is also fine if tracking.
        // Since we injected UserManager, let's use it or UpdateAsync.
        // However, we are modifying custom properties.
        
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
}
