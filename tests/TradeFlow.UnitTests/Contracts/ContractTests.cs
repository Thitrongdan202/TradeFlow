using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using TradeFlow.Infrastructure.Services.Pdf;
using Xunit;

namespace TradeFlow.UnitTests.Contracts;

public class ContractTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        // Seed Party A settings
        context.ContractPartyASettings.Add(new ContractPartyASettings
        {
            CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            TradingName = "LACASA",
            TaxCode = "0316899999",
            RegisteredAddress = "Số 123 Đường Nguyễn Văn Cừ, Quận Ninh Kiều, Cần Thơ",
            RepresentativeName = "Nguyễn Văn An",
            RepresentativePosition = "Giám đốc",
            Phone = "0909 123 456",
            Email = "hopdong@lacasavietnam.vn"
        });

        // Seed Document Category HOP_DONG
        context.DocumentCategories.Add(new DocumentCategory
        {
            Code = "HOP_DONG",
            Name = "Hợp đồng kinh tế",
            Group = DocumentTypeCategory.Accounting,
            Description = "Hợp đồng thương mại",
            DisplayOrder = 1,
            IsActive = true
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task CreateContract_CalculatesFinancialsAndHash_Correctly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var codeGenMock = new Mock<ISystemCodeGenerator>();
        codeGenMock.Setup(c => c.GenerateCodeAsync(SystemCodeConstants.Contract, default))
            .ReturnsAsync("HD-2026-0001");

        var auditMock = new Mock<IAuditService>();
        var pdfMock = new Mock<IContractPdfService>();
        var storageMock = new Mock<IFileStorageService>();

        var service = new ContractService(context, codeGenMock.Object, auditMock.Object, pdfMock.Object, storageMock.Object);

        var dto = new CreateContractDto
        {
            Title = "Hợp đồng mua bán bồn cầu và lavabo thông minh",
            TemplateCode = "B2B_SALE",
            ContractPlace = "Cần Thơ",
            ContractDate = DateTime.UtcNow,
            PartyAName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            PartyATaxCode = "0316899999",
            PartyARepresentative = "Nguyễn Văn An",
            PartyBType = ContractPartyBType.Company,
            PartyBName = "Công ty CP Đầu tư Xây dựng Miền Nam",
            PartyBTaxCode = "0301122334",
            PartyBAddress = "Số 456 Võ Văn Tần, Q.3, TP.HCM",
            PartyBRepresentative = "Trần Thị Lan",
            Items = new List<ContractItemDto>
            {
                new ContractItemDto
                {
                    ProductCode = "BC-01",
                    ProductName = "Bồn cầu thông minh LACASA Smart-01",
                    Unit = "Bộ",
                    Quantity = 10,
                    UnitPrice = 10000000,
                    DiscountAmount = 0,
                    VatRate = 10
                }
            }
        };

        // Act
        var result = await service.CreateContractAsync(dto, "user-01", "Quản trị viên");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HD-2026-0001", result.ContractNumber);
        Assert.Equal(ContractStatus.Draft, result.Status);
        Assert.Equal(100000000, result.SubTotal);
        Assert.Equal(10000000, result.TaxAmount);
        Assert.Equal(110000000, result.TotalAmount);
        Assert.False(string.IsNullOrEmpty(result.AmountInWords));
        Assert.False(string.IsNullOrEmpty(result.DocumentHash));

        // Verify audit log
        auditMock.Verify(a => a.LogAsync(
            AuditEventType.ContractCreated,
            It.IsAny<string>(),
            "Contract",
            result.Id.ToString(),
            It.IsAny<string>(),
            null, null, default), Times.Once);
    }

    [Fact]
    public async Task FinalizeContract_TransitionsStatusToPendingSignature()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var codeGenMock = new Mock<ISystemCodeGenerator>();
        codeGenMock.Setup(c => c.GenerateCodeAsync(SystemCodeConstants.Contract, default))
            .ReturnsAsync("HD-2026-0002");

        var auditMock = new Mock<IAuditService>();
        var pdfMock = new Mock<IContractPdfService>();
        var storageMock = new Mock<IFileStorageService>();

        var service = new ContractService(context, codeGenMock.Object, auditMock.Object, pdfMock.Object, storageMock.Object);

        var created = await service.CreateContractAsync(new CreateContractDto
        {
            Title = "Hợp đồng thử nghiệm",
            PartyBName = "Khách hàng Cá nhân A",
            PartyBType = ContractPartyBType.Individual,
            PartyBIdCardNumber = "087095000111"
        }, "user-01", "Admin");

        // Act
        var finalized = await service.FinalizeContractAsync(created.Id, "user-01", "Admin");

        // Assert
        Assert.Equal(ContractStatus.PendingSignature, finalized.Status);
        Assert.NotNull(finalized.FinalizedAt);
        Assert.Equal("Admin", finalized.FinalizedBy);
    }

    [Fact]
    public async Task SignContract_BothPartiesSign_TransitionsToSigned()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var codeGenMock = new Mock<ISystemCodeGenerator>();
        codeGenMock.Setup(c => c.GenerateCodeAsync(SystemCodeConstants.Contract, default))
            .ReturnsAsync("HD-2026-0003");

        var auditMock = new Mock<IAuditService>();
        var pdfMock = new Mock<IContractPdfService>();
        var storageMock = new Mock<IFileStorageService>();

        var service = new ContractService(context, codeGenMock.Object, auditMock.Object, pdfMock.Object, storageMock.Object);

        var contract = await service.CreateContractAsync(new CreateContractDto
        {
            Title = "Hợp đồng B2B",
            PartyBName = "Công ty TNHH B",
            PartyBType = ContractPartyBType.Company
        }, "user-01", "Admin");

        // Act 1: Party A signs
        var afterA = await service.SignContractAsync(contract.Id, new SignContractDto
        {
            SignerParty = "PartyA",
            SignerName = "Nguyễn Văn An",
            SignatureData = "data:image/svg+xml;utf8,<svg></svg>"
        }, "user-01", "Admin");

        Assert.Equal(ContractStatus.PendingSignature, afterA.Status);
        Assert.NotNull(afterA.PartyASignedAt);
        Assert.Equal("Nguyễn Văn An", afterA.PartyASignedBy);

        // Act 2: Party B signs
        var afterB = await service.SignContractAsync(contract.Id, new SignContractDto
        {
            SignerParty = "PartyB",
            SignerName = "Trần Đại Diện",
            SignatureData = "data:image/svg+xml;utf8,<svg></svg>"
        }, "user-02", "Trần Đại Diện");

        // Assert: Fully signed
        Assert.Equal(ContractStatus.Signed, afterB.Status);
        Assert.Equal(ContractExecutionMethod.Electronic, afterB.ExecutionMethod);
        Assert.NotNull(afterB.PartyBSignedAt);
        Assert.Equal("Trần Đại Diện", afterB.PartyBSignedBy);
    }

    [Fact]
    public void VietnameseNumberToWords_ConvertsAmountsAccurately()
    {
        // Test standard cases
        Assert.Equal("Không đồng chẵn.", VietnameseNumberToWords.ConvertToWords(0));
        Assert.Equal("Mười lăm triệu đồng chẵn.", VietnameseNumberToWords.ConvertToWords(15000000));
        Assert.Equal("Một trăm mười triệu đồng chẵn.", VietnameseNumberToWords.ConvertToWords(110000000));
        Assert.Equal("Một tỷ hai trăm ba mươi tư triệu năm trăm sáu mươi bảy nghìn tám trăm chín mươi đồng chẵn.",
            VietnameseNumberToWords.ConvertToWords(1234567890));
    }

    [Fact]
    public async Task LiquidateContract_TransitionsToLiquidatedWithReason()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var codeGenMock = new Mock<ISystemCodeGenerator>();
        codeGenMock.Setup(c => c.GenerateCodeAsync(SystemCodeConstants.Contract, default))
            .ReturnsAsync("HD-2026-0004");

        var auditMock = new Mock<IAuditService>();
        var pdfMock = new Mock<IContractPdfService>();
        var storageMock = new Mock<IFileStorageService>();

        var service = new ContractService(context, codeGenMock.Object, auditMock.Object, pdfMock.Object, storageMock.Object);

        var contract = await service.CreateContractAsync(new CreateContractDto
        {
            Title = "Hợp đồng hoàn thành",
            PartyBName = "Khách hàng B",
            PartyBType = ContractPartyBType.Individual
        }, "user-01", "Admin");

        // Act
        var liquidated = await service.LiquidateContractAsync(contract.Id, "Đã hoàn thành bàn giao và nghiệm thu 100%", "user-01", "Admin");

        // Assert
        Assert.Equal(ContractStatus.Liquidated, liquidated.Status);
        Assert.Contains("Thanh lý", liquidated.Notes);
    }

    [Fact]
    public void ContractPdfDocument_GeneratesValidA4PdfBytes()
    {
        // Arrange
        var contractDto = new ContractDto
        {
            Id = 1,
            ContractNumber = "HD-2026-9999",
            Title = "Hợp đồng cung cấp thiết bị vệ sinh LACASA",
            TemplateCode = "B2B_SALE",
            ContractDate = DateTime.UtcNow,
            ContractPlace = "Cần Thơ",
            PartyAName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
            PartyATaxCode = "0316899999",
            PartyAAddress = "123 Nguyễn Văn Cừ, Cần Thơ",
            PartyARepresentative = "Nguyễn Văn An",
            PartyAPosition = "Giám đốc",
            PartyBName = "Công ty Xây dựng Phương Nam",
            PartyBTaxCode = "0311224455",
            PartyBAddress = "789 Lê Lợi, Cần Thơ",
            PartyBRepresentative = "Lê Hoàng Nam",
            PartyBPosition = "Tổng giám đốc",
            SubTotal = 50000000,
            TaxRate = 10,
            TaxAmount = 5000000,
            TotalAmount = 55000000,
            AmountInWords = "Năm mươi lăm triệu đồng chẵn.",
            DocumentHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            Clauses = new List<ContractClauseDto>
            {
                new ContractClauseDto { ClauseNumber = "Điều 1", Title = "Đối tượng hợp đồng", Content = "Bên A bán và Bên B mua thiết bị vệ sinh." },
                new ContractClauseDto { ClauseNumber = "Điều 2", Title = "Giá trị & Thanh toán", Content = "Thanh toán trong vòng 30 ngày." }
            },
            Items = new List<ContractItemDto>
            {
                new ContractItemDto
                {
                    ProductCode = "SP-01",
                    ProductName = "Sen tắm đứng cao cấp",
                    Unit = "Bộ",
                    Quantity = 5,
                    UnitPrice = 10000000,
                    LineTotal = 55000000
                }
            }
        };

        var pdfService = new ContractPdfService();

        // Act
        var bytes = pdfService.GeneratePdf(contractDto, null);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "PDF should be generated with substantial content");
        // PDF magic bytes %PDF
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
