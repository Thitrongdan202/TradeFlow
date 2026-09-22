using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Documents;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        builder.ToTable("DocumentAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Active");

        builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
