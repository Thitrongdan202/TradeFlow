using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Sales;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FormNumber).HasMaxLength(20).IsRequired().HasDefaultValue("1");
        builder.Property(x => x.InvoiceSeries).HasMaxLength(50).IsRequired().HasDefaultValue("1C26TFL");
        builder.Property(x => x.InvoiceNo).HasMaxLength(20).IsRequired().HasDefaultValue("00000001");
        
        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CompanyTaxCode).HasMaxLength(50);
        builder.Property(x => x.CompanyAddress).HasMaxLength(1000);
        builder.Property(x => x.CompanyPhone).HasMaxLength(50);
        builder.Property(x => x.CompanyEmail).HasMaxLength(100);
        builder.Property(x => x.CompanyBankAccount).HasMaxLength(50);
        builder.Property(x => x.CompanyBankName).HasMaxLength(255);
        builder.Property(x => x.CompanyLogoUrl).HasMaxLength(500);

        builder.Property(x => x.CustomerName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CustomerCompanyName).HasMaxLength(255);
        builder.Property(x => x.CustomerTaxCode).HasMaxLength(50);
        builder.Property(x => x.CustomerAddress).HasMaxLength(1000);
        builder.Property(x => x.CustomerEmail).HasMaxLength(100);
        builder.Property(x => x.CustomerBankAccount).HasMaxLength(50);
        builder.Property(x => x.CustomerBankName).HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

        builder.Property(x => x.TaxAuthorityCode).HasMaxLength(100);
        builder.Property(x => x.QrCodeData).HasMaxLength(2000);
        builder.Property(x => x.SignedBy).HasMaxLength(255);
        builder.Property(x => x.SignatureValue).HasMaxLength(4000);
        builder.Property(x => x.CertificateSubject).HasMaxLength(1000);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.FormNumber, x.InvoiceSeries, x.InvoiceNo });
        builder.HasIndex(x => x.InvoiceDate);
        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.CustomerId);

        builder.HasOne(x => x.SalesOrder)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
