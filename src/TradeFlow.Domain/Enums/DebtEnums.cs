namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại nghiệp vụ phát sinh trong sổ chi tiết công nợ đối tác
/// </summary>
public enum DebtTransactionType
{
    /// <summary>Thanh toán (Thu tiền khách / Chi tiền nhà cung cấp)</summary>
    Payment = 1,

    /// <summary>Điều chỉnh công nợ (Tăng hoặc giảm dư nợ do đối soát, sai sót hoặc thỏa thuận)</summary>
    Adjustment = 2,

    /// <summary>Chiết khấu thanh toán (Giảm trừ công nợ do thanh toán sớm / chiết khấu tài chính)</summary>
    PaymentDiscount = 3
}
