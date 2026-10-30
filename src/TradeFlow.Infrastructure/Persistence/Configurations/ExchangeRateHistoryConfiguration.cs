using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ExchangeRateHistoryConfiguration : IEntityTypeConfiguration<ExchangeRateHistory>
{
    public void Configure(EntityTypeBuilder<ExchangeRateHistory> builder)
    {
        builder.ToTable("ExchangeRateHistories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.PreviousRate).HasPrecision(18, 6);
        builder.Property(x => x.Source).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.ChangedBy).HasMaxLength(100);

        builder.HasOne(x => x.Currency)
            .WithMany()
            .HasForeignKey(x => x.CurrencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CurrencyCode);
        builder.HasIndex(x => x.EffectiveAt);
        builder.HasIndex(x => x.CurrencyId);
    }
}
