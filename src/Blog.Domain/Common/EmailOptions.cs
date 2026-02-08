namespace Blog.Domain.Common;

public class EmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string NewsletterListId { get; set; } = string.Empty;
}
