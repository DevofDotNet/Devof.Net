using Blog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Blog.Web.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        UserManager<ApplicationUser> userManager,
        ILogger<ResetPasswordModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Code { get; set; }
    public string? StatusMessage { get; set; }
    public bool ResetSuccessful { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? code = null)
    {
        if (code == null)
        {
            StatusMessage = "Invalid password reset link.";
            ResetSuccessful = false;
            return Page();
        }

        Code = code;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            StatusMessage = "Password has been reset successfully.";
            ResetSuccessful = true;
            return Page();
        }

        // Decode the token
        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Request.Form["Code"]!));

        var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
        if (result.Succeeded)
        {
            StatusMessage = "Your password has been reset successfully.";
            ResetSuccessful = true;
            _logger.LogInformation("User {Email} successfully reset their password", Input.Email);
        }
        else
        {
            StatusMessage = "Error resetting password. The link may have expired.";
            ResetSuccessful = false;
            _logger.LogWarning("Failed to reset password for {Email}: {Errors}", Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Page();
    }
}
