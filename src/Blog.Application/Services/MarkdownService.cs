using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoLinks;

namespace Blog.Application.Services;

public interface IMarkdownService
{
    string RenderToHtml(string markdown);
    string GenerateExcerpt(string markdown, int maxLength = 200);
    int CalculateReadingTime(string markdown);
}

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private static readonly Regex HtmlTagsRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesRegex = new Regex(@"\s+", RegexOptions.Compiled);

    public MarkdownService()
    {
        // Build pipeline with safety features and common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks(new AutoLinkOptions { OpenInNewWindow = true })
            .UseEmojiAndSmiley()
            .UseTaskLists()
            .DisableHtml() // XSS protection - disable raw HTML in markdown
            .Build();
    }

    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // Additional XSS sanitization - remove any script tags that might have slipped through
        html = SanitizeHtml(html);
        
        return html;
    }

    public string GenerateExcerpt(string markdown, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Convert to plain text
        var html = Markdown.ToHtml(markdown, _pipeline);
        var plainText = StripHtml(html);
        
        // Clean up whitespace
        plainText = MultipleSpacesRegex.Replace(plainText, " ").Trim();
        
        if (plainText.Length <= maxLength)
            return plainText;

        // Find the last space before maxLength to avoid cutting words
        var truncateIndex = plainText.LastIndexOf(' ', maxLength);
        if (truncateIndex == -1)
            truncateIndex = maxLength;

        return plainText.Substring(0, truncateIndex).Trim() + "...";
    }

    public int CalculateReadingTime(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return 1;

        var html = Markdown.ToHtml(markdown, _pipeline);
        var plainText = StripHtml(html);
        
        // Count words (split by whitespace)
        var words = plainText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Average reading speed is ~200-250 words per minute
        const int wordsPerMinute = 200;
        var readingTime = (int)Math.Ceiling((double)words.Length / wordsPerMinute);
        
        return Math.Max(1, readingTime); // Minimum 1 minute
    }

    private static string StripHtml(string html)
    {
        return HtmlTagsRegex.Replace(html, " ");
    }

    private static string SanitizeHtml(string html)
    {
        // Remove potentially dangerous content
        var sanitized = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
        
        return sanitized;
    }
}
