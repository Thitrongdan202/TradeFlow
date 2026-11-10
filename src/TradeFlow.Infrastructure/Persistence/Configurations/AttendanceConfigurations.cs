using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeFlow.Domain.Entities.Attendance;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ToTable("WorkShifts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AppliesTo).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

public class EmployeeShiftAssignmentConfiguration : IEntityTypeConfiguration<EmployeeShiftAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeShiftAssignment> builder)
    {
        builder.ToTable("EmployeeShiftAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssignedBy).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.WorkShiftId);
        builder.HasIndex(x => x.EffectiveFrom);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.WorkShift)
            .WithMany(s => s.ShiftAssignments)
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClockInSource).HasMaxLength(50);
        builder.Property(x => x.ClockOutSource).HasMaxLength(50);
        builder.Property(x => x.ClockInDevice).HasMaxLength(200);
        builder.Property(x => x.ClockOutDevice).HasMaxLength(200);
        builder.Property(x => x.ClockInIp).HasMaxLength(50);
        builder.Property(x => x.ClockOutIp).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.WorkDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate });

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.WorkShift)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AttendanceAdjustmentRequestConfiguration : IEntityTypeConfiguration<AttendanceAdjustmentRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceAdjustmentRequest> builder)
    {
        builder.ToTable("AttendanceAdjustmentRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
        builder.Property(x => x.ReviewedByUserName).HasMaxLength(200);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.TargetDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AttendanceRecord)
            .WithMany(r => r.AdjustmentRequests)
            .HasForeignKey(x => x.AttendanceRecordId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalDays).HasPrecision(4, 1);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
        builder.Property(x => x.ReviewedByUserName).HasMaxLength(200);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.StartDate);
        builder.HasIndex(x => x.EndDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShiftChangeRequestConfiguration : IEntityTypeConfiguration<ShiftChangeRequest>
{
    public void Configure(EntityTypeBuilder<ShiftChangeRequest> builder)
    {
        builder.ToTable("ShiftChangeRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
        builder.Property(x => x.ReviewedByUserName).HasMaxLength(200);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.TargetDate);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CurrentWorkShift)
            .WithMany()
            .HasForeignKey(x => x.CurrentWorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProposedWorkShift)
            .WithMany()
            .HasForeignKey(x => x.ProposedWorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SubstituteEmployee)
            .WithMany()
            .HasForeignKey(x => x.SubstituteEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AttendancePolicyConfiguration : IEntityTypeConfiguration<AttendancePolicy>
{
    public void Configure(EntityTypeBuilder<AttendancePolicy> builder)
    {
        builder.ToTable("AttendancePolicies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PolicyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExemptEmployeeIds).HasMaxLength(500);

        builder.HasIndex(x => x.IsActive);
    }
}

public class AttendanceAuditLogConfiguration : IEntityTypeConfiguration<AttendanceAuditLog>
{
    public void Configure(EntityTypeBuilder<AttendanceAuditLog> builder)
    {
        builder.ToTable("AttendanceAuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ChangedByUserId).HasMaxLength(450);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => x.AttendanceRecordId);
        builder.HasIndex(x => x.RequestId);
        builder.HasIndex(x => x.ChangedAt);
        builder.HasIndex(x => x.Action);
    }
}

public class LeaveBalanceLedgerConfiguration : IEntityTypeConfiguration<LeaveBalanceLedger>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceLedger> builder)
    {
        builder.ToTable("LeaveBalanceLedgers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DaysChanged).HasPrecision(4, 1);
        builder.Property(x => x.BalanceAfter).HasPrecision(4, 1);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.PerformedByUserId).HasMaxLength(450);
        builder.Property(x => x.PerformedByUserName).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Year);
        builder.HasIndex(x => x.TransactionType);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LeaveRequest)
            .WithMany()
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
