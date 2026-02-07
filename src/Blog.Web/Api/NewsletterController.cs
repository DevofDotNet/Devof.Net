using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Blog.Web.Api;

[ApiController]
[Route("api/newsletter")]
[EnableRateLimiting("fixed")]
public class NewsletterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(ApplicationDbContext context, ILogger<NewsletterController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }

        email = email.Trim().ToLowerInvariant();

        // Basic email validation
        if (!IsValidEmail(email))
        {
            return BadRequest(new { success = false, message = "Please enter a valid email address." });
        }

        try
        {
            // Check if already subscribed
            var existing = await _context.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    return Ok(new { success = true, message = "You're already subscribed!" });
                }
                else
                {
                    // Reactivate subscription
                    existing.IsActive = true;
                    existing.UnsubscribedAt = null;
                    existing.SubscribedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Welcome back! Your subscription has been reactivated." });
                }
            }

            // Create new subscriber
            var subscriber = new Subscriber
            {
                Email = email,
                SubscribedAt = DateTime.UtcNow,
                ConfirmationToken = Guid.NewGuid().ToString("N"),
                IsActive = true
            };

            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New newsletter subscriber: {Email}", email);

            return Ok(new { success = true, message = "Thanks for subscribing! You'll receive the latest posts in your inbox." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing email: {Email}", email);
            return StatusCode(500, new { success = false, message = "Something went wrong. Please try again later." });
        }
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromForm] string email, [FromForm] string? token = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }

        email = email.Trim().ToLowerInvariant();

        var subscriber = await _context.Subscribers
            .FirstOrDefaultAsync(s => s.Email == email && s.IsActive);

        if (subscriber == null)
        {
            return Ok(new { success = true, message = "You have been unsubscribed." });
        }

        subscriber.IsActive = false;
        subscriber.UnsubscribedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Newsletter unsubscribe: {Email}", email);

        return Ok(new { success = true, message = "You have been unsubscribed successfully." });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
