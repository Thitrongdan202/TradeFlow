using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBankAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyBankAccount",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerBankAccount",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "CompanySettings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyBankAccount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerBankAccount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "CompanySettings");
        }
    }
}
