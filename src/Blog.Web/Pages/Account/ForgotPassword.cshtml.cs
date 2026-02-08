using Blog.Application.Services;
using Blog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Blog.Web.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Always show the same message for security reasons (don't reveal if email exists)
            SuccessMessage = "If an account with that email exists, a password reset link has been sent.";

            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { code },
                    protocol: Request.Scheme);

                if (callbackUrl != null)
                {
                    var emailSent = await _emailService.SendPasswordResetAsync(
                        user.Email!,
                        user.UserName!,
                        callbackUrl);

                    if (emailSent)
                    {
                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send password reset email to {Email}", user.Email);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Password reset requested for non-existent or unconfirmed email: {Email}", Input.Email);
            }

            return Page();
        }

        return Page();
    }
}
