namespace TradeFlow.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Issued = 2,
    Adjusted = 3,
    Replaced = 4,
    Cancelled = 99
}
