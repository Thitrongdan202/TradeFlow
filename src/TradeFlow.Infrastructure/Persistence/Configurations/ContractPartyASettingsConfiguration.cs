using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Contracts;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ContractPartyASettingsConfiguration : IEntityTypeConfiguration<ContractPartyASettings>
{
    public void Configure(EntityTypeBuilder<ContractPartyASettings> builder)
    {
        builder.ToTable("ContractPartyASettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.TradingName).HasMaxLength(100);
        builder.Property(x => x.TaxCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BusinessRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.RegisteredAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContactAddress).HasMaxLength(500);
        builder.Property(x => x.RepresentativeName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RepresentativePosition).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RepresentativeAuthority).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(250);
        builder.Property(x => x.BankAccount).HasMaxLength(50);
        builder.Property(x => x.BankAccountHolder).HasMaxLength(150);
        builder.Property(x => x.BankBranch).HasMaxLength(150);
        builder.Property(x => x.DefaultPaymentInstructions).HasMaxLength(2000);
        builder.Property(x => x.DefaultDeliveryTerms).HasMaxLength(2000);
        builder.Property(x => x.DefaultWarrantyTerms).HasMaxLength(2000);
    }
}
