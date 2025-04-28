using System.Globalization;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using OperationResults;
using PdfSmith.BusinessLayer.Exceptions;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Generators;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Services;

public class PdfService(IServiceProvider serviceProvider, IPdfGenerator pdfGenerator) : IPdfService
{
    public async Task<Result<StreamFileContent>> GeneratePdfAsync(PdfGenerationRequest request, CancellationToken cancellationToken)
    {
        var templateEngine = serviceProvider.GetKeyedService<ITemplateEngine>(request.TemplateEngine.ToLowerInvariant());

        if (templateEngine is null)
        {
            return Result.Fail(FailureReasons.ClientError, "Unable to render the template", $"The template engine {request.TemplateEngine} has not been registered");
        }

        string? content = null;
        try
        {
            var model = request.Model.ToExpandoObject();
            content = await templateEngine.RenderAsync(request.Template, model, CultureInfo.CurrentCulture, cancellationToken);
        }
        catch (TemplateEngineException ex)
        {
            return Result.Fail(FailureReasons.ClientError, "Unable to render the template", ex.Message);
        }

        var output = await pdfGenerator.CreateAsync(content, request.Options, cancellationToken);

        var streamFileContent = new StreamFileContent(output, MediaTypeNames.Application.Pdf, $"{Guid.CreateVersion7():N}.pdf");
        return streamFileContent;
    }
}
