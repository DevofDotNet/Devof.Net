using Blog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Blog.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DebugUserModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DebugUserModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string Email { get; set; }

    public ApplicationUser TargetUser { get; set; }
    public IList<string> Roles { get; set; }

    public async Task OnGetAsync()
    {
        // Only allow access in Development environment or local requests
        if (!IsLocalRequest() && !IsDevelopmentEnvironment())
        {
            Response.StatusCode = 403;
            return;
        }

        if (!string.IsNullOrEmpty(Email))
        {
            TargetUser = await _userManager.FindByEmailAsync(Email);
            if (TargetUser != null)
            {
                Roles = await _userManager.GetRolesAsync(TargetUser);
            }
        }
    }

    private bool IsLocalRequest()
    {
        var connection = HttpContext.Connection;
        if (connection.RemoteIpAddress == null) return true;
        if (connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            return IPAddress.IsLoopback(connection.RemoteIpAddress);
        return false;
    }

    private bool IsDevelopmentEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}