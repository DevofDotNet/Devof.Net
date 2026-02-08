using Blog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Blog.Web.Pages.Account;

public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ConfirmEmailModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public string? StatusMessage { get; set; }
    public bool EmailConfirmed { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            StatusMessage = "Invalid email confirmation link.";
            EmailConfirmed = false;
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            StatusMessage = $"Unable to find user with ID '{userId}'.";
            EmailConfirmed = false;
            return Page();
        }

        // Decode the token
        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            StatusMessage = "Thank you for confirming your email. You can now log in.";
            EmailConfirmed = true;
            _logger.LogInformation("User {UserId} confirmed their email successfully", userId);
        }
        else
        {
            StatusMessage = "Error confirming your email. The link may have expired.";
            EmailConfirmed = false;
            _logger.LogWarning("Failed to confirm email for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Page();
    }
}
