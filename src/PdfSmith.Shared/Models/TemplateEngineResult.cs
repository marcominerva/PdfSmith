using System.Diagnostics.CodeAnalysis;

namespace PdfSmith.Shared.Models;

public record class TemplateEngineResult(string? RenderedText, string? ErrorMessage)
{
    [MemberNotNullWhen(true, nameof(ErrorMessage))]
    [MemberNotNullWhen(false, nameof(RenderedText))]
    public bool HasErrors => !string.IsNullOrWhiteSpace(ErrorMessage);
}
