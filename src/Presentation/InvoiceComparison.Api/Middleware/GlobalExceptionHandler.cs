using InvoiceComparison.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceComparison.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errorCode) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid Request Parameters",
                "VALIDATION_ERROR"),

            InvoiceNotFoundException => (
                StatusCodes.Status404NotFound,
                "Invoice Not Found",
                "INVOICE_NOT_FOUND"),

            ELogoApiException eLogoEx => (
                StatusCodes.Status502BadGateway,
                "eLogo Integrator Service Error",
                eLogoEx.ErrorCode),

            LogoDbException dbEx => (
                StatusCodes.Status500InternalServerError,
                "Logo ERP Database Error",
                dbEx.ErrorCode),

            DomainException domainEx => (
                StatusCodes.Status400BadRequest,
                "Domain Rule Violation",
                domainEx.ErrorCode),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "INTERNAL_SERVER_ERROR")
        };

        _logger.LogError(
            exception,
            "[{ErrorCode}] HTTP {StatusCode} on {Method} {Path}: {Message}",
            errorCode,
            statusCode,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestampUtc"] = DateTime.UtcNow;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
