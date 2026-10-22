using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class WarehouseVoucherConfiguration : IEntityTypeConfiguration<WarehouseVoucher>
{
    public void Configure(EntityTypeBuilder<WarehouseVoucher> builder)
    {
        builder.ToTable("WarehouseVouchers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VoucherNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.VoucherNumber).IsUnique();

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PartnerName).HasMaxLength(255);
        builder.Property(x => x.PartnerAddress).HasMaxLength(500);
        builder.Property(x => x.SourceModule).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceReference).HasMaxLength(100);

        builder.Property(x => x.TotalQuantity).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(100);
        builder.Property(x => x.CreatedByName).HasMaxLength(255);
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(100);
        builder.Property(x => x.ApprovedByName).HasMaxLength(255);
        builder.Property(x => x.WarehouseKeeperName).HasMaxLength(255);
        builder.Property(x => x.DelivererOrReceiverName).HasMaxLength(255);

        builder.HasIndex(x => x.VoucherDate);
        builder.HasIndex(x => new { x.WarehouseId, x.VoucherType, x.Status });

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.WarehouseVoucher)
            .HasForeignKey(i => i.WarehouseVoucherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
