using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages.Settings;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("Profile");
    }
}
