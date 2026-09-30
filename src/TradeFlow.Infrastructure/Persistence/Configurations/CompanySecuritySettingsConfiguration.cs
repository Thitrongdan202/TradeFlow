using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Security;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class CompanySecuritySettingsConfiguration : IEntityTypeConfiguration<CompanySecuritySettings>
{
    public void Configure(EntityTypeBuilder<CompanySecuritySettings> builder)
    {
        builder.ToTable("CompanySecuritySettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.XmlDsigCanonicalizationMethod).HasMaxLength(256).IsRequired();
        builder.Property(x => x.XmlDsigSignatureMethod).HasMaxLength(256).IsRequired();
        builder.Property(x => x.XmlDsigDigestMethod).HasMaxLength(256).IsRequired();
    }
}
