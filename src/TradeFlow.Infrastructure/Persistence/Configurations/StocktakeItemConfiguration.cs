using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Warehouses;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class StocktakeItemConfiguration : IEntityTypeConfiguration<StocktakeItem>
{
    public void Configure(EntityTypeBuilder<StocktakeItem> builder)
    {
        builder.ToTable("StocktakeItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitName).HasMaxLength(50).IsRequired();

        builder.Property(x => x.BookQuantity).HasPrecision(18, 4);
        builder.Property(x => x.CountedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.VarianceQuantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.VarianceValue).HasPrecision(18, 2);

        builder.Property(x => x.DiscrepancyReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.StocktakeSessionId, x.ProductId });

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
