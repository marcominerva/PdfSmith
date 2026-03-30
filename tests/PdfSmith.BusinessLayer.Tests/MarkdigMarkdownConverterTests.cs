using PdfSmith.BusinessLayer.Templating;

namespace PdfSmith.BusinessLayer.Tests;

public class MarkdigMarkdownConverterTests
{
    private readonly MarkdigMarkdownConverter converter = new();

    [Theory]
    [InlineData("# Hello World")]
    [InlineData("## Subheading")]
    [InlineData("- Item 1\n- Item 2")]
    [InlineData("* Bold list item")]
    [InlineData("1. First\n2. Second")]
    [InlineData("> A blockquote")]
    [InlineData("[Link](https://example.com)")]
    [InlineData("**bold text**")]
    [InlineData("Some `inline code` here")]
    public async Task IsMarkdownAsync_WithMarkdownContent_ReturnsTrue(string content)
    {
        var result = await converter.IsMarkdownAsync(content);

        Assert.True(result);
    }

    [Theory]
    [InlineData("<html><body>Hello</body></html>")]
    [InlineData("<div>Hello World</div>")]
    [InlineData("<table><tr><td>Cell</td></tr></table>")]
    [InlineData("<!DOCTYPE html><html></html>")]
    [InlineData("<body><h1>Title</h1></body>")]
    [InlineData("<section><p>Text</p></section>")]
    public async Task IsMarkdownAsync_WithHtmlContent_ReturnsFalse(string content)
    {
        var result = await converter.IsMarkdownAsync(content);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task IsMarkdownAsync_WithEmptyOrWhitespace_ReturnsFalse(string? content)
    {
        var result = await converter.IsMarkdownAsync(content);

        Assert.False(result);
    }

    [Fact]
    public async Task IsMarkdownAsync_WithPlainTextWithoutMarkdownSyntax_ReturnsFalse()
    {
        var result = await converter.IsMarkdownAsync("Just plain text without any special formatting");

        Assert.False(result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithHeading_ProducesHtmlHeading()
    {
        var result = await converter.ConvertToHtmlAsync("# Hello World");

        Assert.Contains("<h1", result);
        Assert.Contains("Hello World</h1>", result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithBoldText_ProducesStrongTag()
    {
        var result = await converter.ConvertToHtmlAsync("**bold**");

        Assert.Contains("<strong>bold</strong>", result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithUnorderedList_ProducesHtmlList()
    {
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        var result = await converter.ConvertToHtmlAsync(markdown);

        Assert.Contains("<ul>", result);
        Assert.Contains("<li>Item 1</li>", result);
        Assert.Contains("<li>Item 2</li>", result);
        Assert.Contains("<li>Item 3</li>", result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithLink_ProducesAnchorTag()
    {
        var result = await converter.ConvertToHtmlAsync("[Example](https://example.com)");

        Assert.Contains("<a href=\"https://example.com\">Example</a>", result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithNullInput_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => converter.ConvertToHtmlAsync(null!));
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithCodeBlock_ProducesCodeTag()
    {
        var markdown = """
            ```
            var x = 1;
            ```
            """;

        var result = await converter.ConvertToHtmlAsync(markdown);

        Assert.Contains("<code>", result);
    }

    [Fact]
    public async Task ConvertToHtmlAsync_WithBlockquote_ProducesBlockquoteTag()
    {
        var result = await converter.ConvertToHtmlAsync("> This is a quote");

        Assert.Contains("<blockquote>", result);
    }
}
