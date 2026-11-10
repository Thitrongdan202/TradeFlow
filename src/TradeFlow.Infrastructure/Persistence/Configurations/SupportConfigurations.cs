using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Support;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);

        builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SubmitterFullName).HasMaxLength(150);
        builder.Property(x => x.SubmitterRole).HasMaxLength(100);
        builder.Property(x => x.SubmitterDepartment).HasMaxLength(150);
        builder.Property(x => x.CurrentRoute).HasMaxLength(255);
        builder.Property(x => x.AppVersion).HasMaxLength(100);

        builder.Property(x => x.AssignedToUserId).HasMaxLength(100);
        builder.Property(x => x.AssignedToUserName).HasMaxLength(100);
        builder.Property(x => x.AssignedToFullName).HasMaxLength(150);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.RelatedModule);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Comments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AuditLogs)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupportAttachmentConfiguration : IEntityTypeConfiguration<SupportAttachment>
{
    public void Configure(EntityTypeBuilder<SupportAttachment> builder)
    {
        builder.ToTable("SupportAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UploadedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UploadedByUserName).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.CommentId);
    }
}

public class SupportCommentConfiguration : IEntityTypeConfiguration<SupportComment>
{
    public void Configure(EntityTypeBuilder<SupportComment> builder)
    {
        builder.ToTable("SupportComments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AuthorUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AuthorUserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AuthorFullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.AuthorRole).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.Comment)
            .HasForeignKey(x => x.CommentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SupportAuditLogConfiguration : IEntityTypeConfiguration<SupportAuditLog>
{
    public void Configure(EntityTypeBuilder<SupportAuditLog> builder)
    {
        builder.ToTable("SupportAuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PerformedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PerformedByUserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
