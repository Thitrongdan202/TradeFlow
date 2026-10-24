using TradeFlow.Application.Common.Models.Warehouse;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public interface IWarehouseService
{
    // === 1. Tồn kho & Thẻ kho biến động ===
    Task<List<InventoryStockDto>> GetStocksAsync(int? warehouseId = null, StockCondition? condition = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<List<InventoryStockDto>> GetStockBalancesAsync(int? warehouseId = null, StockCondition? condition = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<List<WarehouseOverviewDto>> GetWarehouseOverviewAsync(CancellationToken cancellationToken = default);
    Task<List<CrossWarehouseMatrixItemDto>> GetCrossWarehouseMatrixAsync(string? searchTerm = null, int? categoryId = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportStockExcelAsync(int? warehouseId = null, StockCondition? condition = null, CancellationToken cancellationToken = default);
    Task<List<InventoryMovementDto>> GetMovementsAsync(int? warehouseId = null, int? productId = null, StockCondition? condition = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task EnsureInitialStockAsync(int warehouseId, int productId, decimal quantity, string user, CancellationToken cancellationToken = default);

    // === 2. Chứng từ kho (Mẫu 01-VT, Mẫu 02-VT, Mẫu 03-VT, Mẫu 05-VT) ===
    Task<List<WarehouseVoucherDto>> GetVouchersAsync(int? warehouseId = null, WarehouseVoucherType? type = null, WarehouseVoucherStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<WarehouseVoucherDto?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseVoucherDto> CreateVoucherAsync(WarehouseVoucherDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ApproveVoucherAsync(int voucherId, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> CancelVoucherAsync(int voucherId, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateVoucherPdfAsync(int voucherId, CancellationToken cancellationToken = default);

    // === 3. Kiểm kê kho (Stocktaking) ===
    Task<List<StocktakeSessionDto>> GetStocktakeSessionsAsync(int? warehouseId = null, StocktakeStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<StocktakeSessionDto?> GetStocktakeSessionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StocktakeSessionDto> CreateStocktakeSessionAsync(StocktakeSessionDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<StocktakeSessionDto> CreateStocktakeSessionAsync(int warehouseId, DateTime cutoffDate, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> StartCountingAsync(int sessionId, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> SaveCountsAsync(int sessionId, List<StocktakeItemDto> counts, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> SubmitStocktakeCountsAsync(int sessionId, List<StocktakeItemDto> counts, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> SubmitStocktakeAsync(int sessionId, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> RejectStocktakeAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> RejectStocktakeSessionAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ApproveAndReconcileStocktakeAsync(int sessionId, string userId, string userName, bool isSuperAdmin, CancellationToken cancellationToken = default);
    Task<bool> CancelStocktakeAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default);

    // === 4. Hàng trả lại (Customer & Supplier Returns) ===
    Task<List<WarehouseReturnOrderDto>> GetReturnOrdersAsync(WarehouseReturnType? type = null, WarehouseReturnStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<WarehouseReturnOrderDto?> GetReturnOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseReturnOrderDto> CreateReturnOrderAsync(WarehouseReturnOrderDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ReceiveReturnIntakeAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> InspectReturnOrderAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string inspectionNotes, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> InspectReturnItemsAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ApproveAndProcessReturnAsync(int returnOrderId, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> RejectReturnOrderAsync(int returnOrderId, string reason, string userId, string userName, CancellationToken cancellationToken = default);

    // === 5. Hàng lỗi / Cách ly (Defective Stock) ===
    Task<List<QuarantinedItemDto>> GetQuarantinedItemsAsync(int? warehouseId = null, QuarantineStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<QuarantinedItemDto?> GetQuarantinedItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> QuarantineDirectStockAsync(int warehouseId, int productId, decimal quantity, string defectReason, string sourceRef, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ResolveQuarantineAsync(int quarantineId, QuarantineStatus targetStatus, string resolutionNotes, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ResolveQuarantineItemAsync(int quarantineId, QuarantineStatus targetStatus, string resolutionNotes, string userId, string userName, CancellationToken cancellationToken = default);
}
