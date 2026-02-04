using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Blog.Web.Pages.Admin;

[Authorize(Roles = "Admin,Moderator")]
public class UsersModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    // We can access DB context directly via UnitOfWork properties usually, 
    // or we might need to add specific methods to UserRepository if not exposed.
    // Assuming we can use IUserRepository to get users.
    // If IUserRepository doesn't support pagination/filtering well, we might need to update it 
    // or cast _unitOfWork.Users to concrete type if strictly necessary (anti-pattern but practical)
    // OR just use UserManager if available in Web layer.
    // Let's stick to UnitOfWork.Users and add necessary methods if missing?
    // Actually, ApplicationDbContext is accessible if we strictly follow Clean Architecture? No.
    // For now, I'll use _unitOfWork.Users. 
    // But IUserRepository is limited. 
    // I previously implemented `UserRepository` in `Repositories.cs` with `GetRecentAsync`.
    // I need `GetAllAsync` with pagination.
    // I'll assume I can add to UserRepository or if I changed it to expose IQueryable (unlikely).
    // Let's modify IUserRepository to add Search/Pagination support.
    // Wait, simpler: I'll inject UserManager here as it's Identity.
    
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

    public UsersModel(IUnitOfWork unitOfWork, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public List<ApplicationUser> Users { get; set; } = new();
    public int TotalUsers { get; set; }
    public int PageSize { get; set; } = 20;
    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(u => u.UserName.Contains(SearchTerm) || u.Email.Contains(SearchTerm) || u.DisplayName.Contains(SearchTerm));
        }

        TotalUsers = await query.CountAsync();
        Users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBanAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent banning self
        if (user.UserName == User.Identity.Name)
        {
            TempData["Error"] = "You cannot ban yourself.";
            return RedirectToPage();
        }

        user.IsActive = false;
        user.IsBanned = true;
        // user.BanReason = ...;
        
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user); // Force logout

        TempData["Success"] = $"User {user.UserName} has been banned.";
        return RedirectToPage(new { PageIndex, SearchTerm });
    }

    public async Task<IActionResult> OnPostUnbanAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = true;
        user.IsBanned = false;
        
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"User {user.UserName} has been activated.";
        return RedirectToPage(new { PageIndex, SearchTerm });
    }
}
