using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Contracts;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class EmploymentContractConfiguration : IEntityTypeConfiguration<EmploymentContract>
{
    public void Configure(EntityTypeBuilder<EmploymentContract> builder)
    {
        builder.ToTable("EmploymentContracts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContractPlace).HasMaxLength(100);

        // Bên A (Người sử dụng lao động)
        builder.Property(x => x.EmployerName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.EmployerTaxCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EmployerAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EmployerPhone).HasMaxLength(50);
        builder.Property(x => x.EmployerRepresentativeName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EmployerRepresentativePosition).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EmployerRepresentativeAuthority).HasMaxLength(200);

        // Bên B (Người lao động)
        builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EmployeeFullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EmployeeGender).HasMaxLength(20);
        builder.Property(x => x.EmployeeNationalId).HasMaxLength(50);
        builder.Property(x => x.EmployeeNationalIdIssuePlace).HasMaxLength(150);
        builder.Property(x => x.EmployeeAddress).HasMaxLength(500);
        builder.Property(x => x.EmployeePhone).HasMaxLength(50);
        builder.Property(x => x.EmployeeEmail).HasMaxLength(150);
        builder.Property(x => x.EmployeeBankInformation).HasMaxLength(255);

        // Công việc & Địa điểm
        builder.Property(x => x.JobTitle).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DepartmentName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Workplace).HasMaxLength(500).IsRequired();
        builder.Property(x => x.JobDescription).HasMaxLength(2000);
        builder.Property(x => x.WorkingSchedule).HasMaxLength(500);
        builder.Property(x => x.LaborProtection).HasMaxLength(1000);

        // Tiền lương & Thỏa thuận thù lao
        builder.Property(x => x.BasicSalary).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.SalaryClauseText).HasMaxLength(4000);
        builder.Property(x => x.SalaryType).HasMaxLength(100);
        builder.Property(x => x.PaymentPeriod).HasMaxLength(255);
        builder.Property(x => x.PaymentMethod).HasMaxLength(255);
        builder.Property(x => x.PositionAllowance).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.LunchAllowance).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.OtherAllowances).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.SalaryReviewPolicy).HasMaxLength(500);
        builder.Property(x => x.SocialInsuranceRate).HasPrecision(5, 2);
        builder.Property(x => x.ProbationSalaryPercentage).HasPrecision(5, 2);

        // Điều khoản tùy chọn & Thực tập/Đào tạo
        builder.Property(x => x.NdaClauseDetails).HasMaxLength(2000);
        builder.Property(x => x.TrainingClauseDetails).HasMaxLength(2000);
        builder.Property(x => x.SpecialBenefitsDetails).HasMaxLength(2000);
        builder.Property(x => x.TrainingSupervisor).HasMaxLength(150);
        builder.Property(x => x.TrainingObjectives).HasMaxLength(2000);
        builder.Property(x => x.InternshipProgramme).HasMaxLength(255);
        builder.Property(x => x.CustomTerms).HasMaxLength(4000);
        builder.Property(x => x.CustomClausesJson);

        // Ký & Xác thực
        builder.Property(x => x.DigitalSignatureHash).HasMaxLength(128);
        builder.Property(x => x.NationalPlatformReference).HasMaxLength(100);
        builder.Property(x => x.PaperScanAttachmentPath).HasMaxLength(500);

        // Lý do hủy / chấm dứt
        builder.Property(x => x.TerminationReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // Foreign keys & Relationships
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentContract)
            .WithMany()
            .HasForeignKey(x => x.ParentContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indices
        builder.HasIndex(x => x.ContractNumber).IsUnique();
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ContractType);
        builder.HasIndex(x => x.StartDate);
        builder.HasIndex(x => x.EndDate);
    }
}
