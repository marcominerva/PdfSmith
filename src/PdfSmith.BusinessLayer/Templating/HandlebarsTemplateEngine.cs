using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using HandlebarsDotNet;
using PdfSmith.BusinessLayer.Exceptions;
using PdfSmith.BusinessLayer.Services;

namespace PdfSmith.BusinessLayer.Templating;

public partial class HandlebarsTemplateEngine(TimeZoneTimeProvider timeZoneTimeProvider) : ITemplateEngine
{
    private readonly IHandlebars _handlebars = Handlebars.Create();
    private static readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> TemplateCache = new();
    private bool _helpersRegistered = false;
    private readonly object _lockObject = new object();

    public async Task<string> RenderAsync(string template, object model, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        try
        {
            // Replace DateTime.Now with timezone-aware helper placeholders like other engines
            var sanitizedTemplate = DateTimeNowRegex.Replace(template, "{{dateTimeNow}}");
            sanitizedTemplate = DateTimeOffsetNowRegex.Replace(sanitizedTemplate, "{{dateTimeOffsetNow}}");

            // Register global helpers for date/time and culture-aware formatting (only once)
            if (!_helpersRegistered)
            {
                lock (_lockObject)
                {
                    if (!_helpersRegistered)
                    {
                        RegisterGlobalHelpers();
                        _helpersRegistered = true;
                    }
                }
            }

            // Get or compile template (with caching for performance)
            var compiledTemplate = TemplateCache.GetOrAdd(sanitizedTemplate, key =>
                _handlebars.Compile(key)
            );

            cancellationToken.ThrowIfCancellationRequested();

            // Pass the model directly (Handlebars expects the root model, not wrapped)
            var result = await Task.Run(() => compiledTemplate(model), cancellationToken);
            return result;
        }
        catch (HandlebarsException ex)
        {
            throw new TemplateEngineException(ex.Message, ex);
        }
        catch (Exception ex) when (ex is not TemplateEngineException)
        {
            throw new TemplateEngineException($"An error occurred while rendering the Handlebars template: {ex.Message}", ex);
        }
    }

    private void RegisterGlobalHelpers()
    {
        // Register helper for formatting currency
        _handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
        {
            if (arguments.Length > 0 && decimal.TryParse(arguments[0].ToString(), out var value))
            {
                // Use the current culture for formatting
                return value.ToString("C", CultureInfo.CurrentCulture);
            }
            return arguments.FirstOrDefault()?.ToString() ?? string.Empty;
        });

        // Register helper for formatting dates
        _handlebars.RegisterHelper("formatDate", (context, arguments) =>
        {
            if (arguments.Length > 0)
            {
                var dateValue = arguments[0];
                var format = arguments.Length > 1 ? arguments[1].ToString() : "yyyy-MM-dd";
                
                if (dateValue is DateTime dateTime)
                {
                    return dateTime.ToString(format, CultureInfo.CurrentCulture);
                }
                if (dateValue is DateTimeOffset dateTimeOffset)
                {
                    return dateTimeOffset.ToString(format, CultureInfo.CurrentCulture);
                }
                if (DateTime.TryParse(dateValue.ToString(), out var parsedDate))
                {
                    return parsedDate.ToString(format, CultureInfo.CurrentCulture);
                }
            }
            return arguments.FirstOrDefault()?.ToString() ?? string.Empty;
        });

        // Register helper for accessing DateTime.Now with timezone support
        _handlebars.RegisterHelper("dateTimeNow", (context, arguments) =>
        {
            return timeZoneTimeProvider.GetLocalNow().DateTime;
        });

        // Register helper for accessing DateTimeOffset.Now with timezone support
        _handlebars.RegisterHelper("dateTimeOffsetNow", (context, arguments) =>
        {
            return timeZoneTimeProvider.GetLocalNow();
        });

        // Register helper for mathematical operations
        _handlebars.RegisterHelper("multiply", (context, arguments) =>
        {
            if (arguments.Length >= 2 && 
                decimal.TryParse(arguments[0].ToString(), out var value1) &&
                decimal.TryParse(arguments[1].ToString(), out var value2))
            {
                return value1 * value2;
            }
            return 0m;
        });
    }

    [GeneratedRegex(@"(?<![\\w$])(?:@)?(?:System\.)?DateTime\.Now(?![\\w$])")]
    private static partial Regex DateTimeNowRegex { get; }

    [GeneratedRegex(@"(?<![\\w$])(?:@)?(?:System\.)?DateTimeOffset\.Now(?![\\w$])")]
    private static partial Regex DateTimeOffsetNowRegex { get; }
}