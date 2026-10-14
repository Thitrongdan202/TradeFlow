using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Contracts;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("ContractTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TypicalUseCase).HasMaxLength(1000);
        builder.Property(x => x.RequiredInfo).HasMaxLength(1000);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.Version).HasMaxLength(20);
        builder.Property(x => x.LegalBasis).HasMaxLength(2000);
        builder.Property(x => x.KnownLimitations).HasMaxLength(2000);
        builder.Property(x => x.DefaultClausesJson).HasColumnType("text");

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.DisplayOrder);
    }
}
