using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class WarehouseVoucherItemConfiguration : IEntityTypeConfiguration<WarehouseVoucherItem>
{
    public void Configure(EntityTypeBuilder<WarehouseVoucherItem> builder)
    {
        builder.ToTable("WarehouseVoucherItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitName).HasMaxLength(50).IsRequired();

        builder.Property(x => x.DocumentQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ActualQuantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
