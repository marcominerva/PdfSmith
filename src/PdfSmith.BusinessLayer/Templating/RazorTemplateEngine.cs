using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp.RuntimeBinder;
using PdfSmith.BusinessLayer.Exceptions;
using PdfSmith.BusinessLayer.Templating.Interfaces;
using RazorLight;
using RazorLight.Compilation;

namespace PdfSmith.BusinessLayer.Templating;

public partial class RazorTemplateEngine(IRazorLightEngine engine) : ITemplateEngine
{
    public async Task<string> RenderAsync(string template, object? model, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedTemplate = DateTimeNowRegex.Replace(template, "@requestTimeProvider.GetLocalNow().DateTime");
            sanitizedTemplate = DateTimeOffsetNowRegex.Replace(sanitizedTemplate, "@requestTimeProvider.GetLocalNow()");

            var content = $"""
                @using System
                @using System.Collections.Generic
                @using System.Linq
                @inject PdfSmith.BusinessLayer.Services.RequestTimeProvider requestTimeProvider
                {sanitizedTemplate}
                """;

            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
            var result = await engine.CompileRenderStringAsync(key, content, model);

            return result;
        }
        catch (TemplateCompilationException ex)
        {
            throw new TemplateEngineException(ex.Message, ex);
        }
        catch (RuntimeBinderException ex)
        {
            throw new TemplateEngineException("Template rendering failed. Ensure the model is not null and all referenced properties exist.", ex);
        }
    }

    [GeneratedRegex("(?<![\\w$])(?:@)?(?:System\\.)?DateTime\\.Now(?![\\w$])")]
    private static partial Regex DateTimeNowRegex { get; }

    [GeneratedRegex("(?<![\\w$])(?:@)?(?:System\\.)?DateTimeOffset\\.Now(?![\\w$])")]
    private static partial Regex DateTimeOffsetNowRegex { get; }
}

