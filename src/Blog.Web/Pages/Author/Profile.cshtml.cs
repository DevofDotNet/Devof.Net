using Blog.Application.DTOs;
using Blog.Application.Services;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Generic;

namespace Blog.Web.Pages.Author;

public class ProfileModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IEngagementService _engagementService;
    private readonly IUnitOfWork _unitOfWork;

    public ProfileModel(
        IPostService postService,
        IEngagementService engagementService,
        IUnitOfWork unitOfWork)
    {
        _postService = postService;
        _engagementService = engagementService;
        _unitOfWork = unitOfWork;
    }

    public AuthorProfileDto? Profile { get; set; }
    public PagedResult<PostDto> Posts { get; set; } = new();
    public string JsonLd { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string username, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var user = await _unitOfWork.Users.GetByUsernameAsync(username);
        if (user == null)
            return Page();

        // Build profile DTO
        var followers = await _unitOfWork.Followers.GetFollowerCountAsync(user.Id);
        var following = await _unitOfWork.Followers.GetFollowingCountAsync(user.Id);
        var isFollowing = currentUserId != null && await _unitOfWork.Followers.IsFollowingAsync(currentUserId, user.Id);

        Profile = new AuthorProfileDto
        {
            UserId = user.Id,
            UserName = user.UserName!,
            DisplayName = user.DisplayName ?? user.UserName!,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            WebsiteUrl = user.WebsiteUrl,
            GitHubUrl = user.GitHubUrl,
            TwitterUrl = user.TwitterUrl,
            LinkedInUrl = user.LinkedInUrl,
            FollowerCount = followers,
            FollowingCount = following,
            IsFollowing = isFollowing
        };

        Posts = await _postService.GetByAuthorAsync(user.Id, page, 9, Domain.Enums.PostStatus.Published, currentUserId);
        Profile.PostCount = Posts.TotalCount;

        // Build JSON-LD with proper JSON escaping to prevent XSS
        var jsonLdObj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Person",
            ["name"] = Profile.DisplayName,
            ["url"] = $"https://devof.net/Author/{Profile.UserName}",
            ["image"] = string.IsNullOrEmpty(Profile.AvatarUrl) ? "https://devof.net/images/default-avatar.png" : Profile.AvatarUrl,
            ["description"] = string.IsNullOrEmpty(Profile.Bio) ? $"{Profile.DisplayName} is an author on Devof.NET" : Profile.Bio
        };
        var sameAs = new List<string?>();
        if (!string.IsNullOrEmpty(Profile.GitHubUrl)) sameAs.Add(Profile.GitHubUrl);
        if (!string.IsNullOrEmpty(Profile.TwitterUrl)) sameAs.Add(Profile.TwitterUrl);
        if (!string.IsNullOrEmpty(Profile.LinkedInUrl)) sameAs.Add(Profile.LinkedInUrl);
        if (sameAs.Count > 0) jsonLdObj["sameAs"] = sameAs;
        JsonLd = JsonSerializer.Serialize(jsonLdObj);

        return Page();
    }

    public async Task<IActionResult> OnPostFollowAsync(string username)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Author/{username}" });

        var user = await _unitOfWork.Users.GetByUsernameAsync(username);
        if (user == null)
            return NotFound();

        var isFollowing = await _unitOfWork.Followers.IsFollowingAsync(currentUserId, user.Id);
        
        if (isFollowing)
            await _engagementService.UnfollowAsync(currentUserId, user.Id);
        else
            await _engagementService.FollowAsync(currentUserId, user.Id);

        return RedirectToPage(new { username });
    }
}

public class AuthorProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowing { get; set; }
}
