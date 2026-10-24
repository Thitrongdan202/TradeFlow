using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContractType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContractDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractPlace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProbationDurationDays = table.Column<int>(type: "integer", nullable: true),
                    ProbationSalaryPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    EmployerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmployerTaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmployerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmployerRepresentativeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmployerRepresentativePosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmployerRepresentativeAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeFullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeDateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployeeGender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmployeeNationalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmployeeNationalIdIssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployeeNationalIdIssuePlace = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EmployeeAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmployeePhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmployeeEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EmployeeBankInformation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Workplace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    JobDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WorkingHoursPerWeek = table.Column<int>(type: "integer", nullable: false),
                    WorkingSchedule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AnnualLeaveDays = table.Column<int>(type: "integer", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SalaryType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentPeriod = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LunchAllowance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SalaryReviewPolicy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SocialInsuranceRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LaborProtection = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HasNonDisclosureAgreement = table.Column<bool>(type: "boolean", nullable: false),
                    NdaClauseDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HasTrainingReimbursement = table.Column<bool>(type: "boolean", nullable: false),
                    TrainingClauseDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HasSpecialBenefits = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialBenefitsDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CustomTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SigningMethod = table.Column<int>(type: "integer", nullable: false),
                    EmployeeSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployeeSignatureData = table.Column<string>(type: "text", nullable: true),
                    EmployerSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployerSignatureData = table.Column<string>(type: "text", nullable: true),
                    DigitalSignatureHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NationalPlatformReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaperScanAttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ParentContractId = table.Column<int>(type: "integer", nullable: true),
                    RenewalCount = table.Column<int>(type: "integer", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmploymentContracts_EmploymentContracts_ParentContractId",
                        column: x => x.ParentContractId,
                        principalTable: "EmploymentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_ContractNumber",
                table: "EmploymentContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_ContractType",
                table: "EmploymentContracts",
                column: "ContractType");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_EmployeeId",
                table: "EmploymentContracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_EndDate",
                table: "EmploymentContracts",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_ParentContractId",
                table: "EmploymentContracts",
                column: "ParentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_StartDate",
                table: "EmploymentContracts",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_Status",
                table: "EmploymentContracts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmploymentContracts");
        }
    }
}
