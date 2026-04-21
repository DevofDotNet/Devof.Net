using Blog.Application.Services;
using Blog.Domain.Common;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

using Task = System.Threading.Tasks.Task;

namespace Blog.Infrastructure.Services;

public class BrevoEmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly TransactionalEmailsApi _apiInstance;

    public BrevoEmailService(IOptions<EmailOptions> emailOptions, ILogger<BrevoEmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_emailOptions.ApiKey))
        {
            throw new ArgumentException("Brevo API key is required.", nameof(emailOptions.Value.ApiKey));
        }

        // Configure Brevo API client
        Configuration.Default.ApiKey.Clear();
        Configuration.Default.ApiKey.Add("api-key", _emailOptions.ApiKey);
        _apiInstance = new TransactionalEmailsApi();
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string confirmationLink, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "Confirm your email address";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to {_emailOptions.SenderName}!</h1>
        </div>
        <div class='content'>
            <h2>Hi {userName},</h2>
            <p>Thank you for registering an account with us. To complete your registration, please confirm your email address by clicking the button below:</p>
            <p style='text-align: center;'>
                <a href='{confirmationLink}' class='button'>Confirm Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{confirmationLink}</p>
            <p>This link will expire in 24 hours for security reasons.</p>
            <p>If you didn't create an account, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} {_emailOptions.SenderName}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "Reset your password";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <h2>Hi {userName},</h2>
            <p>We received a request to reset your password. Click the button below to create a new password:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{resetLink}</p>
            <div class='warning'>
                <strong>Security Notice:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email and ensure your account is secure.
            </div>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} {_emailOptions.SenderName}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(toEmail, subject, htmlContent, cancellationToken);
    }

    public async Task<bool> SubscribeToNewsletterAsync(string email, string? firstName = null, string? lastName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_emailOptions.NewsletterListId))
            {
                _logger.LogWarning("Newsletter list ID is not configured. Skipping newsletter subscription for {Email}", email);
                return false;
            }

            var contactApi = new ContactsApi();
            var createContact = new CreateContact(
                email: email,
                listIds: new List<long?> { long.Parse(_emailOptions.NewsletterListId) },
                updateEnabled: true
            );

            if (!string.IsNullOrEmpty(firstName))
            {
                var attributes = new Dictionary<string, object> { { "FIRSTNAME", firstName } };
                if (!string.IsNullOrEmpty(lastName))
                {
                    attributes.Add("LASTNAME", lastName);
                }
                createContact.Attributes = attributes;
            }

            await Task.Run(() => contactApi.CreateContact(createContact), cancellationToken);
            _logger.LogInformation("Successfully subscribed {Email} to newsletter", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe {Email} to newsletter", email);
            return false;
        }
    }

    public async Task<bool> UnsubscribeFromNewsletterAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_emailOptions.NewsletterListId))
            {
                _logger.LogWarning("Newsletter list ID is not configured. Skipping newsletter unsubscription for {Email}", email);
                return false;
            }

            var contactApi = new ContactsApi();
            var contactEmails = new RemoveContactFromList(new List<string> { email });

            await Task.Run(() => contactApi.RemoveContactFromList(long.Parse(_emailOptions.NewsletterListId), contactEmails), cancellationToken);
            _logger.LogInformation("Successfully unsubscribed {Email} from newsletter", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe {Email} from newsletter", email);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var sender = new SendSmtpEmailSender(_emailOptions.SenderName, _emailOptions.SenderEmail);
            var to = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail) };
            var sendSmtpEmail = new SendSmtpEmail(sender, to, null, null, htmlContent, null, subject);

            await Task.Run(() => _apiInstance.SendTransacEmail(sendSmtpEmail), cancellationToken);
            _logger.LogInformation("Successfully sent email to {Email} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            return false;
        }
    }
}
