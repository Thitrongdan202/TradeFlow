using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(10);
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.DecimalPlaces).HasDefaultValue(2);
        builder.Property(x => x.RateSource).HasMaxLength(50).HasDefaultValue("Thủ công");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsManualOverride).HasDefaultValue(false);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
