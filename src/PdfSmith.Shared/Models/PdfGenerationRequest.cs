using System.ComponentModel;
using System.Text.Json;

namespace PdfSmith.Shared.Models;

public record class PdfGenerationRequest(string Template, JsonDocument Model, [property: DefaultValue("scriban")] string TemplateEngine = "scriban");
