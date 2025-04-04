using System.Text.Json;

namespace PdfSmith.Shared.Models;

public record class PdfGenerationRequest(string Template, JsonDocument Model, string TemplateEngine);
