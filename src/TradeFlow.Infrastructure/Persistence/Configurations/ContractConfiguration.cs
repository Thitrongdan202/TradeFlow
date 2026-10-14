using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Contracts;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TemplateCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TemplateVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ContractPlace).HasMaxLength(100);

        // Bên A
        builder.Property(x => x.PartyAName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PartyATaxCode).HasMaxLength(50);
        builder.Property(x => x.PartyAAddress).HasMaxLength(500);
        builder.Property(x => x.PartyAContactAddress).HasMaxLength(500);
        builder.Property(x => x.PartyARepresentative).HasMaxLength(150);
        builder.Property(x => x.PartyAPosition).HasMaxLength(100);
        builder.Property(x => x.PartyAAuthority).HasMaxLength(200);
        builder.Property(x => x.PartyAPhone).HasMaxLength(50);
        builder.Property(x => x.PartyAEmail).HasMaxLength(150);
        builder.Property(x => x.PartyABankName).HasMaxLength(250);
        builder.Property(x => x.PartyABankAccount).HasMaxLength(50);
        builder.Property(x => x.PartyABankAccountHolder).HasMaxLength(150);
        builder.Property(x => x.PartyABankBranch).HasMaxLength(150);

        // Bên B
        builder.Property(x => x.PartyBName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PartyBTaxCode).HasMaxLength(50);
        builder.Property(x => x.PartyBIdCardNumber).HasMaxLength(50);
        builder.Property(x => x.PartyBIdCardDate).HasMaxLength(50);
        builder.Property(x => x.PartyBIdCardPlace).HasMaxLength(150);
        builder.Property(x => x.PartyBAddress).HasMaxLength(500);
        builder.Property(x => x.PartyBDeliveryAddress).HasMaxLength(500);
        builder.Property(x => x.PartyBPhone).HasMaxLength(50);
        builder.Property(x => x.PartyBEmail).HasMaxLength(150);
        builder.Property(x => x.PartyBRepresentative).HasMaxLength(150);
        builder.Property(x => x.PartyBPosition).HasMaxLength(100);
        builder.Property(x => x.PartyBAuthority).HasMaxLength(200);
        builder.Property(x => x.PartyBBankName).HasMaxLength(250);
        builder.Property(x => x.PartyBBankAccount).HasMaxLength(50);

        // Tiền tệ & Giá trị
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.RemainingAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10);
        builder.Property(x => x.AmountInWords).HasMaxLength(500);

        // Điều khoản text lớn
        builder.Property(x => x.PaymentTerms).HasMaxLength(4000);
        builder.Property(x => x.DeliveryTerms).HasMaxLength(4000);
        builder.Property(x => x.InstallationTerms).HasMaxLength(4000);
        builder.Property(x => x.WarrantyTerms).HasMaxLength(4000);
        builder.Property(x => x.BreachPenaltyTerms).HasMaxLength(4000);
        builder.Property(x => x.DisputeResolutionTerms).HasMaxLength(4000);
        builder.Property(x => x.GeneralTerms).HasMaxLength(4000);
        builder.Property(x => x.CustomClausesJson).HasColumnType("text");

        // Chữ ký & Hash
        builder.Property(x => x.PartyASignedBy).HasMaxLength(150);
        builder.Property(x => x.PartyBSignedBy).HasMaxLength(150);
        builder.Property(x => x.PartyASignatureData).HasColumnType("text");
        builder.Property(x => x.PartyBSignatureData).HasColumnType("text");
        builder.Property(x => x.DocumentHash).HasMaxLength(128);
        builder.Property(x => x.PrintedBy).HasMaxLength(150);

        // Người tạo
        builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FinalizedBy).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // Indexes
        builder.HasIndex(x => x.ContractNumber).IsUnique();
        builder.HasIndex(x => x.ContractDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.TemplateCode);

        // Quan hệ
        builder.HasOne(x => x.PartnerCustomer)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ParentContract)
            .WithMany(x => x.ChildContracts)
            .HasForeignKey(x => x.ParentContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Contract)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
