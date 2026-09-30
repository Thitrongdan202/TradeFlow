using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Security;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class SignerEnrollmentCodeConfiguration : IEntityTypeConfiguration<SignerEnrollmentCode>
{
    public void Configure(EntityTypeBuilder<SignerEnrollmentCode> builder)
    {
        builder.ToTable("SignerEnrollmentCodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.TargetUserName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TargetFullName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TargetPosition).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UsedBy).HasMaxLength(256);
        builder.Property(x => x.RevokedBy).HasMaxLength(256);
        builder.Property(x => x.RevocationReason).HasMaxLength(500);

        builder.HasIndex(x => x.CodeHash).IsUnique();
        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => new { x.IsUsed, x.IsRevoked, x.ExpiresAt });
    }
}
