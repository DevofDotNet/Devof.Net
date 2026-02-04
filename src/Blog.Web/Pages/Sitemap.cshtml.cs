using Blog.Domain.Enums;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Blog.Web.Pages;

public class SitemapModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public SitemapModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<string> Urls { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}"; // Or use SiteUrl from config

        Urls.Add($"{baseUrl}/");
        Urls.Add($"{baseUrl}/Account/Login");
        Urls.Add($"{baseUrl}/Account/Register");
        Urls.Add($"{baseUrl}/Tag");

        var posts = await _context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => p.Slug)
            .ToListAsync();

        foreach (var slug in posts)
        {
            Urls.Add($"{baseUrl}/post/{slug}");
        }

        var tags = await _context.Tags
            .Select(t => t.Slug)
            .ToListAsync();

        foreach (var slug in tags)
        {
            Urls.Add($"{baseUrl}/tag/{slug}");
        }
        
        var authors = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => u.UserName)
            .ToListAsync();
            
        foreach (var username in authors)
        {
            Urls.Add($"{baseUrl}/author/{username}");
        }

        Response.ContentType = "application/xml";
        return Page();
    }
}
