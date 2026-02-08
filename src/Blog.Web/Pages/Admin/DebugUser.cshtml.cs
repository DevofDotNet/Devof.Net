using Blog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
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
        if (!string.IsNullOrEmpty(Email))
        {
            TargetUser = await _userManager.FindByEmailAsync(Email);
            if (TargetUser != null)
            {
                Roles = await _userManager.GetRolesAsync(TargetUser);
            }
        }
    }
}
