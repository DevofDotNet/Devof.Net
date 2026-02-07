using Blog.Domain.Common;

namespace Blog.Domain.Entities;

public class Subscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; } = false;
    public string? ConfirmationToken { get; set; }
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
