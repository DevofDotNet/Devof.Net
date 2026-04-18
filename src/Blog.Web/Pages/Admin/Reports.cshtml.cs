using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Blog.Web.Pages.Admin;

[Authorize(Roles = "Admin,Moderator")]
public class ReportsModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public List<Report> Reports { get; set; } = new();
    public int TotalPending { get; set; }
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public async Task OnGetAsync()
    {
        TotalPending = await _unitOfWork.Reports.GetPendingCountAsync();
        var reports = await _unitOfWork.Reports.GetPendingAsync(PageIndex, PageSize);
        Reports = reports.ToList();
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetByIdAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Dismissed;
        report.ResolvedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        report.ResolvedAt = DateTime.UtcNow;

        await _unitOfWork.Reports.UpdateAsync(report);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Report dismissed.";
        return RedirectToPage(new { PageIndex });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, string? notes)
    {
        var report = await _unitOfWork.Reports.GetByIdAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Resolved;
        report.ModeratorNotes = notes;
        report.ResolvedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        report.ResolvedAt = DateTime.UtcNow;

        await _unitOfWork.Reports.UpdateAsync(report);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Report resolved.";
        return RedirectToPage(new { PageIndex });
    }
}
