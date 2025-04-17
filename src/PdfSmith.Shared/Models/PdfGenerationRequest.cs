using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfSmith.Shared.Models;

[method: JsonConstructor]
public record class PdfGenerationRequest(string Template, JsonDocument Model, [property: DefaultValue("scriban")] string TemplateEngine = "scriban")
{
    public PdfGenerationRequest(string template, object model, string templateEngine = "scriban")
        : this(template, ToJsonDocument(model), templateEngine)
    {
    }

    private static JsonDocument ToJsonDocument(object model)
    {
        var jsonString = JsonSerializer.Serialize(model, JsonSerializerOptions.Default);
        return JsonDocument.Parse(jsonString);
    }
}
