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
