using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Pricing;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("PriceLists");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QuotationNumber).HasMaxLength(100);
        builder.Property(x => x.ProgramTitle).HasMaxLength(500);
        builder.Property(x => x.PriceCondition).HasMaxLength(1000);
        builder.Property(x => x.VatNote).HasMaxLength(500);
        builder.Property(x => x.OriginalFileName).HasMaxLength(260);
        builder.Property(x => x.OriginalFileStorageRef).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Year);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.PriceList)
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}