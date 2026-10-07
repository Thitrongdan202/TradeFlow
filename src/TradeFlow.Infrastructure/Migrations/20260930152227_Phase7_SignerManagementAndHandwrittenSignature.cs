using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_SignerManagementAndHandwrittenSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignerIdentities_CertificateSerialNumber",
                table: "SignerIdentities");

            migrationBuilder.AddColumn<int>(
                name: "FailedPinAttempts",
                table: "SignerIdentities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HandwrittenSignatureImage",
                table: "SignerIdentities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "SignerIdentities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "SignerEnrollmentCodes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "HasExpiration",
                table: "SignerEnrollmentCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SignerHandwrittenSignatureImage",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignerIdentities_CertificateSerialNumber",
                table: "SignerIdentities",
                column: "CertificateSerialNumber",
                unique: true,
                filter: "\"CertificateSerialNumber\" IS NOT NULL AND \"CertificateSerialNumber\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignerIdentities_CertificateSerialNumber",
                table: "SignerIdentities");

            migrationBuilder.DropColumn(
                name: "FailedPinAttempts",
                table: "SignerIdentities");

            migrationBuilder.DropColumn(
                name: "HandwrittenSignatureImage",
                table: "SignerIdentities");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "SignerIdentities");

            migrationBuilder.DropColumn(
                name: "HasExpiration",
                table: "SignerEnrollmentCodes");

            migrationBuilder.DropColumn(
                name: "SignerHandwrittenSignatureImage",
                table: "Invoices");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "SignerEnrollmentCodes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignerIdentities_CertificateSerialNumber",
                table: "SignerIdentities",
                column: "CertificateSerialNumber",
                unique: true);
        }
    }
}
