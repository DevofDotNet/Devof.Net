using System.Text.RegularExpressions;

namespace Blog.Application.Services;

/// <summary>
/// Utility for parsing @mentions from content
/// </summary>
public static class MentionParser
{
    private static readonly Regex MentionRegex = new(@"@(\w+)", RegexOptions.Compiled);

    /// <summary>
    /// Extracts all @mentions from the given content
    /// </summary>
    /// <param name="content">Content to parse</param>
    /// <returns>List of usernames mentioned (without @ symbol)</returns>
    public static List<string> ExtractMentions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<string>();
        }

        var matches = MentionRegex.Matches(content);
        return matches.Select(m => m.Groups[1].Value.ToLower()).Distinct().ToList();
    }
}
