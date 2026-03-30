using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using OperationResults;
using PdfSmith.BusinessLayer.Exceptions;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating.Interfaces;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Services;

public class TemplateService(IServiceProvider serviceProvider, ITimeZoneService timeZoneService, IMarkdownConverter markdownConverter) : ITemplateService
{
    public async Task<Result<TemplateResponse>> CreateAsync(TemplateGenerationRequest request, CancellationToken cancellationToken)
    {
        var templateEngine = serviceProvider.GetKeyedService<ITemplateEngine>(request.TemplateEngine!.ToLowerInvariant().Trim());

        if (templateEngine is null)
        {
            return Result.Fail(FailureReasons.ClientError, "Unable to render the template", $"The template engine '{request.TemplateEngine}' has not been registered");
        }

        var timeZoneInfo = timeZoneService.GetTimeZone();

        if (timeZoneInfo is null)
        {
            var timeZoneId = timeZoneService.GetTimeZoneHeaderValue();
            if (timeZoneId is not null)
            {
                // If timeZoneInfo is null, but timeZoneId has a value, it means that the time zone specified in the header is invalid.
                return Result.Fail(FailureReasons.ClientError, "Unable to find the time zone", $"The time zone '{timeZoneId}' is invalid or is not available on the system");
            }
        }

        string? content;
        try
        {
            var model = request.Model?.ToExpandoObject(timeZoneInfo);

            cancellationToken.ThrowIfCancellationRequested();

            content = await templateEngine.RenderAsync(request.Template, model, CultureInfo.CurrentCulture, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (await markdownConverter.IsMarkdownAsync(content))
            {
                content = await markdownConverter.ConvertToHtmlAsync(content);
            }
        }
        catch (TemplateEngineException ex)
        {
            return Result.Fail(FailureReasons.ClientError, "Unable to render the template", ex.Message);
        }

        return new TemplateResponse(content);
    }
}
