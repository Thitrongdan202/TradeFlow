using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("InventoryStocks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.WarehouseId, x.ProductId, x.Condition }).IsUnique();

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
