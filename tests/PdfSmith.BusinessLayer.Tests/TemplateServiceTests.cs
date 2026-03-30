using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PdfSmith.BusinessLayer.Services;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating.Interfaces;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Tests;

public class TemplateServiceTests
{
    private readonly IMarkdownConverter markdownConverter = Substitute.For<IMarkdownConverter>();
    private readonly ITimeZoneService timeZoneService = Substitute.For<ITimeZoneService>();
    private readonly ITemplateEngine templateEngine = Substitute.For<ITemplateEngine>();

    private TemplateService CreateService()
    {
        var serviceProvider = Substitute.For<IServiceProvider, IKeyedServiceProvider>();
        ((IKeyedServiceProvider)serviceProvider).GetKeyedService(typeof(ITemplateEngine), "scriban").Returns(templateEngine);

        return new TemplateService(serviceProvider, timeZoneService, markdownConverter);
    }

    [Fact]
    public async Task CreateAsync_WhenContentIsMarkdown_ConvertsToHtml()
    {
        var service = CreateService();
        var markdownContent = "# Hello World";
        var expectedHtml = "<h1>Hello World</h1>\n";

        templateEngine.RenderAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(markdownContent));
        markdownConverter.IsMarkdownAsync(markdownContent).Returns(Task.FromResult(true));
        markdownConverter.ConvertToHtmlAsync(markdownContent).Returns(Task.FromResult(expectedHtml));
        timeZoneService.GetTimeZone().Returns(TimeZoneInfo.Utc);

        var request = new TemplateGenerationRequest("# Hello World", (JsonDocument?)null, "scriban");

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedHtml, result.Content!.Result);
        await markdownConverter.Received(1).ConvertToHtmlAsync(markdownContent);
    }

    [Fact]
    public async Task CreateAsync_WhenContentIsHtml_DoesNotConvert()
    {
        var service = CreateService();
        var htmlContent = "<html><body><h1>Hello</h1></body></html>";

        templateEngine.RenderAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(htmlContent));
        markdownConverter.IsMarkdownAsync(htmlContent).Returns(Task.FromResult(false));
        timeZoneService.GetTimeZone().Returns(TimeZoneInfo.Utc);

        var request = new TemplateGenerationRequest("<html><body><h1>Hello</h1></body></html>", (JsonDocument?)null, "scriban");

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(htmlContent, result.Content!.Result);
        await markdownConverter.DidNotReceive().ConvertToHtmlAsync(Arg.Any<string>());
    }
}
