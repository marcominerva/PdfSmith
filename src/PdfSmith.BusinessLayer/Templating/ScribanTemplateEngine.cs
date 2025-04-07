using System.Globalization;
using PdfSmith.Shared.Models;
using Scriban;
using Scriban.Runtime;

namespace PdfSmith.BusinessLayer.Templating;

public class ScribanTemplateEngine : ITemplateEngine
{
    public async Task<TemplateEngineResult> RenderAsync(string text, object model, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        var template = Template.Parse(text);
        if (template.HasErrors)
        {
            return new(null, template.Messages.ToString());
        }

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(new ScriptObject { { "Model", model } });
        context.PushCulture(culture);

        var result = await template.RenderAsync(context);
        return new(result, null);
    }
}
