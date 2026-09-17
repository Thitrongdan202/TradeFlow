using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVatClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaxReductionEligible",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TaxEffectiveFrom",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TaxEffectiveTo",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxTreatment",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTaxReductionEligible",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxEffectiveFrom",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxEffectiveTo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxTreatment",
                table: "Products");
        }
    }
}
