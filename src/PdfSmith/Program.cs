using System.Globalization;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EntityFramework.Exceptions.SqlServer;
using FluentValidation;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MinimalHelpers.FluentValidation;
using OperationResults.AspNetCore.Http;
using PdfSmith.BusinessLayer.Authentication;
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
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSimpleAuthentication(builder.Configuration);
builder.Services.AddTransient<IApiKeyValidator, SubscriptionValidator>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseAzureSql(builder.Configuration.GetConnectionString("SqlConnection"));
    options.UseExceptionProcessor();
});

builder.Services.AddKeyedSingleton<ITemplateEngine, ScribanTemplateEngine>("scriban");
builder.Services.AddKeyedSingleton<ITemplateEngine, RazorTemplateEngine>("razor");

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
        var permitLimit = int.TryParse(context.User.Claims.FirstOrDefault(c => c.Type == "request_per_window")?.Value, out var requestPerWindow) ? requestPerWindow : 3;
        var window = int.TryParse(context.User.Claims.FirstOrDefault(c => c.Type == "window_minutes")?.Value, out var windowMinutes) ? TimeSpan.FromMinutes(windowMinutes) : TimeSpan.FromMinutes(1);

        return RateLimitPartition.GetFixedWindowLimiter(context.User.Identity?.Name ?? "Default", _ => new()
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

builder.Services.AddOpenApi(options =>
{
    options.AddSimpleAuthentication(builder.Configuration);
    options.AddAcceptLanguageHeader();
});

builder.Services.AddDefaultProblemDetails();
builder.Services.AddDefaultExceptionHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

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
.WithValidation<PdfGenerationRequest>()
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Application.Pdf)
.RequireAuthorization()
.RequireRateLimiting("PdfGeneration")
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

Microsoft.Playwright.Program.Main(["install", "chromium"]);

app.Run();

