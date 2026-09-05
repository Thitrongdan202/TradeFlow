using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3A_ArchitectureRefinement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Warehouses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewCode",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SequenceKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentNumber = table.Column<long>(type: "bigint", nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false),
                    FormatPattern = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessCode",
                table: "Warehouses",
                column: "BusinessCode");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BusinessCode",
                table: "Suppliers",
                column: "BusinessCode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_LegacyCode",
                table: "Products",
                column: "LegacyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NewCode",
                table: "Products",
                column: "NewCode");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BusinessCode",
                table: "Customers",
                column: "BusinessCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSequences_SequenceKey",
                table: "SystemSequences",
                column: "SequenceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "SystemSequences");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BusinessCode",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_BusinessCode",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Products_LegacyCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_NewCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Customers_BusinessCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NewCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Customers");
        }
    }
}
