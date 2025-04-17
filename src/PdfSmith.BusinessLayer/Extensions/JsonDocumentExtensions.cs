using System.Dynamic;
using System.Globalization;
using System.Text.Json;

namespace PdfSmith.BusinessLayer.Extensions;

public static class JsonDocumentExtensions
{
    public static object ToExpandoObject(this JsonDocument document)
        => ConvertElement(document.RootElement);

    private static object ConvertElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in element.EnumerateObject())
            {
                expando[property.Name] = ConvertValue(property.Value)!;
            }

            return (ExpandoObject)expando!;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(ConvertValue).ToList();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported JsonValueKind: {element.ValueKind}");
        }
    }

    private static object? ConvertValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertElement(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertValue).ToList(),
            JsonValueKind.String => ParseStringValue(element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var number) ? number : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,

            _ => throw new NotSupportedException($"Unsupported JsonValueKind: {element.ValueKind}")
        };

        static object? ParseStringValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, out var dateTime))
            {
                return dateTime;
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dateTimeOffset))
            {
                return dateTimeOffset;
            }

            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var dateOnly))
            {
                return dateOnly;
            }

            if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var timeOnly))
            {
                return timeOnly;
            }

            return value;
        }
    }
}
