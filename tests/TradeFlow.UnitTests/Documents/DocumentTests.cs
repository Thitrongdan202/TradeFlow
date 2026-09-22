using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Documents;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Documents;

public class DocumentTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        // Seed some categories
        var cat1 = new DocumentCategory
        {
            Code = "BAO_GIA",
            Name = "Báo giá thương mại",
            Group = DocumentTypeCategory.Commercial,
            DisplayOrder = 1,
            IsActive = true
        };
        var cat2 = new DocumentCategory
        {
            Code = "HOP_DONG",
            Name = "Hợp đồng kinh tế",
            Group = DocumentTypeCategory.Commercial,
            DisplayOrder = 2,
            IsActive = true
        };
        context.DocumentCategories.AddRange(cat1, cat2);
        context.SaveChanges();

        return context;
    }

    private (DocumentService Service, TradeFlowDbContext Context, Mock<IFileStorageService> MockStorage) CreateService(TradeFlowDbContext context)
    {
        var mockStorage = new Mock<IFileStorageService>();
        mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync((Stream stream, string fileName, string contentType, string category, System.Threading.CancellationToken ct) =>
            {
                return $"uploads/documents/{Guid.NewGuid():N}_{fileName}";
            });

        mockStorage.Setup(s => s.DeleteFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        mockStorage.Setup(s => s.GetFileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3, 4 }));

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        mockUser.Setup(u => u.UserName).Returns("admin");

        var mockAudit = new Mock<IAuditService>();

        var service = new DocumentService(context, mockStorage.Object, mockUser.Object, mockAudit.Object);
        return (service, context, mockStorage);
    }

    [Fact]
    public async Task UploadDocument_WithValidFile_SucceedsAndPersists()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, mockStorage) = CreateService(context);

        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        using var stream = new MemoryStream(content);

        var doc = await service.UploadDocumentAsync(
            stream,
            "HopDongKinhTe_2026.pdf",
            "application/pdf",
            content.Length,
            "Quotation",
            entityId: 10,
            categoryId: 2,
            description: "Hợp đồng đã ký bản scan");

        Assert.NotNull(doc);
        Assert.True(doc.Id > 0);
        Assert.Equal("HopDongKinhTe_2026.pdf", doc.FileName);
        Assert.Equal("Quotation", doc.RelatedEntityType);
        Assert.Equal(10, doc.RelatedEntityId);
        Assert.Equal(2, doc.CategoryId);
        Assert.Equal("Hợp đồng kinh tế", doc.CategoryName);
        Assert.NotEmpty(doc.StoragePath);

        mockStorage.Verify(s => s.SaveFileAsync(It.IsAny<Stream>(), "HopDongKinhTe_2026.pdf", "application/pdf", "documents", default), Times.Once);
    }

    [Theory]
    [InlineData("malicious_script.exe")]
    [InlineData("attack.bat")]
    [InlineData("exploit.sh")]
    [InlineData("danger.dll")]
    [InlineData("script.js")]
    public async Task UploadDocument_WithUnsafeExtension_ThrowsInvalidOperationException(string fileName)
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
            stream,
            fileName,
            "application/octet-stream",
            3,
            "General"));
    }

    [Fact]
    public async Task UploadDocument_Exceeding20MB_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        using var stream = new MemoryStream(new byte[] { 1 });
        long excessiveSize = (20 * 1024 * 1024) + 1; // 20MB + 1 byte

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
            stream,
            "huge_file.pdf",
            "application/pdf",
            excessiveSize,
            "General"));
    }

    [Fact]
    public async Task GetDocumentsByEntity_ReturnsOnlyMatchingEntityRecords()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        using var s1 = new MemoryStream(new byte[] { 1 });
        using var s2 = new MemoryStream(new byte[] { 2 });
        using var s3 = new MemoryStream(new byte[] { 3 });

        await service.UploadDocumentAsync(s1, "q1.pdf", "application/pdf", 1, "Quotation", entityId: 100);
        await service.UploadDocumentAsync(s2, "q2.pdf", "application/pdf", 1, "Quotation", entityId: 100);
        await service.UploadDocumentAsync(s3, "so1.pdf", "application/pdf", 1, "SalesOrder", entityId: 100);

        var quotationDocs = await service.GetDocumentsByEntityAsync("Quotation", 100);
        Assert.Equal(2, quotationDocs.Count);
        Assert.All(quotationDocs, d => Assert.Equal("Quotation", d.RelatedEntityType));

        var soDocs = await service.GetDocumentsByEntityAsync("SalesOrder", 100);
        Assert.Single(soDocs);
        Assert.Equal("so1.pdf", soDocs[0].FileName);
    }

    [Fact]
    public async Task DeleteDocument_RemovesFromDatabaseAndCallsStorage()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, mockStorage) = CreateService(context);

        using var stream = new MemoryStream(new byte[] { 1 });
        var doc = await service.UploadDocumentAsync(stream, "to_delete.pdf", "application/pdf", 1, "General");

        var deleted = await service.DeleteDocumentAsync(doc.Id);
        Assert.True(deleted);

        var inDb = await service.GetDocumentByIdAsync(doc.Id);
        Assert.Null(inDb);

        mockStorage.Verify(s => s.DeleteFileAsync(doc.StoragePath, default), Times.Once);
    }

    [Fact]
    public async Task CategoryManagement_CreateDuplicateCode_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        var dto = new DocumentCategoryDto
        {
            Code = "BAO_GIA", // already seeded in CreateInMemoryDbContext
            Name = "Báo giá trùng",
            Group = DocumentTypeCategory.Commercial
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCategoryAsync(dto));
    }
}
