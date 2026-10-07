using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_CompanyBrandedLoading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowCompanyNameOnLoading",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLoadingScreen",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SpinnerColor",
                table: "CompanySettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "#10b981");

            migrationBuilder.AddColumn<double>(
                name: "SpinnerOpacity",
                table: "CompanySettings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.80000000000000004);

            migrationBuilder.AddColumn<string>(
                name: "SpinnerSpeed",
                table: "CompanySettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "normal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowCompanyNameOnLoading",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ShowLoadingScreen",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "SpinnerColor",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "SpinnerOpacity",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "SpinnerSpeed",
                table: "CompanySettings");
        }
    }
}
