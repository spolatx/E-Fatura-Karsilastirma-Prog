namespace InvoiceComparison.Application.DTOs;

public sealed record InvoiceIdentityDto(
    string? FicheNo,
    string? Ettn,
    string? TaxNumber,
    string? ClientTitle,
    int? EInvoiceType);


