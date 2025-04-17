using System.Globalization;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using OperationResults;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Services;

public class PdfGeneratorService(IServiceProvider serviceProvider) : IPdfGeneratorService
{
    public async Task<Result<ByteArrayFileContent>> GeneratePdfAsync(PdfGenerationRequest request, CancellationToken cancellationToken)
    {
        var templateEngine = serviceProvider.GetRequiredKeyedService<ITemplateEngine>(request.TemplateEngine.ToLowerInvariant());

        var model = request.Model.ToExpandoObject();
        var result = await templateEngine.RenderAsync(request.Template, model, CultureInfo.CurrentCulture, cancellationToken);

        if (result.HasErrors)
        {
            return Result.Fail(FailureReasons.ClientError, "Unable to render the template", result.ErrorMessage);
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });

        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Set the content of the page to the rendered HTML
        await page.SetContentAsync(result.RenderedText);

        // Generate the PDF
        var output = await page.PdfAsync(new()
        {
            Format = "A4", // or "letter"
            Landscape = false,
        });

        var byteArrayFileContent = new ByteArrayFileContent(output, MediaTypeNames.Application.Pdf, $"{Guid.CreateVersion7():N}.pdf");
        return byteArrayFileContent;
    }
}
