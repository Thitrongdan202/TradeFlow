using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Warehouses;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Warehouses;

public class WarehouseOverviewTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly WarehouseService _sut;

    private readonly Warehouse _warehouse1;
    private readonly Warehouse _warehouse2;
    private readonly Product _product1;
    private readonly Product _product2;
    private readonly Product _product3;

    public WarehouseOverviewTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new TradeFlowDbContext(dbOptions);
        _mockAudit = new Mock<IAuditService>();
        _sut = new WarehouseService(_context, _mockAudit.Object);

        // Seed Warehouses
        _warehouse1 = new Warehouse
        {
            Code = "KHO-HN",
            Name = "Kho Tổng Hà Nội",
            Address = "Cụm Công nghiệp Từ Liêm, Hà Nội",
            ManagerName = "Trần Văn Cường",
            Phone = "0988 111 222",
            IsActive = true
        };
        _warehouse2 = new Warehouse
        {
            Code = "KHO-HCM",
            Name = "Kho Chi nhánh Hồ Chí Minh",
            Address = "Khu Chế Xuất Tân Thuận, Q7, TP.HCM",
            ManagerName = "Lê Thị Thu",
            Phone = "0977 333 444",
            IsActive = true
        };
        _context.Warehouses.AddRange(_warehouse1, _warehouse2);

        // Seed Units
        var unitCai = new UnitOfMeasure { Code = "CAI", Name = "Cái", Symbol = "cái", IsActive = true };
        var unitMet = new UnitOfMeasure { Code = "MET", Name = "Mét", Symbol = "m", IsActive = true };
        var unitBao = new UnitOfMeasure { Code = "BAO", Name = "Bao", Symbol = "bao", IsActive = true };
        _context.UnitOfMeasures.AddRange(unitCai, unitMet, unitBao);

        // Seed Products with heterogeneous units
        _product1 = new Product
        {
            Code = "SP001",
            Name = "Bồn cầu thông minh Lacasa Lux-01",
            Unit = unitCai,
            IsActive = true
        };
        _product2 = new Product
        {
            Code = "SP002",
            Name = "Ống dẫn nước mềm chịu nhiệt",
            Unit = unitMet,
            IsActive = true
        };
        _product3 = new Product
        {
            Code = "SP003",
            Name = "Keo dán gạch chống thấm cao cấp",
            Unit = unitBao,
            IsActive = true
        };
        _context.Products.AddRange(_product1, _product2, _product3);

        // Seed InventoryStocks
        // Warehouse 1: 5 Cái (Low stock < 15), 50 Mét (Normal)
        _context.InventoryStocks.AddRange(
            new InventoryStock
            {
                Warehouse = _warehouse1,
                Product = _product1,
                Quantity = 5,
                Condition = StockCondition.Saleable
            },
            new InventoryStock
            {
                Warehouse = _warehouse1,
                Product = _product2,
                Quantity = 50,
                Condition = StockCondition.Saleable
            },
            new InventoryStock
            {
                Warehouse = _warehouse1,
                Product = _product1,
                Quantity = 2,
                Condition = StockCondition.Defective // Defective item
            }
        );

        // Warehouse 2: 12 Bao (Low stock < 15), 10 Cái
        _context.InventoryStocks.AddRange(
            new InventoryStock
            {
                Warehouse = _warehouse2,
                Product = _product3,
                Quantity = 12,
                Condition = StockCondition.Saleable
            },
            new InventoryStock
            {
                Warehouse = _warehouse2,
                Product = _product1,
                Quantity = 10,
                Condition = StockCondition.Saleable
            }
        );

        // Seed InventoryMovements for costing
        _context.InventoryMovements.AddRange(
            new InventoryMovement
            {
                Warehouse = _warehouse1,
                Product = _product1,
                MovementType = InventoryMovementType.InboundPurchase,
                Quantity = 20,
                UnitCost = 4_500_000m,
                MovementDate = DateTime.UtcNow.AddDays(-2)
            },
            new InventoryMovement
            {
                Warehouse = _warehouse1,
                Product = _product2,
                MovementType = InventoryMovementType.InboundPurchase,
                Quantity = 100,
                UnitCost = 50_000m,
                MovementDate = DateTime.UtcNow.AddDays(-1)
            },
            new InventoryMovement
            {
                Warehouse = _warehouse2,
                Product = _product3,
                MovementType = InventoryMovementType.InboundPurchase,
                Quantity = 30,
                UnitCost = 200_000m,
                MovementDate = DateTime.UtcNow.AddDays(-3)
            }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetWarehouseOverviewAsync_ComputesDistinctSkusAndValuation_WithoutAddingIncompatibleUnits()
    {
        // Act
        var overview = await _sut.GetWarehouseOverviewAsync();

        // Assert
        overview.Should().HaveCount(2);

        var wh1 = overview.FirstOrDefault(w => w.WarehouseCode == "KHO-HN");
        wh1.Should().NotBeNull();
        wh1!.DistinctSkuCount.Should().Be(2, "Kho 1 có SP001 và SP002");
        wh1.LowStockSkuCount.Should().Be(1, "Chỉ SP001 có tồn < 15 (5 cái)");
        wh1.DefectiveItemCount.Should().Be(1, "Có 1 SKU lỗi (SP001)");
        // Valuation: (5 saleable + 2 defective = 7) * 4,500,000 + 50 * 50,000 = 31,500,000 + 2,500,000 = 34,000,000
        wh1.TotalValuation.Should().Be(34_000_000m);
        wh1.LatestMovementAt.Should().NotBeNull();

        var wh2 = overview.FirstOrDefault(w => w.WarehouseCode == "KHO-HCM");
        wh2.Should().NotBeNull();
        wh2!.DistinctSkuCount.Should().Be(2, "Kho 2 có SP001 và SP003");
        wh2.LowStockSkuCount.Should().Be(2, "Cả SP001 (10 cái) và SP003 (12 bao) đều có tồn < 15");
    }

    [Fact]
    public async Task GetCrossWarehouseMatrixAsync_MapsQuantitiesPerWarehouse_AndSameUnitTotal()
    {
        // Act
        var matrix = await _sut.GetCrossWarehouseMatrixAsync();

        // Assert
        matrix.Should().HaveCount(3);

        var rowSp1 = matrix.FirstOrDefault(r => r.ProductCode == "SP001");
        rowSp1.Should().NotBeNull();
        rowSp1!.UnitName.Should().Be("Cái");
        rowSp1.WarehouseQuantities.Should().ContainKey(_warehouse1.Id);
        rowSp1.WarehouseQuantities[_warehouse1.Id].Should().Be(5);
        rowSp1.WarehouseQuantities.Should().ContainKey(_warehouse2.Id);
        rowSp1.WarehouseQuantities[_warehouse2.Id].Should().Be(10);
        rowSp1.TotalQuantity.Should().Be(15, "Tổng tồn của cùng 1 sản phẩm (Cái) qua 2 kho là 5 + 10 = 15");
        rowSp1.EstimatedCost.Should().Be(4_500_000m);

        var rowSp2 = matrix.FirstOrDefault(r => r.ProductCode == "SP002");
        rowSp2.Should().NotBeNull();
        rowSp2!.UnitName.Should().Be("Mét");
        rowSp2.TotalQuantity.Should().Be(50);

        var rowSp3 = matrix.FirstOrDefault(r => r.ProductCode == "SP003");
        rowSp3.Should().NotBeNull();
        rowSp3!.UnitName.Should().Be("Bao");
        rowSp3.TotalQuantity.Should().Be(12);
    }

    [Fact]
    public async Task ExportStockExcelAsync_GeneratesValidExcelWorkbook()
    {
        // Act
        var excelBytes = await _sut.ExportStockExcelAsync();

        // Assert
        excelBytes.Should().NotBeNull();
        excelBytes.Length.Should().BeGreaterThan(100);

        // Verify valid zip/excel file header: PK\x03\x04
        excelBytes[0].Should().Be((byte)'P');
        excelBytes[1].Should().Be((byte)'K');

        // Can be parsed by ClosedXML
        using var stream = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("BaoCaoTonKho");
        worksheet.Should().NotBeNull();
        worksheet.Cell(1, 1).GetString().Should().Contain("LACASA");
        worksheet.Cell(2, 1).GetString().Should().Contain("BÁO CÁO TỒN KHO");
    }
}
