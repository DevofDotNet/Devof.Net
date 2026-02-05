using Blog.Domain.Common;
using Blog.Domain.Enums;

namespace Blog.Domain.Entities;

public class Report : BaseEntity
{
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public string? ModeratorNotes { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
    public string? ResolvedById { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Reporter
    public string ReporterId { get; set; } = string.Empty;
    public virtual ApplicationUser Reporter { get; set; } = null!;

    // Reported User (optional - for user reports)
    public string? ReportedUserId { get; set; }
    public virtual ApplicationUser? ReportedUser { get; set; }

    // Reported Post (optional - for post reports)
    public int? ReportedPostId { get; set; }
    public virtual Post? ReportedPost { get; set; }

    // Reported Comment (optional - for comment reports)
    public int? ReportedCommentId { get; set; }
    public virtual Comment? ReportedComment { get; set; }
}
