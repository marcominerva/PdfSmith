using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using PdfSmith.BusinessLayer.Extensions;
using PdfSmith.BusinessLayer.Templating;
using PdfSmith.Shared.Models;
using SimpleAuthentication;
using TinyHelpers.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSimpleAuthentication(builder.Configuration);

builder.Services.AddKeyedSingleton<ITemplateEngine, ScribanTemplateEngine>("scriban");

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("PdfGeneration", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(context.User.Identity?.Name ?? "Default", _ => new FixedWindowRateLimiterOptions
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

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapPost("/api/pdf", async (PdfGenerationRequest request, IServiceProvider serviceProvider, HttpContext httpContext) =>
{
    var templateEngine = serviceProvider.GetRequiredKeyedService<ITemplateEngine>(request.TemplateEngine.ToLowerInvariant());

    var model = request.Model.ToExpandoObject();
    var result = await templateEngine.RenderAsync(request.Template, model, CultureInfo.CurrentCulture, httpContext.RequestAborted);
    return TypedResults.Ok(result);
})
.RequireAuthorization()
.RequireRateLimiting("PdfGeneration")
.WithRequestTimeout(new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutStatusCode = StatusCodes.Status408RequestTimeout
});

app.Run();

