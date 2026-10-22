using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class WarehouseReturnOrderConfiguration : IEntityTypeConfiguration<WarehouseReturnOrder>
{
    public void Configure(EntityTypeBuilder<WarehouseReturnOrder> builder)
    {
        builder.ToTable("WarehouseReturnOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.CustomerName).HasMaxLength(255);
        builder.Property(x => x.SupplierName).HasMaxLength(255);
        builder.Property(x => x.OriginalInvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.OriginalSalesOrderCode).HasMaxLength(50);

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ReceivedByUserId).HasMaxLength(100);
        builder.Property(x => x.ReceivedByName).HasMaxLength(255);
        builder.Property(x => x.InspectedByUserId).HasMaxLength(100);
        builder.Property(x => x.InspectedByName).HasMaxLength(255);
        builder.Property(x => x.InspectionNotes).HasMaxLength(1000);
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(100);
        builder.Property(x => x.ApprovedByName).HasMaxLength(255);

        builder.HasIndex(x => x.ReturnDate);
        builder.HasIndex(x => new { x.WarehouseId, x.ReturnType, x.Status });

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ResultingVoucher)
            .WithMany()
            .HasForeignKey(x => x.ResultingVoucherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.WarehouseReturnOrder)
            .HasForeignKey(i => i.WarehouseReturnOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
