using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Blog.Web.Pages;

public class NewsletterConfirmModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterConfirmModel> _logger;

    public NewsletterConfirmModel(ApplicationDbContext context, ILogger<NewsletterConfirmModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string? StatusMessage { get; set; }
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(string? token, string? email)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            StatusMessage = "Invalid confirmation link. Please check your email and try again.";
            IsSuccess = false;
            return Page();
        }

        email = email.Trim().ToLowerInvariant();

        try
        {
            var subscriber = await _context.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email && s.ConfirmationToken == token);

            if (subscriber == null)
            {
                _logger.LogWarning("Invalid newsletter confirmation attempt for {Email}", email);
                StatusMessage = "Invalid confirmation token. The link may have expired or already been used.";
                IsSuccess = false;
                return Page();
            }

            if (subscriber.IsConfirmed && subscriber.IsActive)
            {
                StatusMessage = "You're already confirmed and subscribed!";
                IsSuccess = true;
                return Page();
            }

            // Activate subscription after confirmation
            subscriber.IsConfirmed = true;
            subscriber.IsActive = true;
            subscriber.ConfirmedAt = DateTime.UtcNow;
            subscriber.ConfirmationToken = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Newsletter confirmed via page link: {Email}", email);

            StatusMessage = "Your subscription has been confirmed. Thank you!";
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming newsletter via page for {Email}", email);
            StatusMessage = "Something went wrong. Please try again later.";
            IsSuccess = false;
        }

        return Page();
    }
}
