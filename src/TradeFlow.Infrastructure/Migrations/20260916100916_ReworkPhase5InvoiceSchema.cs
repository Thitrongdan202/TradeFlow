using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkPhase5InvoiceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerBankAccount",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyBankAccount",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateSubject",
                table: "Invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyBankName",
                table: "Invoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerBankName",
                table: "Invoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormNumber",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "1");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "00000001");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceSeries",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "1C26TFL");

            migrationBuilder.AddColumn<string>(
                name: "QrCodeData",
                table: "Invoices",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignatureStatus",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SignatureValue",
                table: "Invoices",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Invoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxAuthorityCode",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "InvoiceItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FormNumber_InvoiceSeries_InvoiceNo",
                table: "Invoices",
                columns: new[] { "FormNumber", "InvoiceSeries", "InvoiceNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_FormNumber_InvoiceSeries_InvoiceNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CertificateSubject",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyBankName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerBankName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FormNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceSeries",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "QrCodeData",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignatureStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignatureValue",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxAuthorityCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "InvoiceItems");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerBankAccount",
                table: "Invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyBankAccount",
                table: "Invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
