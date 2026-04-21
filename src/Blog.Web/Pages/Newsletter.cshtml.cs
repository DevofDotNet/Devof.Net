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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterModel> _logger;

    public NewsletterModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ApplicationDbContext context,
        ILogger<NewsletterModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _context = context;
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
        var email = Input.Email.Trim().ToLowerInvariant();
        string confirmationLink;
        bool emailSent;

        // Check if already subscribed
        var existing = await _context.Subscribers.FirstOrDefaultAsync(s => s.Email == email);
        if (existing != null)
        {
            if (existing.IsActive && existing.IsConfirmed)
            {
                StatusMessage = "You're already subscribed!";
                IsSuccess = true;
                return Page();
            }
            // Update token for re-confirmation
            existing.ConfirmationToken = Guid.NewGuid().ToString("N");
            existing.IsActive = false;
            existing.IsConfirmed = false;
            existing.SubscribedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            confirmationLink = $"{Request.Scheme}://{Request.Host}/api/newsletter/confirm?token={existing.ConfirmationToken}&email={existing.Email}";
            emailSent = await _emailService.SendNotificationEmailAsync(
                email,
                "Confirm your newsletter subscription",
                $"<p>Please confirm your newsletter subscription by clicking <a href='{confirmationLink}'>here</a>.</p>");
            
            StatusMessage = "Welcome back! Please check your email to confirm your subscription.";
            IsSuccess = true;
            return Page();
        }

        // Generate confirmation token
        var confirmationToken = Guid.NewGuid().ToString("N");

        // Store pending subscription in database (matching API implementation)
        var subscriber = new Subscriber
        {
            Email = email,
            SubscribedAt = DateTime.UtcNow,
            ConfirmationToken = confirmationToken,
            IsActive = false,
            IsConfirmed = false
        };
        _context.Subscribers.Add(subscriber);
        await _context.SaveChangesAsync();

        // Use API endpoint for confirmation link
        confirmationLink = $"{Request.Scheme}://{Request.Host}/api/newsletter/confirm?token={confirmationToken}&email={email}";

        emailSent = await _emailService.SendNotificationEmailAsync(
            email,
            "Confirm your newsletter subscription",
            $"<p>Please confirm your newsletter subscription by clicking <a href='{confirmationLink}'>here</a>.</p>");

        if (emailSent)
        {
            StatusMessage = "Please check your email to confirm your subscription.";
            IsSuccess = true;
            _logger.LogInformation("Confirmation email sent to {Email} for newsletter", email);
        }
        else
        {
            StatusMessage = "Failed to send confirmation email. Please try again later.";
            IsSuccess = false;
            _logger.LogWarning("Failed to send confirmation email to {Email}", email);
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
