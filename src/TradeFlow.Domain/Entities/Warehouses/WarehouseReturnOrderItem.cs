using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Chi tiết dòng hàng hóa trả lại và kết quả phân loại kiểm tra
/// </summary>
public class WarehouseReturnOrderItem : AuditableEntity<int>
{
    public int WarehouseReturnOrderId { get; set; }
    public WarehouseReturnOrder? WarehouseReturnOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    /// <summary>Số lượng yêu cầu trả / theo chứng từ</summary>
    public decimal RequestedQuantity { get; set; }

    /// <summary>Số lượng thực tế nhận được tại cửa kho</summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>Tình trạng phẩm chất xác định sau khi kiểm nghiệm</summary>
    public StockCondition Condition { get; set; } = StockCondition.Undetermined;

    /// <summary>Phương án xử lý được phê duyệt</summary>
    public ReturnDisposition Disposition { get; set; } = ReturnDisposition.PendingInspection;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DispositionName => Disposition switch
    {
        ReturnDisposition.PendingInspection => "Chờ kiểm định",
        ReturnDisposition.RestockSaleable => "Nhập kho bán hàng",
        ReturnDisposition.TransferUsable => "Chuyển kho hàng dùng được",
        ReturnDisposition.QuarantineDefective => "Chuyển kho cách ly / lỗi",
        ReturnDisposition.RepairService => "Sửa chữa nội bộ",
        ReturnDisposition.RejectReturn => "Từ chối trả hàng",
        ReturnDisposition.ReturnToSupplier => "Xuất trả nhà cung cấp",
        _ => Disposition.ToString()
    };

    /// <summary>Mô tả chi tiết lỗi / khuyết tật phát hiện (nếu có)</summary>
    public string? DefectDetails { get; set; }

    /// <summary>Ghi chú của kiểm nghiệm viên</summary>
    public string? InspectionNotes { get; set; }

    public decimal UnitPrice { get; set; }
}
