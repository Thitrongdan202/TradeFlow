using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttendanceRecordId = table.Column<int>(type: "integer", nullable: true),
                    RequestId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ChangedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldValuesJson = table.Column<string>(type: "text", nullable: true),
                    NewValuesJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyLeaveToleranceMinutes = table.Column<int>(type: "integer", nullable: false),
                    HalfDayLateMinutes = table.Column<int>(type: "integer", nullable: false),
                    AllowSelfAdjustment = table.Column<bool>(type: "boolean", nullable: false),
                    RequireManagerApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequireHrApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AutoClockOutTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ExemptEmployeeIds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveType = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalDays = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShiftType = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "integer", nullable: false),
                    FlexibleMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsOvernight = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AppliesTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    WorkShiftId = table.Column<int>(type: "integer", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClockInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockInSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClockOutSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClockInDevice = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClockOutDevice = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClockInIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClockOutIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WorkingMinutes = table.Column<int>(type: "integer", nullable: false),
                    LateMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "integer", nullable: false),
                    OvertimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsOvernight = table.Column<bool>(type: "boolean", nullable: false),
                    IsManuallyAdjusted = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    WorkShiftId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    SpecificDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeShiftAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeShiftAssignments_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentWorkShiftId = table.Column<int>(type: "integer", nullable: false),
                    ProposedWorkShiftId = table.Column<int>(type: "integer", nullable: false),
                    SubstituteEmployeeId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Employees_SubstituteEmployeeId",
                        column: x => x.SubstituteEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_WorkShifts_CurrentWorkShiftId",
                        column: x => x.CurrentWorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_WorkShifts_ProposedWorkShiftId",
                        column: x => x.ProposedWorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AttendanceRecordId = table.Column<int>(type: "integer", nullable: true),
                    RequestType = table.Column<int>(type: "integer", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProposedClockIn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProposedClockOut = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AttendanceRecords_AttendanceRe~",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_AttendanceRecordId",
                table: "AttendanceAdjustmentRequests",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_EmployeeId",
                table: "AttendanceAdjustmentRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_Status",
                table: "AttendanceAdjustmentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_TargetDate",
                table: "AttendanceAdjustmentRequests",
                column: "TargetDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_Action",
                table: "AttendanceAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_AttendanceRecordId",
                table: "AttendanceAuditLogs",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_ChangedAt",
                table: "AttendanceAuditLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_RequestId",
                table: "AttendanceAuditLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_IsActive",
                table: "AttendancePolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId",
                table: "AttendanceRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_WorkDate",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Status",
                table: "AttendanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_WorkDate",
                table: "AttendanceRecords",
                column: "WorkDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_WorkShiftId",
                table: "AttendanceRecords",
                column: "WorkShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftAssignments_EffectiveFrom",
                table: "EmployeeShiftAssignments",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftAssignments_EmployeeId",
                table: "EmployeeShiftAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftAssignments_IsActive",
                table: "EmployeeShiftAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftAssignments_WorkShiftId",
                table: "EmployeeShiftAssignments",
                column: "WorkShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EndDate",
                table: "LeaveRequests",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StartDate",
                table: "LeaveRequests",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_CurrentWorkShiftId",
                table: "ShiftChangeRequests",
                column: "CurrentWorkShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_EmployeeId",
                table: "ShiftChangeRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_ProposedWorkShiftId",
                table: "ShiftChangeRequests",
                column: "ProposedWorkShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_Status",
                table: "ShiftChangeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_SubstituteEmployeeId",
                table: "ShiftChangeRequests",
                column: "SubstituteEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_TargetDate",
                table: "ShiftChangeRequests",
                column: "TargetDate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_Code",
                table: "WorkShifts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_IsActive",
                table: "WorkShifts",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAdjustmentRequests");

            migrationBuilder.DropTable(
                name: "AttendanceAuditLogs");

            migrationBuilder.DropTable(
                name: "AttendancePolicies");

            migrationBuilder.DropTable(
                name: "EmployeeShiftAssignments");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "ShiftChangeRequests");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "WorkShifts");
        }
    }
}
