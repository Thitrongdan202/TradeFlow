using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Direction).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.PreviousBalance).HasPrecision(18, 4);
        builder.Property(x => x.NewBalance).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);

        builder.Property(x => x.VoucherNumber).HasMaxLength(50);
        builder.Property(x => x.SourceReference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(100);
        builder.Property(x => x.CreatedByName).HasMaxLength(255);

        builder.HasIndex(x => x.MovementDate);
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId, x.MovementDate });
        builder.HasIndex(x => x.SourceReference);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany(v => v.Movements)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
