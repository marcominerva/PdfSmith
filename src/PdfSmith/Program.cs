using System.Globalization;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MinimalHelpers.FluentValidation;
using OperationResults.AspNetCore.Http;
using PdfSmith.BusinessLayer.Authentication;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Generators;
using PdfSmith.BusinessLayer.Services;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.BusinessLayer.Validations;
using PdfSmith.DataAccessLayer;
using PdfSmith.Shared.Models;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TimeZoneTimeProvider>();

builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSimpleAuthentication(builder.Configuration);
builder.Services.AddTransient<IApiKeyValidator, SubscriptionValidator>();

builder.Services.AddAzureSql<ApplicationDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));

builder.Services.AddRazorLightEngine();
builder.Services.AddHandlebarsEngine();

builder.Services.AddKeyedSingleton<ITemplateEngine, ScribanTemplateEngine>("scriban");
builder.Services.AddKeyedSingleton<ITemplateEngine, RazorTemplateEngine>("razor");
builder.Services.AddKeyedSingleton<ITemplateEngine, HandlebarsTemplateEngine>("handlebars");

builder.Services.AddSingleton<IPdfGenerator, ChromiumPdfGenerator>();
builder.Services.AddSingleton<IPdfService, PdfService>();

builder.Services.AddRequestLocalization(options =>
{
    var supportedCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.DefaultRequestCulture = new RequestCulture("en-US");
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("PdfGeneration", context =>
    {
        var permitLimit = int.TryParse(context.User.Claims.FirstOrDefault(c => c.Type == "requests_per_window")?.Value, out var requestPerWindow) ? requestPerWindow : 3;
        var window = int.TryParse(context.User.Claims.FirstOrDefault(c => c.Type == "window_minutes")?.Value, out var windowMinutes) ? TimeSpan.FromMinutes(windowMinutes) : TimeSpan.FromMinutes(1);

        return RateLimitPartition.GetFixedWindowLimiter(context.User.Identity?.Name ?? "Default", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window))
        {
            var response = context.HttpContext.Response;
            response.Headers.RetryAfter = window.TotalSeconds.ToString();
        }

        return ValueTask.CompletedTask;
    };
});

builder.Services.AddRequestTimeouts();

ValidatorOptions.Global.LanguageManager.Enabled = false;
builder.Services.AddValidatorsFromAssemblyContaining<PdfGenerationRequestValidator>();

builder.Services.AddOpenApiOperationParameters(options =>
{
    options.Parameters.Add(new()
    {
        Name = TimeZoneService.HeaderKey,
        In = ParameterLocation.Header,
        Required = false,
        Schema = OpenApiSchemaHelper.CreateStringSchema()
    });
});

builder.Services.AddOpenApi(options =>
{
    options.RemoveServerList();

    options.AddSimpleAuthentication(builder.Configuration);
    options.AddAcceptLanguageHeader();
    options.AddDefaultProblemDetailsResponse();

    options.AddOperationParameters();
});

builder.Services.AddDefaultProblemDetails();
builder.Services.AddDefaultExceptionHandler();

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", $"{app.Environment.ApplicationName} v1");
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
//app.UseCors();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapPost("/api/pdf", async (PdfGenerationRequest request, IPdfService pdfService, HttpContext httpContext) =>
{
    var result = await pdfService.GeneratePdfAsync(request, httpContext.RequestAborted);

    var response = httpContext.CreateResponse(result);
    return response;
})
.WithName("GeneratePdf")
.WithSummary("Dynamically generates a PDF document using a provided template and model")
.WithDescription("This endpoint accepts a template (as a string) and a model (as a JSON object) to generate a PDF document on the fly. The template can use the Razor, Scriban, or Handlebars engine, specified via the 'templateEngine' property. The model is injected into the template for dynamic content rendering. Additional PDF options and a custom file name can be provided. The result is a PDF file generated according to the submitted template and data.")
.WithValidation<PdfGenerationRequest>()
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Application.Pdf)
.RequireAuthorization()
.RequireRateLimiting("PdfGeneration")
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

// On Windows, it is installed in %USERPROFILE%\AppData\Local\ms-playwright by default
// We can use PLAYWRIGHT_BROWSERS_PATH environment variable to change the default location
Microsoft.Playwright.Program.Main(["install", "chromium"]);

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}