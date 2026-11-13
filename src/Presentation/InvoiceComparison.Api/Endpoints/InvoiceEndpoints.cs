using InvoiceComparison.Application.DTOs;
using InvoiceComparison.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceComparison.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/companies (returns company Id and Name for frontend dropdown)
        app.MapGet("/api/companies", (
            IMissingInvoiceDetectorService detectorService) =>
        {
            var companies = detectorService.GetCompanies();
            return Results.Ok(companies);
        })
        .WithName("GetCompanies")
        .WithTags("Companies")
        .Produces<IReadOnlyList<CompanyDto>>(StatusCodes.Status200OK);

        var group = app.MapGroup("/api/invoices")
            .WithTags("Invoices");

        // GET /api/invoices/missing?companyId=firma1&startDate=2026-11-01&endDate=2026-11-10
        group.MapGet("/missing", async (
            [FromQuery] string companyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IMissingInvoiceDetectorService detectorService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing Company",
                    Detail = "Query parameter 'companyId' is required."
                });
            }

            var request = new MissingInvoicesRequest
            {
                CompanyId = companyId.Trim(),
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

        // GET /api/invoices/{ettn}/pdf?companyId=firma1
        group.MapGet("/{ettn}/pdf", async (
            [FromRoute] string ettn,
            [FromQuery] string companyId,
            IMissingInvoiceDetectorService detectorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing Company",
                    Detail = "Query parameter 'companyId' is required."
                });
            }

            var pdfBytes = await detectorService
                .GetInvoicePdfAsync(companyId.Trim(), ettn, cancellationToken)
                .ConfigureAwait(false);

            var fileName = $"Invoice_{ettn.Trim()}.pdf";
            httpContext.Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");

            return Results.File(pdfBytes, contentType: "application/pdf");
        })
        .WithName("GetInvoicePdf")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        // POST /api/invoices/{ettn}/mark-unread?companyId=firma1
        group.MapPost("/{ettn}/mark-unread", async (
            [FromRoute] string ettn,
            [FromQuery] string companyId,
            IMissingInvoiceDetectorService detectorService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing Company",
                    Detail = "Query parameter 'companyId' is required."
                });
            }

            var response = await detectorService
                .MarkInvoiceAsUnreadAsync(companyId.Trim(), ettn, cancellationToken)
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
