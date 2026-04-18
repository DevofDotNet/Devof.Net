using Blog.Application.DTOs;
using FluentValidation;

namespace Blog.Application.Validators;

public class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(50).WithMessage("Content must be at least 50 characters");

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(500).WithMessage("Cover image URL must not exceed 500 characters")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
            .WithMessage("Cover image must be a valid URL");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title must not exceed 200 characters");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description must not exceed 500 characters");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Count <= 5)
            .WithMessage("Maximum 5 tags allowed");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}

public class UpdatePostValidator : AbstractValidator<UpdatePostDto>
{
    public UpdatePostValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid post ID");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(50).WithMessage("Content must be at least 50 characters");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Count <= 5)
            .WithMessage("Maximum 5 tags allowed");
    }
}

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("Invalid post ID");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MaximumLength(5000).WithMessage("Comment must not exceed 5000 characters");
    }
}

public class UserProfileUpdateValidator : AbstractValidator<UserProfileUpdateDto>
{
    public UserProfileUpdateValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio must not exceed 500 characters");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(200).WithMessage("Website URL must not exceed 200 characters")
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Website must be a valid URL");

        RuleFor(x => x.GitHubUrl)
            .MaximumLength(200).WithMessage("GitHub URL must not exceed 200 characters")
            .Must(BeAValidGitHubUrl).When(x => !string.IsNullOrEmpty(x.GitHubUrl))
            .WithMessage("GitHub URL must be a valid GitHub profile URL (e.g., https://github.com/username)");

        RuleFor(x => x.TwitterUrl)
            .MaximumLength(200).WithMessage("Twitter URL must not exceed 200 characters")
            .Must(BeAValidTwitterUrl).When(x => !string.IsNullOrEmpty(x.TwitterUrl))
            .WithMessage("Twitter URL must be a valid Twitter/X profile URL (e.g., https://twitter.com/username)");

        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(200).WithMessage("LinkedIn URL must not exceed 200 characters")
            .Must(BeAValidLinkedInUrl).When(x => !string.IsNullOrEmpty(x.LinkedInUrl))
            .WithMessage("LinkedIn URL must be a valid LinkedIn profile URL (e.g., https://linkedin.com/in/username)");

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location must not exceed 100 characters");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    private static bool BeAValidHttpsUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool BeAValidGitHubUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        // Use EndsWith to prevent URLs like https://evil.com?github.com
        return host.EndsWith("github.com") || host.EndsWith("github.io");
    }

    private static bool BeAValidTwitterUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        // Use EndsWith to prevent URLs like https://evil.com?twitter.com
        return host.EndsWith("twitter.com") || host.EndsWith("x.com");
    }

    private static bool BeAValidLinkedInUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        // Use EndsWith to prevent URLs like https://evil.com?linkedin.com
        return host.EndsWith("linkedin.com");
    }
}

public class CreateReportValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(100).WithMessage("Reason must not exceed 100 characters");

        RuleFor(x => x.Details)
            .MaximumLength(1000).WithMessage("Details must not exceed 1000 characters");

        RuleFor(x => x)
            .Must(x => x.PostId.HasValue || x.CommentId.HasValue || !string.IsNullOrEmpty(x.UserId))
            .WithMessage("You must specify what you are reporting (post, comment, or user)");
    }
}
