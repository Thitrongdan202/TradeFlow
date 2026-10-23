using System;
using System.Collections.Generic;
using FluentAssertions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Application.Common.Models.Warehouse;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Services.Pdf;
using Xunit;

namespace TradeFlow.UnitTests.Documents;

public class DocumentOutputAndContractSurgeryTests
{
    public DocumentOutputAndContractSurgeryTests()
    {
        // QuestPDF license configuration for tests
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void ContractPartyASettings_ShouldHaveConfigurableSigningLocation()
    {
        // Arrange
        var settings = new ContractPartyASettings
        {
            CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            DefaultSigningLocation = "TP. Hồ Chí Minh"
        };

        // Assert
        settings.DefaultSigningLocation.Should().Be("TP. Hồ Chí Minh");
    }

    [Fact]
    public void ContractPartyASettingsDto_ShouldCarrySigningLocation()
    {
        // Arrange
        var dto = new ContractPartyASettingsDto
        {
            CompanyName = "LACASA",
            DefaultSigningLocation = "TP. Hà Nội"
        };

        // Assert
        dto.DefaultSigningLocation.Should().Be("TP. Hà Nội");
    }

    [Fact]
    public void CompanySettings_ShouldHaveAccountingRegimeConfigured()
    {
        // Arrange
        var settings = new CompanySettings("LACASA VIỆT NAM")
        {
            AccountingRegime = "TT99_2025"
        };

        // Assert
        settings.AccountingRegime.Should().Be("TT99_2025");
    }

    [Fact]
    public void WarehouseVoucherPdfDocument_OutboundIssue_ShouldGenerateValidPdfBytes()
    {
        // Arrange
        var voucher = new WarehouseVoucherDto
        {
            Id = 1,
            VoucherType = WarehouseVoucherType.OutboundIssue,
            VoucherNumber = "PXK-2026-0001",
            VoucherDate = new DateTime(2026, 10, 22),
            WarehouseName = "Kho Tổng Thiết Bị Sứ",
            WarehouseCode = "KHO-01",
            Reason = "Xuất kho bán hàng công trình",
            DelivererOrReceiverName = "Nguyễn Văn B",
            PartnerName = "Công ty CP Đầu Tư Xây Dựng Nam Long",
            CreatedByName = "Trần Văn Tuấn",
            WarehouseKeeperName = "Lê Văn Kho",
            ApprovedByName = "Quản Lý 01",
            TotalQuantity = 15,
            TotalAmount = 45000000,
            Items = new List<WarehouseVoucherItemDto>
            {
                new()
                {
                    ProductCode = "SP-001",
                    ProductName = "Bồn cầu thông minh LACASA Smart-01",
                    UnitName = "Bộ",
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 3500000,
                    LineTotal = 35000000
                },
                new()
                {
                    ProductCode = "SP-002",
                    ProductName = "Bộ sen tắm cây nhiệt độ mạ crom",
                    UnitName = "Bộ",
                    DocumentQuantity = 5,
                    ActualQuantity = 5,
                    UnitPrice = 2000000,
                    LineTotal = 10000000
                }
            }
        };

        var company = new CompanySettings("CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM")
        {
            Address = "Số 123 Đường Nguyễn Văn Cừ, Cần Thơ",
            TaxCode = "0316899999",
            Phone = "0909 123 456",
            AccountingRegime = "TT99_2025"
        };

        var doc = new WarehouseVoucherPdfDocument(voucher, company);

        // Act
        var pdfBytes = doc.GeneratePdf();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);
        // PDF header check (%PDF-)
        pdfBytes[0].Should().Be((byte)'%');
        pdfBytes[1].Should().Be((byte)'P');
        pdfBytes[2].Should().Be((byte)'D');
        pdfBytes[3].Should().Be((byte)'F');
    }

    [Fact]
    public void WarehouseVoucherPdfDocument_InboundReceipt_ShouldGenerateValidPdfBytes()
    {
        // Arrange
        var voucher = new WarehouseVoucherDto
        {
            Id = 2,
            VoucherType = WarehouseVoucherType.InboundReceipt,
            VoucherNumber = "PNK-2026-0005",
            VoucherDate = new DateTime(2026, 10, 22),
            WarehouseName = "Kho Trung Tâm Cần Thơ",
            WarehouseCode = "KHO-02",
            Reason = "Nhập kho mua hàng từ nhà máy",
            DelivererOrReceiverName = "Lái xe giao nhận",
            PartnerName = "Nhà máy Sứ Vệ Sinh Viglacera",
            CreatedByName = "Trần Văn Tuấn",
            WarehouseKeeperName = "Nguyễn Thủ Kho",
            ApprovedByName = "Giám Đốc Chi Nhánh",
            TotalQuantity = 20,
            TotalAmount = 50000000,
            Items = new List<WarehouseVoucherItemDto>
            {
                new()
                {
                    ProductCode = "SP-003",
                    ProductName = "Chậu lavabo đặt bàn đá nhân tạo",
                    UnitName = "Cái",
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 2500000,
                    LineTotal = 50000000
                }
            }
        };

        var company = new CompanySettings("CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM")
        {
            AccountingRegime = "TT99_2025"
        };

        var doc = new WarehouseVoucherPdfDocument(voucher, company);

        // Act
        var pdfBytes = doc.GeneratePdf();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);
    }

    [Theory]
    [InlineData(1500000, "Một triệu năm trăm nghìn")]
    [InlineData(45000000, "Bốn mươi lăm triệu")]
    public void VietnameseNumberToWords_ShouldConvertCorrectly(decimal amount, string expectedPrefix)
    {
        var result = VietnameseNumberToWords.ConvertToWords(amount);
        result.Should().StartWith(expectedPrefix);
    }
}
