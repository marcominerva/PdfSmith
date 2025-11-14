using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using MinimalHelpers.FluentValidation;
using OperationResults.AspNetCore.Http;
using PdfSmith.BackgroundServices;
using PdfSmith.BusinessLayer.Authentication;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Generators;
using PdfSmith.BusinessLayer.Generators.Interfaces;
using PdfSmith.BusinessLayer.Services;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.BusinessLayer.Templating.Interfaces;
using PdfSmith.BusinessLayer.Validations;
using PdfSmith.DataAccessLayer;
using PdfSmith.HealthChecks;
using PdfSmith.Logging;
using PdfSmith.Shared.Models;
using Serilog;
using Serilog.Core;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    loggerConfiguration.ReadFrom.Services(services);
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ILogEventEnricher, HttpContextEnricher>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RequestTimeProvider>();

builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSimpleAuthentication(builder.Configuration);
builder.Services.AddTransient<IApiKeyValidator, SubscriptionValidator>();

builder.Services.AddAzureSql<ApplicationDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));

builder.Services.AddRazorLightEngine();

builder.Services.AddKeyedSingleton<ITemplateEngine, ScribanTemplateEngine>("scriban");
builder.Services.AddKeyedSingleton<ITemplateEngine, RazorTemplateEngine>("razor");
builder.Services.AddKeyedSingleton<ITemplateEngine, HandlebarsTemplateEngine>("handlebars");

builder.Services.AddSingleton<ITemplateService, TemplateService>();
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

builder.Services.AddSingleton<PlaywrightHealthCheck>();
builder.Services.AddHostedService<InstallPlaywrightBackgroundService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database", tags: ["ready"])
    .AddCheck<PlaywrightHealthCheck>("Playwright", tags: ["ready"]);

if (builder.Environment.IsProduction())
{
    // Add OpenTelemetry and configure it to use Azure Monitor.
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

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

app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
});

app.UseAuthorization();

app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthChecksResponseWriter()
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
    ResponseWriter = HealthChecksResponseWriter()
});

app.MapPost("/api/template", async (TemplateGenerationRequest request, ITemplateService templateService, HttpContext httpContext) =>
{
    var result = await templateService.CreateAsync(request, httpContext.RequestAborted);

    var response = httpContext.CreateResponse(result);
    return response;
})
.WithName("CreateTemplate")
.WithSummary("Renders a template to HTML using the specified template engine")
.WithDescription("Accepts a template (string) and a model (JSON) and returns the rendered HTML as a string. Supports Razor, Scriban, and Handlebars via the 'templateEngine' property. The template is rendered using the request culture and optional time zone header. Useful to preview or validate templates before generating a PDF.")
.WithValidation<TemplateGenerationRequest>()
.Produces<TemplateResponse>(StatusCodes.Status200OK)
.RequireAuthorization()
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(5),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

app.MapPost("/api/pdf", async (PdfGenerationRequest request, IPdfService pdfService, HttpContext httpContext) =>
{
    var result = await pdfService.GeneratePdfAsync(request, httpContext.RequestAborted);

    var response = httpContext.CreateResponse(result);
    return response;
})
.WithName("GeneratePdf")
.WithSummary("Renders a template and generates a PDF using the specified template engine")
.WithDescription("Accepts a template (string) and a model (JSON) and returns a generated PDF document. Supports Razor, Scriban, and Handlebars via the 'templateEngine' property. The model is injected into the template for dynamic content rendering. Additional PDF options and a custom file name can be provided. The template is rendered using the request culture and optional time zone header.")
.WithValidation<PdfGenerationRequest>()
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Application.Pdf)
.RequireAuthorization()
.RequireRateLimiting("PdfGeneration")
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

static Func<HttpContext, HealthReport, Task> HealthChecksResponseWriter()
    => async (context, report) =>
    {
        var result = JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                duration = report.TotalDuration.TotalMilliseconds,
                details = report.Entries.Select(entry => new
                {
                    service = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                })
            });

        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(result);
    };

