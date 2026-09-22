namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phân nhóm loại chứng từ / hồ sơ
/// </summary>
public enum DocumentTypeCategory
{
    /// <summary>Chứng từ thương mại (Báo giá, Đơn đặt hàng...)</summary>
    Commercial = 1,

    /// <summary>Chứng từ vận hành nội bộ (Phiếu giao hàng, Biên bản bàn giao, Lệnh xuất kho...)</summary>
    Internal = 2,

    /// <summary>Chứng từ kế toán & pháp lý (Hóa đơn điện tử, Hợp đồng kinh tế...)</summary>
    Accounting = 3,

    /// <summary>Hồ sơ & tài liệu đính kèm (CO/CQ, Catalogue, Bản vẽ, File scan...)</summary>
    Attachment = 4
}
