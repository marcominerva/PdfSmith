using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfSmith.Shared.Models;

[method: JsonConstructor]
public record class PdfGenerationRequest(string Template, JsonDocument Model, PdfOptions? Options = null, [property: DefaultValue("scriban")] string TemplateEngine = "scriban")
{
    public PdfGenerationRequest(string template, object model, PdfOptions? options = null, string templateEngine = "scriban")
        : this(template, ToJsonDocument(model), options, templateEngine)
    {
    }

    private static JsonDocument ToJsonDocument(object model)
    {
        var jsonString = JsonSerializer.Serialize(model, JsonSerializerOptions.Default);
        return JsonDocument.Parse(jsonString);
    }
}

public record class PdfOptions(string PageSize = "A4", PdfOrientation Orientation = PdfOrientation.Portrait, PdfMargins? Margins = null);

public record class PdfMargins(string Top = "50px", string Bottom = "50px", string Left = "50px", string Right = "50px");

public enum PdfOrientation
{
    Portrait,
    Landscape
}