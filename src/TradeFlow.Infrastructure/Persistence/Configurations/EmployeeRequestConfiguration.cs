using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Employees;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class EmployeeRequestConfiguration : IEntityTypeConfiguration<EmployeeRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeRequest> builder)
    {
        builder.ToTable("EmployeeRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.AttendanceCode).HasMaxLength(50);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.NationalId).HasMaxLength(50);
        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.WorkingBranch).HasMaxLength(200);
        builder.Property(x => x.PayrollBranch).HasMaxLength(200);
        builder.Property(x => x.BankInformation).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ProposedUserId).HasMaxLength(450);

        builder.Property(x => x.RequestedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(200);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedBy);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedEmployee)
            .WithMany()
            .HasForeignKey(x => x.CreatedEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
