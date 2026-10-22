using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class StocktakeSessionConfiguration : IEntityTypeConfiguration<StocktakeSession>
{
    public void Configure(EntityTypeBuilder<StocktakeSession> builder)
    {
        builder.ToTable("StocktakeSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Scope).HasMaxLength(50).IsRequired().HasDefaultValue("All");
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.CountedByUserId).HasMaxLength(100);
        builder.Property(x => x.CountedByName).HasMaxLength(255);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(100);
        builder.Property(x => x.ReviewedByName).HasMaxLength(255);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(x => x.StocktakeDate);
        builder.HasIndex(x => new { x.WarehouseId, x.Status });

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AdjustmentInboundVoucher)
            .WithMany()
            .HasForeignKey(x => x.AdjustmentInboundVoucherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AdjustmentOutboundVoucher)
            .WithMany()
            .HasForeignKey(x => x.AdjustmentOutboundVoucherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.StocktakeSession)
            .HasForeignKey(i => i.StocktakeSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
