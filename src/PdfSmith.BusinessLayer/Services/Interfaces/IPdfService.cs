using OperationResults;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Services.Interfaces;

public interface IPdfService
{
    Task<Result<StreamFileContent>> GeneratePdfAsync(PdfGenerationRequest request, CancellationToken cancellationToken);
}