using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Pricing;

/// <summary>
/// Chi tiết một dòng sản phẩm và mức giá trong Bảng giá.
/// </summary>
public class PriceListItem : AuditableEntity<int>
{
    public PriceListItem() { }

    /// <summary>Khóa ngoại liên kết bảng giá</summary>
    public int PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>Khóa ngoại liên kết sản phẩm danh mục (null nếu là sản phẩm mới chưa liên kết)</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Số thứ tự hiển thị (STT trong tài liệu)</summary>
    public int SortOrder { get; set; }

    /// <summary>Nhóm / chủng loại sản phẩm (vd: BỒN CẦU 1 KHỐI, SEN TẮM...)</summary>
    public string? Group { get; set; }

    /// <summary>Mã hàng mới theo tài liệu bảng giá (vd: TL2138 (K8012))</summary>
    public string NewCode { get; set; } = string.Empty;

    /// <summary>Mã hàng cũ dùng để tra cứu lịch sử (vd: TL2138)</summary>
    public string? LegacyCode { get; set; }

    /// <summary>Thông tin sản phẩm / quy cách kỹ thuật</summary>
    public string? ProductInfo { get; set; }

    /// <summary>Đường dẫn hình ảnh trích xuất từ file Excel hoặc từ kho ảnh</summary>
    public string? ImageStorageRef { get; set; }

    /// <summary>Giá đại lý chưa VAT</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Loại tiền tệ (mặc định VND)</summary>
    public string CurrencyCode { get; set; } = "VND";

    /// <summary>Thuế suất VAT (%) áp dụng cho sản phẩm này (vd: 8 hoặc 10)</summary>
    public decimal? VatRate { get; set; }

    /// <summary>Ghi chú riêng cho sản phẩm</summary>
    public string? Note { get; set; }

    /// <summary>Trạng thái đối soát với danh mục sản phẩm (Đã khớp, Cần xem xét, Sản phẩm mới)</summary>
    public PriceMatchStatus MatchStatus { get; set; } = PriceMatchStatus.Matched;
}