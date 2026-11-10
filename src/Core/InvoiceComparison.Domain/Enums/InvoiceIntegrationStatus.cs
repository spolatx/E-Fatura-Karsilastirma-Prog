namespace InvoiceComparison.Domain.Enums;

public enum InvoiceIntegrationStatus
{
    Unknown = 0,
    UnreadInInbox = 1,
    ReadByErp = 2,
    Archived = 3,
    Cancelled = 4
}

