using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Web.Pages;

public class NotFoundModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = 404;
    }
}
