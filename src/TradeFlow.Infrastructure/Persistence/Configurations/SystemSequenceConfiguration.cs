using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Settings;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class SystemSequenceConfiguration : IEntityTypeConfiguration<SystemSequence>
{
    public void Configure(EntityTypeBuilder<SystemSequence> builder)
    {
        builder.ToTable("SystemSequences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SequenceKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FormatPattern).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.SequenceKey).IsUnique();
    }
}