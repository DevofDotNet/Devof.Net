using Blog.Application.DTOs;
using Blog.Application.Services;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Admin;

[Authorize(Policy = "AdminPolicy")]
public class IndexModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPostService _postService;

    public IndexModel(IUnitOfWork unitOfWork, IPostService postService)
    {
        _unitOfWork = unitOfWork;
        _postService = postService;
    }

    public DashboardStats Stats { get; set; } = new();
    public List<UserSummaryDto> RecentUsers { get; set; } = new();
    public List<PostDto> RecentPosts { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get stats
        Stats = new DashboardStats
        {
            TotalUsers = await _unitOfWork.Users.CountAsync(),
            TotalPosts = await _unitOfWork.Posts.CountPublishedAsync(),
            TotalComments = await _unitOfWork.Comments.CountAsync(),
            PendingReports = await _unitOfWork.Reports.CountPendingAsync()
        };

        // Get recent users
        var users = await _unitOfWork.Users.GetRecentAsync(5);
        RecentUsers = users.Select(u => new UserSummaryDto
        {
            UserId = u.Id,
            UserName = u.UserName!,
            DisplayName = u.DisplayName ?? u.UserName!,
            Email = u.Email!,
            AvatarUrl = u.AvatarUrl,
            JoinedAt = u.CreatedAt,
            IsActive = u.IsActive
        }).ToList();

        // Get recent posts
        var posts = await _postService.GetLatestAsync(1, 5, null);
        RecentPosts = posts.Items;
    }
}

public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int PendingReports { get; set; }
}

public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
}
