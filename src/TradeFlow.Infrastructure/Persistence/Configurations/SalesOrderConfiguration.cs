using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Sales;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CustomerTaxCode).HasMaxLength(50);
        builder.Property(x => x.CustomerAddress).HasMaxLength(1000);
        builder.Property(x => x.CustomerPhone).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.OrderDate);
        builder.HasIndex(x => x.CustomerId);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
