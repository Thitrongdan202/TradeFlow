using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Employees;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AttendanceCode).HasMaxLength(50);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NationalId).HasMaxLength(50);
        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.AvatarPath).HasMaxLength(500);

        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Facebook).HasMaxLength(200);
        builder.Property(x => x.MobileDevice).HasMaxLength(200);

        builder.Property(x => x.WorkingBranch).HasMaxLength(200);
        builder.Property(x => x.PayrollBranch).HasMaxLength(200);
        builder.Property(x => x.TerminationReason).HasMaxLength(1000);
        builder.Property(x => x.BankInformation).HasMaxLength(500);
        builder.Property(x => x.DebtAndAdvance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.UserId).HasMaxLength(450);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Phone);
        builder.HasIndex(x => x.NationalId);
        builder.HasIndex(x => x.AttendanceCode);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
