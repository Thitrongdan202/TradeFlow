using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeEmployeeAndRequestIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRequests_CreatedAt",
                table: "EmployeeRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRequests_Status_CreatedAt",
                table: "EmployeeRequests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeRequests_CreatedAt",
                table: "EmployeeRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRequests_Status_CreatedAt",
                table: "EmployeeRequests");
        }
    }
}
