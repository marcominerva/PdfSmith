using System.Globalization;
using PdfSmith.Shared.Models;

namespace PdfSmith.BusinessLayer.Templating;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string template, object model, CultureInfo culture, CancellationToken cancellationToken = default);
}
