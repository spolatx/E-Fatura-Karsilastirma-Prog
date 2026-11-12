using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceComparison.Api.Endpoints;
using InvoiceComparison.Api.Middleware;
using InvoiceComparison.Application;
using InvoiceComparison.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Enable Windows Service and IIS support for background server deployments
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LogoELogoInvoiceComparisonService";
});
builder.WebHost.UseIISIntegration();

// Register Application & Infrastructure Services (Clean Architecture DI)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Global Exception Handler & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure JSON Serialization for Minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Configure CORS Policy for Next.js Frontend
const string FrontendCorsPolicy = "NextJsFrontendPolicy";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);

// Serve static frontend files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Map Minimal API Route Group (/api/invoices)
app.MapInvoiceEndpoints();

// SPA Fallback: route any unknown client paths to index.html
app.MapFallbackToFile("index.html");

app.Run();
