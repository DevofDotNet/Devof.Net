using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Blog.Application.Services;
using System.Text;
using System.Xml;

namespace Blog.Web.Pages;

public class FeedModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IConfiguration _configuration;

    public FeedModel(IPostService postService, IConfiguration configuration)
    {
        _postService = postService;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var siteUrl = _configuration["AppSettings:SiteUrl"] ?? "https://devof.net";
        var siteName = _configuration["AppSettings:SiteName"] ?? "Devof.NET";

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

            // RSS 2.0 format
            await writer.WriteStartElementAsync(null, "rss", null);
            await writer.WriteAttributeStringAsync(null, "version", null, "2.0");
            await writer.WriteAttributeStringAsync("xmlns", "atom", null, "http://www.w3.org/2005/Atom");
            await writer.WriteAttributeStringAsync("xmlns", "content", null, "http://purl.org/rss/1.0/modules/content/");

            await writer.WriteStartElementAsync(null, "channel", null);

            // Channel metadata
            await writer.WriteElementStringAsync(null, "title", null, siteName);
            await writer.WriteElementStringAsync(null, "link", null, siteUrl);
            await writer.WriteElementStringAsync(null, "description", null, "A community of developers sharing knowledge, building together.");
            await writer.WriteElementStringAsync(null, "language", null, "en-us");
            await writer.WriteElementStringAsync(null, "lastBuildDate", null, DateTime.UtcNow.ToString("R"));
            await writer.WriteElementStringAsync(null, "generator", null, "Devof.NET");

            // Atom self-link
            await writer.WriteStartElementAsync("atom", "link", "http://www.w3.org/2005/Atom");
            await writer.WriteAttributeStringAsync(null, "href", null, $"{siteUrl}/feed");
            await writer.WriteAttributeStringAsync(null, "rel", null, "self");
            await writer.WriteAttributeStringAsync(null, "type", null, "application/rss+xml");
            await writer.WriteEndElementAsync();

            // Image
            await writer.WriteStartElementAsync(null, "image", null);
            await writer.WriteElementStringAsync(null, "url", null, $"{siteUrl}/images/logo.png");
            await writer.WriteElementStringAsync(null, "title", null, siteName);
            await writer.WriteElementStringAsync(null, "link", null, siteUrl);
            await writer.WriteEndElementAsync();

            // Get latest posts
            var postsResult = await _postService.GetLatestAsync(1, 20, null);

            foreach (var post in postsResult.Items)
            {
                await writer.WriteStartElementAsync(null, "item", null);

                await writer.WriteElementStringAsync(null, "title", null, post.Title);
                await writer.WriteElementStringAsync(null, "link", null, $"{siteUrl}/Post/{post.Slug}");
                await writer.WriteElementStringAsync(null, "guid", null, $"{siteUrl}/Post/{post.Slug}");
                await writer.WriteElementStringAsync(null, "pubDate", null, (post.PublishedAt ?? post.CreatedAt).ToString("R"));
                await writer.WriteElementStringAsync(null, "author", null, post.Author?.DisplayName ?? post.Author?.UserName ?? "Unknown");

                // Description (excerpt)
                if (!string.IsNullOrEmpty(post.Excerpt))
                {
                    await writer.WriteElementStringAsync(null, "description", null, post.Excerpt);
                }

                // Categories (tags)
                if (post.Tags != null)
                {
                    foreach (var tag in post.Tags)
                    {
                        await writer.WriteElementStringAsync(null, "category", null, tag.Name);
                    }
                }

                // Cover image as enclosure
                if (!string.IsNullOrEmpty(post.CoverImageUrl))
                {
                    await writer.WriteStartElementAsync(null, "enclosure", null);
                    await writer.WriteAttributeStringAsync(null, "url", null, post.CoverImageUrl.StartsWith("http") ? post.CoverImageUrl : $"{siteUrl}{post.CoverImageUrl}");
                    await writer.WriteAttributeStringAsync(null, "type", null, "image/jpeg");
                    await writer.WriteAttributeStringAsync(null, "length", null, "0");
                    await writer.WriteEndElementAsync();
                }

                await writer.WriteEndElementAsync(); // item
            }

            await writer.WriteEndElementAsync(); // channel
            await writer.WriteEndElementAsync(); // rss
            await writer.WriteEndDocumentAsync();
        }

        stream.Position = 0;
        return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    }
}
