using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseOperationsAndStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LastMovementAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VoucherType = table.Column<int>(type: "integer", nullable: false),
                    VoucherNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PartnerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PartnerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceModule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceRecordId = table.Column<int>(type: "integer", nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WarehouseKeeperName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DelivererOrReceiverName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseVouchers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MovementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    NewBalance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherId = table.Column<int>(type: "integer", nullable: true),
                    VoucherNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_WarehouseVouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuarantinedInventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DefectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateIdentified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReportedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ResultingVoucherId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarantinedInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuarantinedInventoryItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuarantinedInventoryItems_WarehouseVouchers_ResultingVouche~",
                        column: x => x.ResultingVoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuarantinedInventoryItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    StocktakeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "All"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SnapshotTakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CountedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CountedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdjustmentInboundVoucherId = table.Column<int>(type: "integer", nullable: true),
                    AdjustmentOutboundVoucherId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_WarehouseVouchers_AdjustmentInboundVouche~",
                        column: x => x.AdjustmentInboundVoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_WarehouseVouchers_AdjustmentOutboundVouch~",
                        column: x => x.AdjustmentOutboundVoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StocktakeSessions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseReturnOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReturnType = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OriginalInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    OriginalInvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalSalesOrderId = table.Column<int>(type: "integer", nullable: true),
                    OriginalSalesOrderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceivedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InspectedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InspectedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InspectionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultingVoucherId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseReturnOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrders_WarehouseVouchers_ResultingVoucherId",
                        column: x => x.ResultingVoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseVoucherItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseVoucherId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    DocumentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseVoucherItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseVoucherItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseVoucherItems_WarehouseVouchers_WarehouseVoucherId",
                        column: x => x.WarehouseVoucherId,
                        principalTable: "WarehouseVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StocktakeSessionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VarianceQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VarianceValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscrepancyReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeItems_StocktakeSessions_StocktakeSessionId",
                        column: x => x.StocktakeSessionId,
                        principalTable: "StocktakeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseReturnOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseReturnOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Disposition = table.Column<int>(type: "integer", nullable: false),
                    DefectDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseReturnOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseReturnOrderItems_WarehouseReturnOrders_WarehouseRe~",
                        column: x => x.WarehouseReturnOrderId,
                        principalTable: "WarehouseReturnOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_MovementDate",
                table: "InventoryMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_SourceReference",
                table: "InventoryMovements",
                column: "SourceReference");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_VoucherId",
                table: "InventoryMovements",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseId_ProductId_MovementDate",
                table: "InventoryMovements",
                columns: new[] { "WarehouseId", "ProductId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_ProductId",
                table: "InventoryStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStocks_WarehouseId_ProductId_Condition",
                table: "InventoryStocks",
                columns: new[] { "WarehouseId", "ProductId", "Condition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedInventoryItems_Code",
                table: "QuarantinedInventoryItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedInventoryItems_DateIdentified",
                table: "QuarantinedInventoryItems",
                column: "DateIdentified");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedInventoryItems_ProductId",
                table: "QuarantinedInventoryItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedInventoryItems_ResultingVoucherId",
                table: "QuarantinedInventoryItems",
                column: "ResultingVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedInventoryItems_WarehouseId_Status",
                table: "QuarantinedInventoryItems",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_ProductId",
                table: "StocktakeItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_StocktakeSessionId_ProductId",
                table: "StocktakeItems",
                columns: new[] { "StocktakeSessionId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_AdjustmentInboundVoucherId",
                table: "StocktakeSessions",
                column: "AdjustmentInboundVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_AdjustmentOutboundVoucherId",
                table: "StocktakeSessions",
                column: "AdjustmentOutboundVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_Code",
                table: "StocktakeSessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_StocktakeDate",
                table: "StocktakeSessions",
                column: "StocktakeDate");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_WarehouseId_Status",
                table: "StocktakeSessions",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrderItems_ProductId",
                table: "WarehouseReturnOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrderItems_WarehouseReturnOrderId",
                table: "WarehouseReturnOrderItems",
                column: "WarehouseReturnOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_Code",
                table: "WarehouseReturnOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_CustomerId",
                table: "WarehouseReturnOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_ResultingVoucherId",
                table: "WarehouseReturnOrders",
                column: "ResultingVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_ReturnDate",
                table: "WarehouseReturnOrders",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_SupplierId",
                table: "WarehouseReturnOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReturnOrders_WarehouseId_ReturnType_Status",
                table: "WarehouseReturnOrders",
                columns: new[] { "WarehouseId", "ReturnType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVoucherItems_ProductId",
                table: "WarehouseVoucherItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVoucherItems_WarehouseVoucherId",
                table: "WarehouseVoucherItems",
                column: "WarehouseVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVouchers_VoucherDate",
                table: "WarehouseVouchers",
                column: "VoucherDate");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVouchers_VoucherNumber",
                table: "WarehouseVouchers",
                column: "VoucherNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseVouchers_WarehouseId_VoucherType_Status",
                table: "WarehouseVouchers",
                columns: new[] { "WarehouseId", "VoucherType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "InventoryStocks");

            migrationBuilder.DropTable(
                name: "QuarantinedInventoryItems");

            migrationBuilder.DropTable(
                name: "StocktakeItems");

            migrationBuilder.DropTable(
                name: "WarehouseReturnOrderItems");

            migrationBuilder.DropTable(
                name: "WarehouseVoucherItems");

            migrationBuilder.DropTable(
                name: "StocktakeSessions");

            migrationBuilder.DropTable(
                name: "WarehouseReturnOrders");

            migrationBuilder.DropTable(
                name: "WarehouseVouchers");
        }
    }
}
