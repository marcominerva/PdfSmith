using System.Globalization;
using HandlebarsDotNet;
using PdfSmith.BusinessLayer.Exceptions;
using PdfSmith.BusinessLayer.Services;
using PdfSmith.BusinessLayer.Templating.Interfaces;

namespace PdfSmith.BusinessLayer.Templating;

public class HandlebarsTemplateEngine(ClientTimeProvider requestTimeProvider) : ITemplateEngine
{
    private readonly Lazy<IHandlebars> handlebarsInstance = new(() => CreateHandlebarsInstance(requestTimeProvider));

    public Task<string> RenderAsync(string template, object? model, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        try
        {
            var handlebars = handlebarsInstance.Value;
            var compiledTemplate = handlebars.Compile(template);

            cancellationToken.ThrowIfCancellationRequested();

            var result = compiledTemplate(new { Model = model });
            return Task.FromResult(result);
        }
        catch (HandlebarsException ex)
        {
            throw new TemplateEngineException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new TemplateEngineException($"An error occurred while rendering the Handlebars template: {ex.Message}", ex);
        }
    }

    private static IHandlebars CreateHandlebarsInstance(ClientTimeProvider requestTimeProvider)
    {
        var handlebars = Handlebars.Create();

        // Register helper for formatting values.
        handlebars.RegisterHelper("formatNumber", (context, arguments) =>
            arguments.Length switch
            {
                0 => string.Empty,
                >= 2 when decimal.TryParse(arguments[0].ToString(), CultureInfo.CurrentCulture, out var value)
                    => value.ToString(arguments[1].ToString(), CultureInfo.CurrentCulture),
                >= 1 when decimal.TryParse(arguments[0].ToString(), CultureInfo.CurrentCulture, out var value)
                    => value.ToString(CultureInfo.CurrentCulture),
                _ => arguments[0]
            });

        handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
            arguments.Length switch
            {
                0 => string.Empty,
                >= 1 when decimal.TryParse(arguments[0].ToString(), CultureInfo.CurrentCulture, out var value)
                    => value.ToString("C", CultureInfo.CurrentCulture),
                _ => arguments.FirstOrDefault()?.ToString() ?? string.Empty
            });

        handlebars.RegisterHelper("formatDate", (context, arguments) =>
        {
            if (arguments.Length == 0)
            {
                return string.Empty;
            }

            var dateValue = arguments[0];
            var format = arguments.ElementAtOrDefault(1)?.ToString() ??
                $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}";

            if (dateValue is DateTime dateTime)
            {
                return dateTime.ToString(format, CultureInfo.CurrentCulture);
            }

            if (dateValue is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString(format, CultureInfo.CurrentCulture);
            }

            if (DateTime.TryParse(dateValue.ToString(), CultureInfo.CurrentCulture, out var parsedDate))
            {
                return parsedDate.ToString(format, CultureInfo.CurrentCulture);
            }

            return arguments.FirstOrDefault()?.ToString() ?? string.Empty;
        });

        // Register helpers for getting current date/time in the correct timezone.
        handlebars.RegisterHelper("now", (context, arguments) =>
        {
            var format = arguments.ElementAtOrDefault(0)?.ToString() ??
                $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}";

            var now = requestTimeProvider.GetLocalNow().DateTime;
            return now.ToString(format, CultureInfo.CurrentCulture);
        });

        handlebars.RegisterHelper("utcNow", (context, arguments) =>
        {
            var format = arguments.ElementAtOrDefault(0)?.ToString() ??
                $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}";

            var now = requestTimeProvider.GetUtcNow().DateTime;
            return now.ToString(format, CultureInfo.CurrentCulture);
        });

        // Register helper for mathematical operations.
        handlebars.RegisterHelper("add", (context, arguments) => GetValues(arguments, out var value1, out var value2) ? value1 + value2 : string.Empty);
        handlebars.RegisterHelper("multiply", (context, arguments) => GetValues(arguments, out var value1, out var value2) ? value1 * value2 : string.Empty);
        handlebars.RegisterHelper("subtract", (context, arguments) => GetValues(arguments, out var value1, out var value2) ? value1 - value2 : string.Empty);
        handlebars.RegisterHelper("divide", (context, arguments) => GetValues(arguments, out var value1, out var value2) ? value1 / value2 : string.Empty);

        handlebars.RegisterHelper("round", (context, arguments) =>
        {
            if (arguments.Length == 0 ||
                !decimal.TryParse(arguments[0].ToString(), CultureInfo.CurrentCulture, out var value1))
            {
                return string.Empty;
            }

            var decimals = 0;
            if (arguments.Length >= 2)
            {
                int.TryParse(arguments[1].ToString(), CultureInfo.CurrentCulture, out decimals);
            }

            return Math.Round(value1, decimals, MidpointRounding.AwayFromZero);
        });

        return handlebars;

        static bool GetValues(Arguments arguments, out decimal value1, out decimal value2)
        {
            value1 = value2 = 0;

            if (arguments.Length < 2)
            {
                return false;
            }

            if (decimal.TryParse(arguments[0].ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out value1) &&
                decimal.TryParse(arguments[1].ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out value2))
            {
                return true;
            }

            return false;
        }
    }
}