using Blog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text;

namespace Blog.Web.Pages.Settings;

[Authorize]
public class DataExportModel : PageModel
{
    private readonly IDataExportService _dataExportService;

    public DataExportModel(IDataExportService dataExportService)
    {
        _dataExportService = dataExportService;
    }

    public bool ExportRequested { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        var json = await _dataExportService.ExportUserDataAsync(userId);
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"devof-net-data-export-{DateTime.UtcNow:yyyyMMdd}.json");
    }
}
