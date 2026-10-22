using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Warehouse;

public class InventoryStockDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }

    public StockCondition Condition { get; set; } = StockCondition.Saleable;
    public string ConditionName => Condition switch
    {
        StockCondition.Saleable => "Hàng tốt (Khả dụng)",
        StockCondition.Usable => "Hàng đã qua sử dụng / Trưng bày",
        StockCondition.Defective => "Hàng lỗi / Cách ly",
        _ => "Chưa xác định"
    };

    public decimal Quantity { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public class InventoryMovementDto
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public string MovementTypeName => MovementType switch
    {
        InventoryMovementType.InboundPurchase => "Nhập mua hàng",
        InventoryMovementType.InboundStocktakeSurplus => "Nhập kiểm kê thừa",
        InventoryMovementType.InboundCustomerReturn => "Nhập khách trả lại",
        InventoryMovementType.InboundTransfer => "Nhập chuyển kho",
        InventoryMovementType.OutboundSale => "Xuất bán hàng",
        InventoryMovementType.OutboundStocktakeShortage => "Xuất kiểm kê thiếu",
        InventoryMovementType.OutboundSupplierReturn => "Xuất trả nhà cung cấp",
        InventoryMovementType.OutboundScrap => "Xuất hủy hàng hỏng",
        InventoryMovementType.OutboundTransfer => "Xuất chuyển kho",
        InventoryMovementType.QuarantineTransferIn => "Chuyển vào cách ly",
        InventoryMovementType.QuarantineDisposition => "Giải tỏa cách ly",
        _ => "Biến động kho"
    };

    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public StockCondition Condition { get; set; }
    public decimal Quantity { get; set; }
    public string Direction { get; set; } = "IN";
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice
    {
        get => UnitCost;
        set => UnitCost = value;
    }

    public int? VoucherId { get; set; }
    public string? VoucherNumber { get; set; }
    public string? SourceReference { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }
}

public class WarehouseVoucherDto
{
    public int Id { get; set; }
    public WarehouseVoucherType VoucherType { get; set; } = WarehouseVoucherType.InboundReceipt;
    public string VoucherTypeName => VoucherType switch
    {
        WarehouseVoucherType.InboundReceipt => "Phiếu nhập kho (Mẫu 01-VT)",
        WarehouseVoucherType.OutboundIssue => "Phiếu xuất kho (Mẫu 02-VT)",
        WarehouseVoucherType.InspectionRecord => "Biên bản kiểm nghiệm (Mẫu 03-VT)",
        WarehouseVoucherType.StocktakeReport => "Biên bản kiểm kê (Mẫu 05-VT)",
        _ => "Chứng từ kho"
    };

    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;

    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string? WarehouseAddress { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public string? PartnerAddress { get; set; }
    public string SourceModule { get; set; } = "Warehouse";
    public int? SourceRecordId { get; set; }
    public string? SourceReference { get; set; }

    public WarehouseVoucherStatus Status { get; set; } = WarehouseVoucherStatus.Draft;
    public string StatusName => Status switch
    {
        WarehouseVoucherStatus.Draft => "Bản nháp",
        WarehouseVoucherStatus.Approved => "Đã ghi sổ",
        WarehouseVoucherStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };

    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public string? CreatedByName { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public string? DelivererOrReceiverName { get; set; }

    public List<WarehouseVoucherItemDto> Items { get; set; } = new();
}

public class WarehouseVoucherItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public StockCondition Condition { get; set; } = StockCondition.Saleable;

    public decimal DocumentQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class StocktakeSessionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime StocktakeDate { get; set; } = DateTime.UtcNow;
    public string Scope { get; set; } = "All";
    public string Reason { get; set; } = "Kiểm kê định kỳ";
    public string? Notes { get; set; }

    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft;
    public string StatusName => Status switch
    {
        StocktakeStatus.Draft => "Bản nháp",
        StocktakeStatus.Counting => "Đang kiểm đếm",
        StocktakeStatus.PendingReview => "Chờ duyệt",
        StocktakeStatus.Rejected => "Cần kiểm tra lại",
        StocktakeStatus.Approved => "Đã duyệt",
        StocktakeStatus.Completed => "Đã hoàn tất",
        StocktakeStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };

    public DateTime? SnapshotTakenAt { get; set; }
    public string? CountedByUserId { get; set; }
    public string? CountedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int? AdjustmentInboundVoucherId { get; set; }
    public string? AdjustmentInboundVoucherNumber { get; set; }
    public int? AdjustmentOutboundVoucherId { get; set; }
    public string? AdjustmentOutboundVoucherNumber { get; set; }

    public int TotalProductsCount => Items.Count;
    public int MatchedCount => Items.Count(i => i.VarianceQuantity == 0);
    public int SurplusCount => Items.Count(i => i.VarianceQuantity > 0);
    public int ShortageCount => Items.Count(i => i.VarianceQuantity < 0);
    public decimal TotalSurplusQuantity => Items.Where(i => i.VarianceQuantity > 0).Sum(i => i.VarianceQuantity);
    public decimal TotalShortageQuantity => Items.Where(i => i.VarianceQuantity < 0).Sum(i => Math.Abs(i.VarianceQuantity));
    public decimal TotalBookQuantity => Items.Sum(i => i.BookQuantity);
    public decimal TotalCountedQuantity => Items.Sum(i => i.CountedQuantity);
    public decimal TotalDiscrepancyQuantity => Items.Sum(i => i.VarianceQuantity);
    public decimal DiscrepancyAmount => Items.Sum(i => i.VarianceValue);

    public List<StocktakeItemDto> Items { get; set; } = new();
}

public class StocktakeItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public decimal BookQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }

    public StockCondition Condition { get; set; } = StockCondition.Saleable;
    public decimal UnitCost { get; set; }
    public decimal UnitPrice
    {
        get => UnitCost;
        set => UnitCost = value;
    }
    public decimal VarianceValue { get; set; }
    public string? DiscrepancyReason { get; set; }
    public string? Notes { get; set; }
}

