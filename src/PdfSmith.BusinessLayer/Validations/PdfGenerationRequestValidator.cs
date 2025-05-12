using FluentValidation;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Validations;

public class PdfGenerationRequestValidator : AbstractValidator<PdfGenerationRequest>
{
    public PdfGenerationRequestValidator()
    {
        RuleFor(r => r.Template).NotEmpty();
        RuleFor(r => r.TemplateEngine).NotEmpty();
    }
}
