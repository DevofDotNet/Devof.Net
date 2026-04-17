using Blog.Domain.Entities;
using Blog.Application.Services;
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
    private readonly IEmailService _emailService;

    public NewsletterController(ApplicationDbContext context, ILogger<NewsletterController> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
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
                    // Re-subscribe requires confirmation again
                    existing.ConfirmationToken = Guid.NewGuid().ToString("N");
                    existing.IsActive = false;
                    existing.IsConfirmed = false;
                    existing.UnsubscribedAt = null;
                    existing.SubscribedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Send confirmation email
                    await SendConfirmationEmailAsync(existing);

                    return Ok(new { success = true, message = "Welcome back! Please check your email to confirm your subscription." });
                }
            }

            // Create new subscriber (GDPR: require double opt-in)
            var subscriber = new Subscriber
            {
                Email = email,
                SubscribedAt = DateTime.UtcNow,
                ConfirmationToken = Guid.NewGuid().ToString("N"),
                IsActive = false,
                IsConfirmed = false
            };

            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();

            // Send confirmation email (GDPR double opt-in)
            await SendConfirmationEmailAsync(subscriber);

            _logger.LogInformation("New newsletter subscriber (pending confirmation): {Email}", email);

            return Ok(new { success = true, message = "Thanks for subscribing! Please check your email to confirm your subscription." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing email: {Email}", email);
            return StatusCode(500, new { success = false, message = "Something went wrong. Please try again later." });
        }
    }

    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm([FromQuery] string token, [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, message = "Invalid confirmation request." });
        }

        email = email.Trim().ToLowerInvariant();

        try
        {
            var subscriber = await _context.Subscribers
                .FirstOrDefaultAsync(s => s.Email == email && s.ConfirmationToken == token);

            if (subscriber == null)
            {
                return BadRequest(new { success = false, message = "Invalid confirmation token." });
            }

            if (subscriber.IsConfirmed && subscriber.IsActive)
            {
                return Ok(new { success = true, message = "You're already confirmed and subscribed!" });
            }

            // Activate subscription after confirmation
            subscriber.IsConfirmed = true;
            subscriber.IsActive = true;
            subscriber.ConfirmedAt = DateTime.UtcNow;
            subscriber.ConfirmationToken = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Newsletter confirmed: {Email}", email);

            return Ok(new { success = true, message = "Your subscription has been confirmed. Thank you!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming newsletter: {Email}", email);
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

        // First check if any subscriber exists (regardless of IsActive status)
        var subscriber = await _context.Subscribers
            .FirstOrDefaultAsync(s => s.Email == email);

        if (subscriber == null)
        {
            // No subscriber found - but return success to avoid revealing if email is subscribed
            return Ok(new { success = true, message = "You have been unsubscribed." });
        }

        if (!subscriber.IsActive)
        {
            // Already unsubscribed
            return Ok(new { success = true, message = "You are not currently subscribed." });
        }

        subscriber.IsActive = false;
        subscriber.UnsubscribedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Newsletter unsubscribe: {Email}", email);

        return Ok(new { success = true, message = "You have been unsubscribed successfully." });
    }

    private async Task SendConfirmationEmailAsync(Subscriber subscriber)
    {
        var confirmationLink = Url.Action("Confirm", "Newsletter",
            new { token = subscriber.ConfirmationToken, email = subscriber.Email },
            Request.Scheme);

        var subject = "Confirm your newsletter subscription";
        var body = $@"<html><body>
<h2>Welcome!</h2>
<p>Please confirm your newsletter subscription by clicking the link below:</p>
<p><a href=""{confirmationLink}"">Confirm Subscription</a></p>
<p>Or copy and paste this link: {confirmationLink}</p>
<p>If you didn't request this, please ignore this email.</p>
</body></html>";

        try
        {
            await _emailService.SendNotificationEmailAsync(subscriber.Email, subject, body);
            _logger.LogInformation("Confirmation email sent to: {Email}", subscriber.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email: {Email}", subscriber.Email);
        }
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