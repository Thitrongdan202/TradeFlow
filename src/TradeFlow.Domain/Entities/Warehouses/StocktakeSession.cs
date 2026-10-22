using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Đợt kiểm kê kho vật lý và đối soát chênh lệch
/// </summary>
public class StocktakeSession : AuditableEntity<int>
{
    /// <summary>Mã đợt kiểm kê (KK-2026-0001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Thời điểm chốt số liệu sổ sách (Cutoff snapshot)</summary>
    public DateTime StocktakeDate { get; set; } = DateTime.UtcNow;

    /// <summary>Phạm vi kiểm kê: "All" (Toàn bộ kho), "Category" (Theo danh mục), "Custom" (Tùy chọn mặt hàng)</summary>
    public string Scope { get; set; } = "All";

    /// <summary>Mục đích / lý do kiểm kê</summary>
    public string Reason { get; set; } = "Kiểm kê định kỳ";

    public string? Notes { get; set; }

    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft;

    /// <summary>Thời điểm chốt snapshot sổ sách</summary>
    public DateTime? SnapshotTakenAt { get; set; }

    // Người thực hiện kiểm đếm (Counter)
    public string? CountedByUserId { get; set; }
    public string? CountedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Người phê duyệt / Giám sát (Reviewer / Approver - Phải tách biệt trách nhiệm với Counter)
    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Liên kết phiếu nhập kho điều chỉnh hàng thừa (nếu có thừa)</summary>
    public int? AdjustmentInboundVoucherId { get; set; }
    public WarehouseVoucher? AdjustmentInboundVoucher { get; set; }

    /// <summary>Liên kết phiếu xuất kho điều chỉnh hàng thiếu (nếu có thiếu)</summary>
    public int? AdjustmentOutboundVoucherId { get; set; }
    public WarehouseVoucher? AdjustmentOutboundVoucher { get; set; }

    public ICollection<StocktakeItem> Items { get; set; } = new List<StocktakeItem>();
}
