using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDocumentSettingsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolder",
                table: "CompanySettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOrderNote",
                table: "CompanySettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultVatNote",
                table: "CompanySettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderFooterNote1",
                table: "CompanySettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderFooterNote2",
                table: "CompanySettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderHotline",
                table: "CompanySettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderQrCodePath",
                table: "CompanySettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountHolder",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultOrderNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultVatNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "OrderFooterNote1",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "OrderFooterNote2",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "OrderHotline",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "OrderQrCodePath",
                table: "CompanySettings");
        }
    }
}
