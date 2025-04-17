using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using OperationResults;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Services;

public class PdfGeneratorService(IServiceProvider serviceProvider) : IPdfGeneratorService
{
    public async Task<Result<StreamFileContent>> GeneratePdfAsync(PdfGenerationRequest request, CancellationToken cancellationToken)
    {
        var templateEngine = serviceProvider.GetRequiredKeyedService<ITemplateEngine>(request.TemplateEngine.ToLowerInvariant());

        var model = request.Model.ToExpandoObject();
        var result = await templateEngine.RenderAsync(request.Template, model, CultureInfo.CurrentCulture, cancellationToken);

        return Result.Fail(FailureReasons.ItemNotFound);
    }
}
