namespace InvoiceComparison.Domain.Enums;

public enum InvoiceDirection
{
    Purchase = 1, // Gelen / Satın Alma (Inbox)
    Sales = 2     // Giden / Satış (Outbox)
}
