using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class QuarantinedInventoryItemConfiguration : IEntityTypeConfiguration<QuarantinedInventoryItem>
{
    public void Configure(EntityTypeBuilder<QuarantinedInventoryItem> builder)
    {
        builder.ToTable("QuarantinedInventoryItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitName).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.DefectReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceReference).HasMaxLength(100);
        builder.Property(x => x.ReportedByName).HasMaxLength(255);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.Property(x => x.ResolvedByName).HasMaxLength(255);

        builder.HasIndex(x => x.DateIdentified);
        builder.HasIndex(x => new { x.WarehouseId, x.Status });

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ResultingVoucher)
            .WithMany()
            .HasForeignKey(x => x.ResultingVoucherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
