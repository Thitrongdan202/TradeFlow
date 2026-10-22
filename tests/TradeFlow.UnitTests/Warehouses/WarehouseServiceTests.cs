using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Warehouse;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Warehouses;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Warehouses;

public class WarehouseServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly WarehouseService _sut;

    private readonly Warehouse _testWarehouse;
    private readonly Product _testProduct1;
    private readonly Product _testProduct2;

    public WarehouseServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new TradeFlowDbContext(dbOptions);
        _mockAudit = new Mock<IAuditService>();

        _sut = new WarehouseService(_context, _mockAudit.Object);

        // Seed master data
        _testWarehouse = new Warehouse
        {
            Code = "KHO01",
            Name = "Kho Tổng Lacasa Hà Nội",
            IsActive = true
        };
        _context.Warehouses.Add(_testWarehouse);

        _testProduct1 = new Product
        {
            Code = "SP000001",
            Name = "Bồn cầu liền khối Lacasa Luxury LC-8800",
            IsActive = true
        };
        _testProduct2 = new Product
        {
            Code = "SP000002",
            Name = "Sen cây tắm đứng hiển thị nhiệt độ LC-620",
            IsActive = true
        };
        _context.Products.AddRange(_testProduct1, _testProduct2);

        // Document Categories for vouchers
        _context.DocumentCategories.AddRange(
            new DocumentCategory { Code = "PHIEU_NHAP", Name = "Phiếu nhập kho", IsActive = true },
            new DocumentCategory { Code = "PHIEU_XUAT", Name = "Phiếu xuất kho", IsActive = true },
            new DocumentCategory { Code = "BIEN_BAN", Name = "Biên bản nghiệm thu / kiểm kê", IsActive = true }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task EnsureInitialStockAsync_ShouldCreateStockAndMovement()
    {
        // Act
        await _sut.EnsureInitialStockAsync(_testWarehouse.Id, _testProduct1.Id, 50, "Admin");

        // Assert
        var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct1.Id);
        stock.Should().NotBeNull();
        stock!.Quantity.Should().Be(50);
        stock.Condition.Should().Be(StockCondition.Saleable);

        var movement = await _context.InventoryMovements.FirstOrDefaultAsync(m => m.WarehouseId == _testWarehouse.Id && m.ProductId == _testProduct1.Id);
        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(50);
        movement.Direction.Should().Be("IN");
        movement.PreviousBalance.Should().Be(0);
        movement.NewBalance.Should().Be(50);
    }

    [Fact]
    public async Task Stocktake_SeparationOfDuties_ShouldBlockCounterFromApprovingOwnStocktake()
    {
        // Arrange: Initial stock 50
        await _sut.EnsureInitialStockAsync(_testWarehouse.Id, _testProduct1.Id, 50, "Admin");

        // Create session by counter 'kho01'
        var session = await _sut.CreateStocktakeSessionAsync(_testWarehouse.Id, DateTime.UtcNow, "Kiểm kê định kỳ", "user_kho01", "Kho 01");

        // Counter enters count 48 (shortage of 2)
        var counts = new List<StocktakeItemDto>
        {
            new() { ProductId = _testProduct1.Id, BookQuantity = 50, CountedQuantity = 48, Condition = StockCondition.Saleable, UnitPrice = 1000000 }
        };
        await _sut.SaveCountsAsync(session.Id, counts, "user_kho01", "Kho 01");
        await _sut.SubmitStocktakeAsync(session.Id, "user_kho01", "Kho 01");

        // Act: Counter 'user_kho01' tries to approve own stocktake without superAdmin
        Func<Task> act = async () => await _sut.ApproveAndReconcileStocktakeAsync(session.Id, "user_kho01", "Kho 01", isSuperAdmin: false);

        // Assert: Throws InvalidOperationException enforcing separation of duties
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không được phép tự phê duyệt kết quả kiểm kê*");
    }

    [Fact]
    public async Task Stocktake_ApprovalBySupervisor_ShouldReconcileVarianceAndCreateAdjustmentVouchers()
    {
        // Arrange: Initial stock 50
        await _sut.EnsureInitialStockAsync(_testWarehouse.Id, _testProduct1.Id, 50, "Admin");
        await _sut.EnsureInitialStockAsync(_testWarehouse.Id, _testProduct2.Id, 30, "Admin");

        // Create session by counter 'kho01'
        var session = await _sut.CreateStocktakeSessionAsync(_testWarehouse.Id, DateTime.UtcNow, "Kiểm kê tháng 10", "user_kho01", "Kho 01");

        // Counter enters: SP1 count = 55 (surplus +5), SP2 count = 28 (shortage -2)
        var counts = new List<StocktakeItemDto>
        {
            new() { ProductId = _testProduct1.Id, BookQuantity = 50, CountedQuantity = 55, Condition = StockCondition.Saleable, UnitPrice = 2000000, Notes = "Thừa 5 cái" },
            new() { ProductId = _testProduct2.Id, BookQuantity = 30, CountedQuantity = 28, Condition = StockCondition.Saleable, UnitPrice = 1500000, Notes = "Thiếu 2 bộ" }
        };
        await _sut.SaveCountsAsync(session.Id, counts, "user_kho01", "Kho 01");
        await _sut.SubmitStocktakeAsync(session.Id, "user_kho01", "Kho 01");

        // Act: Supervisor 'user_quanly01' approves
        var result = await _sut.ApproveAndReconcileStocktakeAsync(session.Id, "user_quanly01", "Quản Lý 01", isSuperAdmin: false);
        result.Should().BeTrue();

        // Assert stock balances updated to counted quantities
        var stock1 = await _context.InventoryStocks.FirstAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct1.Id);
        stock1.Quantity.Should().Be(55); // Increased from 50 to 55

        var stock2 = await _context.InventoryStocks.FirstAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct2.Id);
        stock2.Quantity.Should().Be(28); // Decreased from 30 to 28

        // Verify adjustment vouchers generated
        var updatedSession = await _context.StocktakeSessions.FirstAsync(s => s.Id == session.Id);
        updatedSession.Status.Should().Be(StocktakeStatus.Completed);
        updatedSession.AdjustmentInboundVoucherId.Should().NotBeNull();
        updatedSession.AdjustmentOutboundVoucherId.Should().NotBeNull();

        // Check Inbound Voucher (01-VT for surplus)
        var inboundVoucher = await _context.WarehouseVouchers
            .Include(v => v.Items)
            .FirstAsync(v => v.Id == updatedSession.AdjustmentInboundVoucherId);
        inboundVoucher.VoucherType.Should().Be(WarehouseVoucherType.InboundReceipt);
        inboundVoucher.TotalQuantity.Should().Be(5);
        inboundVoucher.Status.Should().Be(WarehouseVoucherStatus.Approved);

        // Check Outbound Voucher (02-VT for shortage)
        var outboundVoucher = await _context.WarehouseVouchers
            .Include(v => v.Items)
            .FirstAsync(v => v.Id == updatedSession.AdjustmentOutboundVoucherId);
        outboundVoucher.VoucherType.Should().Be(WarehouseVoucherType.OutboundIssue);
        outboundVoucher.TotalQuantity.Should().Be(2);
        outboundVoucher.Status.Should().Be(WarehouseVoucherStatus.Approved);
    }

    [Fact]
    public async Task CustomerReturn_InspectionAndApproval_ShouldHandleSaleableAndDefectiveItems()
    {
        // Arrange
        await _sut.EnsureInitialStockAsync(_testWarehouse.Id, _testProduct1.Id, 20, "Admin");

        var returnOrder = await _sut.CreateReturnOrderAsync(new WarehouseReturnOrderDto
        {
            ReturnType = WarehouseReturnType.CustomerReturn,
            WarehouseId = _testWarehouse.Id,
            CustomerName = "Công ty XD An Phát",
            OriginalInvoiceNumber = "HD-2026-0042",
            Reason = "Khách hàng hoàn trả 1 bộ bồn cầu và 2 bộ sen cây",
            Items = new List<WarehouseReturnOrderItemDto>
            {
                new() { ProductId = _testProduct1.Id, RequestedQuantity = 1, UnitPrice = 2500000 },
                new() { ProductId = _testProduct2.Id, RequestedQuantity = 2, UnitPrice = 1800000 }
            }
        }, "user_kho01", "Kho 01");

        // 1. Intake at warehouse gate
        var intakeItems = returnOrder.Items.Select(i => new WarehouseReturnOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            RequestedQuantity = i.RequestedQuantity,
            ReceivedQuantity = i.RequestedQuantity,
            UnitPrice = i.UnitPrice
        }).ToList();
        await _sut.ReceiveReturnIntakeAsync(returnOrder.Id, intakeItems, "user_kho01", "Kho 01");

        // 2. Inspection: SP1 is Saleable (RestockSaleable), SP2 is Defective (QuarantineDefective)
        var inspectionItems = returnOrder.Items.Select(i => new WarehouseReturnOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            RequestedQuantity = i.RequestedQuantity,
            ReceivedQuantity = i.RequestedQuantity,
            UnitPrice = i.UnitPrice,
            Condition = i.ProductId == _testProduct1.Id ? StockCondition.Saleable : StockCondition.Defective,
            Disposition = i.ProductId == _testProduct1.Id ? ReturnDisposition.RestockSaleable : ReturnDisposition.QuarantineDefective,
            InspectionNotes = i.ProductId == _testProduct1.Id ? "Nguyên đai kiện" : "Lỗi van nhiệt"
        }).ToList();
        await _sut.InspectReturnOrderAsync(returnOrder.Id, inspectionItems, "Kiểm nghiệm đạt", "user_kho01", "Kho 01");

        // 3. Supervisor approves
        var approved = await _sut.ApproveAndProcessReturnAsync(returnOrder.Id, "user_quanly01", "Quản Lý 01");
        approved.Should().BeTrue();

        // Assert: SP1 (Saleable) was added to stock (20 + 1 = 21)
        var sp1Stock = await _context.InventoryStocks.FirstAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct1.Id && s.Condition == StockCondition.Saleable);
        sp1Stock.Quantity.Should().Be(21);

        // Assert: SP2 (Defective) was registered in QuarantinedInventoryItems (isolated)
        var quarantineItem = await _context.QuarantinedInventoryItems.FirstOrDefaultAsync(q => q.WarehouseId == _testWarehouse.Id && q.ProductId == _testProduct2.Id);
        quarantineItem.Should().NotBeNull();
        quarantineItem!.Quantity.Should().Be(2);
        quarantineItem.Status.Should().Be(QuarantineStatus.InQuarantine);

        // Assert: SP2 Defective stock in ledger is 2
        var sp2DefectiveStock = await _context.InventoryStocks.FirstAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct2.Id && s.Condition == StockCondition.Defective);
        sp2DefectiveStock.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task QuarantineResolution_WhenRestored_ShouldTransferBackToSaleable()
    {
        // Arrange: 2 defective units quarantined
        await _sut.QuarantineDirectStockAsync(_testWarehouse.Id, _testProduct2.Id, 2, "Hỏng vỏ hộp", "QC-01", "user_kho01", "Kho 01");

        var qItem = await _context.QuarantinedInventoryItems.FirstAsync(q => q.WarehouseId == _testWarehouse.Id && q.ProductId == _testProduct2.Id);
        qItem.Status.Should().Be(QuarantineStatus.InQuarantine);

        // Act: Resolved as Restored (re-packaged and tested good)
        var resolved = await _sut.ResolveQuarantineAsync(qItem.Id, QuarantineStatus.Restored, "Đã thay vỏ hộp chính hãng, kiểm tra đạt 100%", "user_quanly01", "Quản Lý 01");
        resolved.Should().BeTrue();

        // Assert: Quarantine item status is Restored
        var updatedQ = await _context.QuarantinedInventoryItems.FirstAsync(q => q.Id == qItem.Id);
        updatedQ.Status.Should().Be(QuarantineStatus.Restored);

        // Assert: Defective stock decreased by 2 (down to 0)
        var defStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct2.Id && s.Condition == StockCondition.Defective);
        defStock!.Quantity.Should().Be(0);

        // Assert: Saleable stock increased by 2
        var saleableStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == _testWarehouse.Id && s.ProductId == _testProduct2.Id && s.Condition == StockCondition.Saleable);
        saleableStock!.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task WarehouseVoucher_WhenApproved_ShouldCreateDocumentAttachment()
    {
        // Arrange: Inbound voucher
        var voucherDto = new WarehouseVoucherDto
        {
            VoucherType = WarehouseVoucherType.InboundReceipt,
            WarehouseId = _testWarehouse.Id,
            PartnerName = "Nhà cung cấp Viglacera",
            Reason = "Nhập mua vật tư đợt 1",
            Items = new List<WarehouseVoucherItemDto>
            {
                new() { ProductId = _testProduct1.Id, ActualQuantity = 10, UnitPrice = 2000000 }
            }
        };
        var voucher = await _sut.CreateVoucherAsync(voucherDto, "user_kho01", "Kho 01");

        // Act: Approve voucher
        var approved = await _sut.ApproveVoucherAsync(voucher.Id, "user_quanly01", "Quản Lý 01");
        approved.Should().BeTrue();

        // Assert: Canonical DocumentAttachment created in database
        var attachment = await _context.DocumentAttachments.FirstOrDefaultAsync(d => d.RelatedEntityType == "WarehouseVoucher" && d.RelatedEntityId == voucher.Id);
        attachment.Should().NotBeNull();
        attachment!.FileName.Should().Contain(voucher.VoucherNumber);
        attachment.Description.Should().Contain("Phiếu nhập kho Mẫu 01-VT");
    }
}
