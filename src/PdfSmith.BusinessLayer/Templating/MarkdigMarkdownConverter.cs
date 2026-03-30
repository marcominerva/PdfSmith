using System.Text.RegularExpressions;
using Markdig;
using PdfSmith.BusinessLayer.Templating.Interfaces;

namespace PdfSmith.BusinessLayer.Templating;

/// <summary>
/// Converts Markdown content to HTML using the Markdig library.
/// </summary>
public partial class MarkdigMarkdownConverter : IMarkdownConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <inheritdoc />
    public Task<bool> IsMarkdownAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(false);
        }

        var trimmed = content.TrimStart();

        if (HtmlBlockTagRegex.IsMatch(trimmed))
        {
            return Task.FromResult(false);
        }

        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(MarkdownPatternRegex.IsMatch(content));
    }

    /// <inheritdoc />
    public Task<string> ConvertToHtmlAsync(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var html = Markdown.ToHtml(markdown, Pipeline);
        return Task.FromResult(html);
    }

    [GeneratedRegex(@"^\s*<(html|head|body|div|section|article|header|footer|nav|main|table|form|script|style|link|meta)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex HtmlBlockTagRegex { get; }

    // Matches common Markdown syntax: headings (# ), horizontal rules (*** or ---), unordered lists (-, *, +),
    // ordered lists (1.), blockquotes (>), links ([text](url)), emphasis (*text*, **text**), and inline code (`code`).
    [GeneratedRegex(@"(^#{1,6}\s)|(^\*{3,}$)|(^-{3,}$)|(^\s*[-*+]\s)|(^\s*\d+\.\s)|(^\s*>)|\[.+\]\(.+\)|(\*{1,2}.+\*{1,2})|(_{1,2}.+_{1,2})|(`.+`)", RegexOptions.Multiline)]
    private static partial Regex MarkdownPatternRegex { get; }
}
