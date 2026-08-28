using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Users;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.PerformedBy)
            .HasMaxLength(256);

        builder.Property(x => x.TargetEntity)
            .HasMaxLength(100);

        builder.Property(x => x.TargetId)
            .HasMaxLength(100);

        builder.Property(x => x.Details)
            .HasMaxLength(4000);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        // Index for querying by performer and time
        builder.HasIndex(x => x.PerformedBy);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.EventType);
    }
}
