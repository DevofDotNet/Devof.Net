using Blog.Application.Services;
using Blog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Blog.Web.Pages;

public class NewsletterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<NewsletterModel> _logger;

    public NewsletterModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<NewsletterModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? StatusMessage { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool CurrentlySubscribed { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public bool Subscribe { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

        if (IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CurrentlySubscribed = user.NewsletterSubscribed;
                Input.Email = user.Email ?? string.Empty;
            }
        }
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (action == "subscribe")
        {
            return await HandleSubscribeAsync();
        }
        else if (action == "unsubscribe")
        {
            return await HandleUnsubscribeAsync();
        }

        return Page();
    }

    private async Task<IActionResult> HandleSubscribeAsync()
    {
        // GDPR: Require double opt-in - send confirmation email instead of immediate subscription
        var user = IsAuthenticated ? await _userManager.GetUserAsync(User) : null;

        // Generate confirmation token
        var confirmationToken = Guid.NewGuid().ToString("N");
        var confirmationLink = Url.Page("/NewsletterConfirm", null, new { token = confirmationToken, email = Input.Email }, Request.Scheme);

        // Store pending subscription in temp data or use a pending subscriptions table
        // For simplicity, we'll send a confirmation email
        var emailSent = await _emailService.SendNotificationEmailAsync(
            Input.Email,
            "Confirm your newsletter subscription",
            $"<p>Please confirm your newsletter subscription by clicking <a href='{confirmationLink}'>here</a>.</p>");

        if (emailSent)
        {
            StatusMessage = "Please check your email to confirm your subscription.";
            IsSuccess = true;
            _logger.LogInformation("Confirmation email sent to {Email} for newsletter", Input.Email);
        }
        else
        {
            StatusMessage = "Failed to send confirmation email. Please try again later.";
            IsSuccess = false;
            _logger.LogWarning("Failed to send confirmation email to {Email}", Input.Email);
        }

        return Page();
    }

    private async Task<IActionResult> HandleUnsubscribeAsync()
    {
        // Unsubscribe from Brevo
        var emailSent = await _emailService.UnsubscribeFromNewsletterAsync(Input.Email);

        if (IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.NewsletterSubscribed = false;
                user.NewsletterSubscribedAt = null;
                await _userManager.UpdateAsync(user);
                CurrentlySubscribed = false;
            }
        }

        if (emailSent)
        {
            StatusMessage = "Successfully unsubscribed from the newsletter.";
            IsSuccess = true;
            _logger.LogInformation("Email {Email} unsubscribed from newsletter", Input.Email);
        }
        else
        {
            StatusMessage = "Failed to unsubscribe. Please try again later.";
            IsSuccess = false;
            _logger.LogWarning("Failed to unsubscribe {Email} from newsletter", Input.Email);
        }

        return Page();
    }
}
