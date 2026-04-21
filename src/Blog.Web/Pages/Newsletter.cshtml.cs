using Blog.Application.Services;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Blog.Web.Pages;

public class NewsletterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<NewsletterModel> _logger;
    private readonly ApplicationDbContext _context;

    public NewsletterModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<NewsletterModel> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
        _context = context;
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
        var email = Input.Email.Trim().ToLowerInvariant();

        // Find or create subscriber record so the confirmation link works
        var subscriber = await _context.Subscribers
            .FirstOrDefaultAsync(s => s.Email == email);

        var isNew = subscriber == null;
        var confirmationToken = Guid.NewGuid().ToString("N");

        if (subscriber == null)
        {
            subscriber = new Subscriber
            {
                Email = email,
                ConfirmationToken = confirmationToken,
                IsActive = false,
                IsConfirmed = false,
                SubscribedAt = DateTime.UtcNow
            };
            _context.Subscribers.Add(subscriber);
        }
        else
        {
            subscriber.ConfirmationToken = confirmationToken;
            subscriber.IsActive = false;
            subscriber.IsConfirmed = false;
            subscriber.UnsubscribedAt = null;
            subscriber.SubscribedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Generate confirmation link pointing to the NewsletterConfirm page
        var confirmationLink = Url.Page("/NewsletterConfirm", null, new { token = confirmationToken, email }, Request.Scheme);

        var emailSent = await _emailService.SendNotificationEmailAsync(
            email,
            "Confirm your newsletter subscription",
            $"<p>Please confirm your newsletter subscription by clicking <a href='{confirmationLink}'>here</a>.</p><p>Or copy this link: {confirmationLink}</p>");

        if (emailSent)
        {
            StatusMessage = "Please check your email to confirm your subscription.";
            IsSuccess = true;
            _logger.LogInformation("Newsletter confirmation email sent to {Email} (isNew={IsNew})", email, isNew);
        }
        else
        {
            StatusMessage = "Failed to send confirmation email. Please try again later.";
            IsSuccess = false;
            _logger.LogWarning("Failed to send newsletter confirmation email to {Email}", email);
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