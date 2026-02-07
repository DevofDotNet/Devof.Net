using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Blog.Web.Pages;

public class ServerErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = 500;
    }
}
