using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scriban.Runtime;
using Scriban;
using static System.Net.Mime.MediaTypeNames;
using PdfSmith.Shared.Models;

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
