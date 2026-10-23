using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableSigningLocationAndRegime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultSigningLocation",
                table: "ContractPartyASettings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingRegime",
                table: "CompanySettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultSigningLocation",
                table: "ContractPartyASettings");

            migrationBuilder.DropColumn(
                name: "AccountingRegime",
                table: "CompanySettings");
        }
    }
}
