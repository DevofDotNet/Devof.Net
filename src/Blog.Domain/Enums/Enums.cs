namespace Blog.Domain.Enums;

public enum PostStatus
{
    Draft = 0,
    Published = 1,
    Unpublished = 2,
    Archived = 3
}

public enum ReportStatus
{
    Pending = 0,
    Reviewed = 1,
    Resolved = 2,
    Dismissed = 3
}

public enum ReportType
{
    Post = 0,
    Comment = 1,
    User = 2
}

public enum NotificationType
{
    Mention = 0,
    Reply = 1,
    Follow = 2,
    Like = 3,
    Comment = 4,
    System = 5
}

public enum ConsentType
{
    Essential = 0,
    Analytics = 1,
    Marketing = 2
}

