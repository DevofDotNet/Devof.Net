using Blog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Blog.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            // Find user by email first, then use their username for sign-in
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} logged in.", user.UserName);
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ErrorMessage = "This account has been locked out, please try again later.";
                return Page();
            }
            else
            {
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }
        }

        return Page();
    }
}
