namespace Blog.Application.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to a user
    /// </summary>
    Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string confirmationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to a user
    /// </summary>
    Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic notification email
    /// </summary>
    Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes an email to the newsletter list
    /// </summary>
    Task<bool> SubscribeToNewsletterAsync(string email, string? firstName = null, string? lastName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes an email from the newsletter list
    /// </summary>
    Task<bool> UnsubscribeFromNewsletterAsync(string email, CancellationToken cancellationToken = default);
}
