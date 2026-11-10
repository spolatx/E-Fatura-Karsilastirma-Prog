namespace InvoiceComparison.Application.DTOs;

public sealed record MarkInvoiceUnreadResponse
{
    public required string Ettn { get; init; }
    public required bool IsSuccess { get; init; }
    public required string Message { get; init; }
    public required DateTime ProcessedAtUtc { get; init; }
}

