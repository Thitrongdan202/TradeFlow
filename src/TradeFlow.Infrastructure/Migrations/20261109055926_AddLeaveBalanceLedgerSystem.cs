using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalanceLedgerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveBalanceLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    DaysChanged = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LeaveRequestId = table.Column<int>(type: "integer", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PerformedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalanceLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceLedgers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceLedgers_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceLedgers_CreatedAt",
                table: "LeaveBalanceLedgers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceLedgers_EmployeeId",
                table: "LeaveBalanceLedgers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceLedgers_LeaveRequestId",
                table: "LeaveBalanceLedgers",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceLedgers_TransactionType",
                table: "LeaveBalanceLedgers",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceLedgers_Year",
                table: "LeaveBalanceLedgers",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveBalanceLedgers");
        }
    }
}
