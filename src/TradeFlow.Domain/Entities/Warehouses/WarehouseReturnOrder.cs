using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Đơn hàng trả lại (Khách trả hàng hoặc Trả hàng nhà cung cấp)
/// </summary>
public class WarehouseReturnOrder : AuditableEntity<int>
{
    /// <summary>Mã đơn hàng trả (KTH-2026-0001 hoặc TNCC-2026-0001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    public WarehouseReturnType ReturnType { get; set; } = WarehouseReturnType.CustomerReturn;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    // Đối tác
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? CustomerName { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? SupplierName { get; set; }

    // Liên kết chứng từ bán hàng / mua hàng gốc
    public int? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public int? OriginalSalesOrderId { get; set; }
    public string? OriginalSalesOrderCode { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public WarehouseReturnStatus Status { get; set; } = WarehouseReturnStatus.PendingIntake;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ReturnTypeName => ReturnType switch
    {
        WarehouseReturnType.CustomerReturn => "Khách trả hàng",
        WarehouseReturnType.SupplierReturn => "Trả hàng NCC",
        _ => ReturnType.ToString()
    };

    // Quá trình tiếp nhận tại cửa kho
    public string? ReceivedByUserId { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedAt { get; set; }

    // Quá trình kiểm tra chất lượng (Inspection)
    public string? InspectedByUserId { get; set; }
    public string? InspectedByName { get; set; }
    public DateTime? InspectedAt { get; set; }
    public string? InspectionNotes { get; set; }

    // Quá trình phê duyệt phương án xử lý
    public string? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Phiếu kho kết quả sau khi duyệt xử lý</summary>
    public int? ResultingVoucherId { get; set; }
    public WarehouseVoucher? ResultingVoucher { get; set; }

    public ICollection<WarehouseReturnOrderItem> Items { get; set; } = new List<WarehouseReturnOrderItem>();
}
