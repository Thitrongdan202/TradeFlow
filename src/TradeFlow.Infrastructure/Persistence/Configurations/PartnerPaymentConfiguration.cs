using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Partners;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class PartnerPaymentConfiguration : IEntityTypeConfiguration<PartnerPayment>
{
    public void Configure(EntityTypeBuilder<PartnerPayment> builder)
    {
        builder.ToTable("PartnerPayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ReferenceCode)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(x => x.PaymentCode).IsUnique();
        builder.HasIndex(x => new { x.PartnerType, x.PartnerId });
        builder.HasIndex(x => x.PaymentDate);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.SalesOrderId);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesOrder)
            .WithMany()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
