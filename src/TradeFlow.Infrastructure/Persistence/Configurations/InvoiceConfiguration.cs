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
        
        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CompanyTaxCode).HasMaxLength(50);
        builder.Property(x => x.CompanyAddress).HasMaxLength(1000);
        builder.Property(x => x.CompanyPhone).HasMaxLength(50);
        builder.Property(x => x.CompanyEmail).HasMaxLength(100);
        builder.Property(x => x.CompanyLogoUrl).HasMaxLength(500);

        builder.Property(x => x.CustomerName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CustomerCompanyName).HasMaxLength(255);
        builder.Property(x => x.CustomerTaxCode).HasMaxLength(50);
        builder.Property(x => x.CustomerAddress).HasMaxLength(1000);
        builder.Property(x => x.CustomerEmail).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
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
            .OnDelete(DeleteBehavior.Restrict);
    }
}
