using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Settings;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");

        builder.HasKey(x => x.Id);

        // Singleton: always Id = 1
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TradingName)
            .HasMaxLength(200);

        builder.Property(x => x.TaxCode)
            .HasMaxLength(50);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Website)
            .HasMaxLength(256);

        builder.Property(x => x.LogoPath)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(256);
    }
}
