using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceComparison.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices")
            .WithTags("Invoices");

        // GET /api/invoices/missing?startDate=2026-11-01&endDate=2026-11-10
        group.MapGet("/missing", async (
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IMissingInvoiceDetectorService detectorService,
            CancellationToken cancellationToken) =>
        {
            var request = new MissingInvoicesRequest
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var response = await detectorService
                .DetectMissingInvoicesAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(response);
        })
        .WithName("GetMissingInvoices")
        .Produces<MissingInvoicesResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        // GET /api/invoices/{ettn}/pdf
        group.MapGet("/{ettn}/pdf", async (
            [FromRoute] string ettn,
            IMissingInvoiceDetectorService detectorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var pdfBytes = await detectorService
                .GetInvoicePdfAsync(ettn, cancellationToken)
                .ConfigureAwait(false);

            var fileName = $"Invoice_{ettn.Trim()}.pdf";
            httpContext.Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");

            return Results.File(pdfBytes, contentType: "application/pdf");
        })
        .WithName("GetInvoicePdf")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        // POST /api/invoices/{ettn}/mark-unread
        group.MapPost("/{ettn}/mark-unread", async (
            [FromRoute] string ettn,
            IMissingInvoiceDetectorService detectorService,
            CancellationToken cancellationToken) =>
        {
            var response = await detectorService
                .MarkInvoiceAsUnreadAsync(ettn, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(response);
        })
        .WithName("MarkInvoiceAsUnread")
        .Produces<MarkInvoiceUnreadResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        return app;
    }
}

