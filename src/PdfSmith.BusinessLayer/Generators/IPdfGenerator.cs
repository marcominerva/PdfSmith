using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Generators;

public interface IPdfGenerator
{
    Task<Stream> CreateAsync(string content, PdfOptions? options, CancellationToken cancellationToken = default);
}
