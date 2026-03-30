namespace PdfSmith.BusinessLayer.Templating.Interfaces;

/// <summary>
/// Provides functionality to detect and convert Markdown content to HTML.
/// </summary>
public interface IMarkdownConverter
{
    /// <summary>
    /// Determines whether the specified content is in Markdown format rather than HTML.
    /// </summary>
    /// <param name="content">
    /// The rendered template content to evaluate. May be <see langword="null"/>, in which case the method returns
    /// <see langword="false"/>.
    /// </param>
    /// <returns>A task that resolves to <see langword="true"/> if the content appears to be Markdown; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsMarkdownAsync(string? content);

    /// <summary>
    /// Converts the specified Markdown content to HTML.
    /// </summary>
    /// <param name="markdown">The Markdown content to convert.</param>
    /// <returns>A task that resolves to the resulting HTML string.</returns>
    Task<string> ConvertToHtmlAsync(string markdown);
}
