using Blog.Domain.Entities;
using Blog.Domain.Enums;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data; // Assuming namespace, will verify
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Blog.Web.Pages.Admin;

[Authorize(Roles = "Admin,Moderator")]
public class PostsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PostsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Blog.Domain.Entities.Post> Posts { get; set; } = new();
    public int TotalPosts { get; set; }
    public int PageSize { get; set; } = 20;
    
    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public PostStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Posts.Include(p => p.Author).AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(p => p.Title.Contains(SearchTerm) || (p.Author != null && p.Author.DisplayName.Contains(SearchTerm)));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(p => p.Status == StatusFilter.Value);
        }

        TotalPosts = await query.CountAsync();
        Posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUnpublishAsync(string id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return NotFound();

        post.Status = PostStatus.Unpublished;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Post unpublished successfully.";
        return RedirectToPage(new { PageIndex, SearchTerm, StatusFilter });
    }
    
    public async Task<IActionResult> OnPostPublishAsync(string id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return NotFound();

        // Only revert to Published if it was Unpublished? Or allow publishing drafts?
        // Admin power: Publish anything.
        post.Status = PostStatus.Published;
        if (!post.PublishedAt.HasValue) post.PublishedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        TempData["Success"] = "Post published successfully.";
        return RedirectToPage(new { PageIndex, SearchTerm, StatusFilter });
    }
}
