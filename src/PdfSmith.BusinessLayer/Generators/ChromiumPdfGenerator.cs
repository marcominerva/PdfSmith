using Microsoft.Playwright;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Generators;

public class ChromiumPdfGenerator : IPdfGenerator
{
    public async Task<Stream> CreateAsync(string content, PdfOptions? options, CancellationToken cancellationToken = default)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });

        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Set the content of the page to the rendered HTML
        await page.SetContentAsync(content);

        // Generate the PDF
        var pdfOptions = options ?? new();
        var output = await page.PdfAsync(new()
        {
            Format = pdfOptions.PageSize ?? "A4",
            Landscape = pdfOptions.Orientation is PdfOrientation.Landscape,
            PrintBackground = true,
            Margin = pdfOptions.Margins is not null ? new()
            {
                Top = $"{pdfOptions.Margins.Top}px",
                Bottom = $"{pdfOptions.Margins.Bottom}px",
                Left = $"{pdfOptions.Margins.Left}px",
                Right = $"{pdfOptions.Margins.Right}px"
            }
            : null,
        });

        await context.CloseAsync();
        await browser.CloseAsync();

        var result = new MemoryStream(output);
        return result;
    }
}
