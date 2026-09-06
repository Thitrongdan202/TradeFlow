using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Pricing;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("PriceListItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Group).HasMaxLength(200);
        builder.Property(x => x.NewCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LegacyCode).HasMaxLength(100);
        builder.Property(x => x.ProductInfo).HasMaxLength(2000);
        builder.Property(x => x.ImageStorageRef).HasMaxLength(500);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => x.PriceListId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.NewCode);
        builder.HasIndex(x => x.LegacyCode);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}