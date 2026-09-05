using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BusinessCode).HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(300);
        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.ContactPerson).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.BusinessCode);
        builder.HasIndex(x => x.Phone);
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.TaxCode);
    }
}