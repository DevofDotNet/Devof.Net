using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Blog.Domain.Interfaces;
using System.Text;
using System.Xml;

namespace Blog.Web.Pages;

public class SitemapModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public SitemapModel(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var siteUrl = _configuration["AppSettings:SiteUrl"] ?? "https://devof.net";

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            Async = true
        };

        var stream = new MemoryStream();
        await using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Homepage
            await WriteUrlAsync(writer, siteUrl, DateTime.UtcNow, "daily", "1.0");

            // Static pages
            await WriteUrlAsync(writer, $"{siteUrl}/About", DateTime.UtcNow, "monthly", "0.8");
            await WriteUrlAsync(writer, $"{siteUrl}/Privacy", DateTime.UtcNow, "monthly", "0.5");
            await WriteUrlAsync(writer, $"{siteUrl}/Terms", DateTime.UtcNow, "monthly", "0.5");
            await WriteUrlAsync(writer, $"{siteUrl}/Tag", DateTime.UtcNow, "weekly", "0.8");

            // Published posts
            var posts = await _unitOfWork.Posts.GetAllPublishedAsync(1, 10000);
            foreach (var post in posts)
            {
                var lastMod = post.UpdatedAt ?? post.PublishedAt ?? post.CreatedAt;
                await WriteUrlAsync(writer, $"{siteUrl}/Post/{post.Slug}", lastMod, "weekly", "0.9");
            }

            // Tags
            var tags = await _unitOfWork.Tags.GetAllAsync();
            foreach (var tag in tags)
            {
                await WriteUrlAsync(writer, $"{siteUrl}/Tag/{tag.Slug}", DateTime.UtcNow, "weekly", "0.7");
            }

            // Author profiles (get distinct authors)
            var authors = posts.Where(p => p.Author != null).Select(p => p.Author).DistinctBy(a => a!.Id);
            foreach (var author in authors)
            {
                await WriteUrlAsync(writer, $"{siteUrl}/Author/{author.UserName}", DateTime.UtcNow, "weekly", "0.6");
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
        }

        stream.Position = 0;
        return File(stream.ToArray(), "application/xml", "sitemap.xml");
    }

    private static async Task WriteUrlAsync(XmlWriter writer, string loc, DateTime lastMod, string changeFreq, string priority)
    {
        await writer.WriteStartElementAsync(null, "url", null);

        await writer.WriteStartElementAsync(null, "loc", null);
        await writer.WriteStringAsync(loc);
        await writer.WriteEndElementAsync();

        await writer.WriteStartElementAsync(null, "lastmod", null);
        await writer.WriteStringAsync(lastMod.ToString("yyyy-MM-dd"));
        await writer.WriteEndElementAsync();

        await writer.WriteStartElementAsync(null, "changefreq", null);
        await writer.WriteStringAsync(changeFreq);
        await writer.WriteEndElementAsync();

        await writer.WriteStartElementAsync(null, "priority", null);
        await writer.WriteStringAsync(priority);
        await writer.WriteEndElementAsync();

        await writer.WriteEndElementAsync();
    }
}
