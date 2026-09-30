using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Security;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class SignerIdentityConfiguration : IEntityTypeConfiguration<SignerIdentity>
{
    public void Configure(EntityTypeBuilder<SignerIdentity> builder)
    {
        builder.ToTable("SignerIdentities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Position).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CertificateSerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CertificateSubject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CertificateIssuer).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CertificateThumbprint).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EncryptedPrivateKey).HasMaxLength(4000);
        builder.Property(x => x.KeySalt).HasMaxLength(256);
        builder.Property(x => x.PinVerificationHash).HasMaxLength(256);
        builder.Property(x => x.PublicKeyXml).HasMaxLength(4000);
        builder.Property(x => x.PublicKeyPem).HasMaxLength(4000);
        builder.Property(x => x.EnrollmentCodeHash).HasMaxLength(128);
        builder.Property(x => x.EnrolledBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RevokedBy).HasMaxLength(256);
        builder.Property(x => x.RevocationReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CertificateSerialNumber).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
