using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class WarehouseReturnOrderItemConfiguration : IEntityTypeConfiguration<WarehouseReturnOrderItem>
{
    public void Configure(EntityTypeBuilder<WarehouseReturnOrderItem> builder)
    {
        builder.ToTable("WarehouseReturnOrderItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitName).HasMaxLength(50).IsRequired();

        builder.Property(x => x.RequestedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);

        builder.Property(x => x.DefectDetails).HasMaxLength(1000);
        builder.Property(x => x.InspectionNotes).HasMaxLength(1000);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
