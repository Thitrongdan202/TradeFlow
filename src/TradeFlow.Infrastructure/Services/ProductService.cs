using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.MasterData;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace TradeFlow.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<TradeFlowDbContext> _contextFactory;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IDbContextFactory<TradeFlowDbContext> contextFactory,
        IAuditService auditService,
        ILogger<ProductService> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteProductResult> DeleteProductAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        // 1. Role-based Authorization: Admin only
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("Administrator"))
        {
            _logger.LogWarning("Unauthorized attempt to delete Product ID {ProductId} by user {User}", id, user?.Identity?.Name);
            return new DeleteProductResult
            {
                Succeeded = false,
                ErrorMessage = "Chỉ Quản trị viên (Administrator) mới có quyền thực hiện thao tác xóa sản phẩm."
            };
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var product = await context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            return new DeleteProductResult
            {
                Succeeded = false,
                ErrorMessage = "Không tìm thấy sản phẩm cần xóa."
            };
        }

        // 2. Policy: Inactive records only
        if (product.IsActive)
        {
            return new DeleteProductResult
            {
                Succeeded = false,
                ErrorMessage = $"Không thể xóa sản phẩm '{product.Name}' vì đang ở trạng thái Hoạt động. Vui lòng chuyển trạng thái sang 'Ngừng dùng' trước khi xóa."
            };
        }

        // 3. Referential Integrity Protection: Check historical transactions
        int orderItemCount = await context.SalesOrderItems.CountAsync(x => x.ProductId == id, cancellationToken);
        int invoiceItemCount = await context.InvoiceItems.CountAsync(x => x.ProductId == id, cancellationToken);
        int quotationItemCount = await context.QuotationItems.CountAsync(x => x.ProductId == id, cancellationToken);
        int totalRefs = orderItemCount + invoiceItemCount + quotationItemCount;

        if (totalRefs > 0)
        {
            return new DeleteProductResult
            {
                Succeeded = false,
                IsReferenced = true,
                ReferenceCount = totalRefs,
                ErrorMessage = $"Không thể xóa sản phẩm '{product.Name}' ({product.Code}) vì đã phát sinh {totalRefs} dòng chứng từ giao dịch lịch sử ({orderItemCount} đơn hàng, {invoiceItemCount} hóa đơn, {quotationItemCount} báo giá). Hãy duy trì sản phẩm ở trạng thái 'Ngừng dùng' để bảo vệ toàn vẹn dữ liệu chứng từ."
            };
        }

        // 4. Safe physical deletion when zero historical transactions exist
        var priceItems = await context.PriceListItems.Where(x => x.ProductId == id).ToListAsync(cancellationToken);
        if (priceItems.Any())
        {
            context.PriceListItems.RemoveRange(priceItems);
        }

        var images = await context.ProductImages.Where(x => x.ProductId == id).ToListAsync(cancellationToken);
        if (images.Any())
        {
            context.ProductImages.RemoveRange(images);
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ProductDeleted,
            user.Identity?.Name ?? "System",
            "Product",
            product.Code,
            $"Đã xóa vĩnh viễn sản phẩm {product.Name} ({product.Code}).",
            cancellationToken: cancellationToken);

        return new DeleteProductResult { Succeeded = true };
    }

    public async Task<BulkDeleteProductResult> BulkDeleteProductsAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("Administrator"))
        {
            return new BulkDeleteProductResult
            {
                RequestedCount = ids.Count,
                Messages = new List<string> { "Chỉ Quản trị viên (Administrator) mới có quyền thực hiện thao tác xóa sản phẩm." }
            };
        }

        var result = new BulkDeleteProductResult { RequestedCount = ids.Count };

        foreach (var id in ids)
        {
            var singleResult = await DeleteProductAsync(id, user, cancellationToken);
            if (singleResult.Succeeded)
            {
                result.DeletedCount++;
            }
            else
            {
                result.SkippedCount++;
                if (!string.IsNullOrEmpty(singleResult.ErrorMessage))
                {
                    result.Messages.Add(singleResult.ErrorMessage);
                }
            }
        }

        return result;
    }
}
