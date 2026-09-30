using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Security;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class DocumentSignatureAuditConfiguration : IEntityTypeConfiguration<DocumentSignatureAudit>
{
    public void Configure(EntityTypeBuilder<DocumentSignatureAudit> builder)
    {
        builder.ToTable("DocumentSignatureAudits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SignerUserId).HasMaxLength(450);
        builder.Property(x => x.SignerName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SignerPosition).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DocumentHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SignatureValue).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CertificateSubject).HasMaxLength(500);
        builder.Property(x => x.CertificateSerialNumber).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(100);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.VerificationResult).HasMaxLength(500);
        builder.Property(x => x.LastVerifiedBy).HasMaxLength(256);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(x => new { x.DocumentType, x.DocumentId });
        builder.HasIndex(x => x.SignerIdentityId);
        builder.HasIndex(x => x.SigningTime);
        builder.HasIndex(x => x.SignatureStatus);
    }
}
