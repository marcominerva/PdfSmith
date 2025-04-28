using System.Globalization;
using PdfSmith.BusinessLayer.Exceptions;
using Scriban;
using Scriban.Runtime;

namespace PdfSmith.BusinessLayer.Templating;

public class ScribanTemplateEngine : ITemplateEngine
{
    public async Task<string> RenderAsync(string text, object model, CultureInfo culture, CancellationToken cancellationToken = default)
    {
        var template = Template.Parse(text);
        if (template.HasErrors)
        {
            throw new TemplateEngineException(template.Messages.ToString());
        }

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(new ScriptObject { { "Model", model } });
        context.PushCulture(culture);

        var result = await template.RenderAsync(context);
        return result;
    }
}