public class WarehouseReturnOrderDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public WarehouseReturnType ReturnType { get; set; } = WarehouseReturnType.CustomerReturn;
    public string ReturnTypeName => ReturnType == WarehouseReturnType.CustomerReturn ? "Khách trả hàng" : "Trả nhà cung cấp";

    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string PartnerName => CustomerName ?? SupplierName ?? string.Empty;

    public int? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public int? OriginalSalesOrderId { get; set; }
    public string? OriginalSalesOrderCode { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public WarehouseReturnStatus Status { get; set; } = WarehouseReturnStatus.PendingIntake;
    public string StatusName => Status switch
    {
        WarehouseReturnStatus.PendingIntake => "Chờ tiếp nhận",
        WarehouseReturnStatus.ReceivedAwaitingInspection => "Đã nhận, chờ kiểm định",
        WarehouseReturnStatus.Inspected => "Đã kiểm định",
        WarehouseReturnStatus.Approved => "Đã duyệt xử lý",
        WarehouseReturnStatus.Completed => "Đã hoàn tất",
        WarehouseReturnStatus.Rejected => "Từ chối nhận",
        WarehouseReturnStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };

    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? InspectedByName { get; set; }
    public DateTime? InspectedAt { get; set; }
    public string? InspectionNotes { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public int? ResultingVoucherId { get; set; }
    public string? ResultingVoucherNumber { get; set; }

    public List<WarehouseReturnOrderItemDto> Items { get; set; } = new();
}

public class WarehouseReturnOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public StockCondition Condition { get; set; } = StockCondition.Undetermined;
    public ReturnDisposition Disposition { get; set; } = ReturnDisposition.PendingInspection;

    public string DispositionName => Disposition switch
    {
        ReturnDisposition.PendingInspection => "Chờ phân loại",
        ReturnDisposition.RestockSaleable => "Nhập lại kho hàng tốt",
        ReturnDisposition.TransferUsable => "Chuyển kho hàng cũ/trưng bày",
        ReturnDisposition.QuarantineDefective => "Cách ly hàng lỗi/hỏng",
        ReturnDisposition.RepairService => "Chuyển bảo hành/sửa chữa",
        ReturnDisposition.RejectReturn => "Từ chối nhận",
        ReturnDisposition.ReturnToSupplier => "Trả lại nhà cung cấp",
        _ => "Chưa xác định"
    };

    public string? DefectDetails { get; set; }
    public string? InspectionNotes { get; set; }
    public decimal UnitPrice { get; set; }
}

public class QuarantinedItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string DefectReason { get; set; } = string.Empty;
    public DateTime DateIdentified { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public string? ReportedByName { get; set; }

    public QuarantineStatus Status { get; set; } = QuarantineStatus.InQuarantine;
    public string StatusName => Status switch
    {
        QuarantineStatus.InQuarantine => "Đang cách ly",
        QuarantineStatus.Repairing => "Đang sửa chữa / bảo hành",
        QuarantineStatus.Restored => "Đã phục hồi",
        QuarantineStatus.ReturnedToSupplier => "Đã trả nhà cung cấp",
        QuarantineStatus.Scrapped => "Đã xuất hủy / thanh lý",
        _ => "Không xác định"
    };

    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByName { get; set; }

    public int? ResultingVoucherId { get; set; }
    public string? ResultingVoucherNumber { get; set; }
}
