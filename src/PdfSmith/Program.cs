using System.Globalization;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Localization;
using OperationResults.AspNetCore.Http;
using PdfSmith.BusinessLayer.Services;
using PdfSmith.BusinessLayer.Services.Interfaces;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.Shared.Models;
using SimpleAuthentication;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSimpleAuthentication(builder.Configuration);

builder.Services.AddKeyedSingleton<ITemplateEngine, ScribanTemplateEngine>("scriban");
builder.Services.AddSingleton<IPdfGeneratorService, PdfGeneratorService>();

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
        return RateLimitPartition.GetFixedWindowLimiter(context.User.Identity?.Name ?? "Default", _ => new()
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(30),
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

app.MapPost("/api/pdf", async (PdfGenerationRequest request, IPdfGeneratorService pdfGeneratorService, HttpContext httpContext) =>
{
    var result = await pdfGeneratorService.GeneratePdfAsync(request, httpContext.RequestAborted);

    var response = httpContext.CreateResponse(result);
    return response;
})
.Accepts<PdfGenerationRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Application.Pdf)
.RequireAuthorization()
.RequireRateLimiting("PdfGeneration")
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

//Microsoft.Playwright.Program.Main(["install", "chromium"]);

app.Run();

