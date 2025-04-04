namespace PdfSmith.Shared.Models;

public record class TemplateEngineResult(string? RenderedText, string? ErrorMessage)
{
    public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);
}
