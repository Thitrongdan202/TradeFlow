using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractPartyASettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TradingName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BusinessRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegisteredAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepresentativeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RepresentativePosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RepresentativeAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BankName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankAccountHolder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DefaultPaymentInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultDeliveryTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultWarrantyTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPartyASettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContractDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractPlace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PartyBType = table.Column<int>(type: "integer", nullable: false),
                    PartnerId = table.Column<int>(type: "integer", nullable: true),
                    PartyAName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PartyATaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyAAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PartyAContactAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PartyARepresentative = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyAPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PartyAAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PartyAPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyAEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyABankName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PartyABankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyABankAccountHolder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyABankBranch = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyBName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PartyBTaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyBIdCardNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyBIdCardDate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyBIdCardPlace = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyBAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PartyBDeliveryAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PartyBPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartyBEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyBRepresentative = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyBPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PartyBAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PartyBBankName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PartyBBankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AmountInWords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeliveryTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InstallationTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    WarrantyTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BreachPenaltyTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DisputeResolutionTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    GeneralTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CustomClausesJson = table.Column<string>(type: "text", nullable: false),
                    ExecutionMethod = table.Column<int>(type: "integer", nullable: false),
                    PartyASignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PartyASignedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyASignatureData = table.Column<string>(type: "text", nullable: true),
                    PartyBSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PartyBSignedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PartyBSignatureData = table.Column<string>(type: "text", nullable: true),
                    DocumentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPaperPrinted = table.Column<bool>(type: "boolean", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrintedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaperSignedScanAttachmentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ParentContractId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Contracts_ParentContractId",
                        column: x => x.ParentContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Customers_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContractTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TypicalUseCase = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TargetPartyType = table.Column<int>(type: "integer", nullable: true),
                    RequiredInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    KnownLimitations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsReviewedByLegal = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultClausesJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Specifications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractItems_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_ContractId",
                table: "ContractItems",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractItems_ProductId",
                table: "ContractItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractDate",
                table: "Contracts",
                column: "ContractDate");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedByUserId",
                table: "Contracts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ParentContractId",
                table: "Contracts",
                column: "ParentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PartnerId",
                table: "Contracts",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Status",
                table: "Contracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TemplateCode",
                table: "Contracts",
                column: "TemplateCode");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_Category",
                table: "ContractTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_Code",
                table: "ContractTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_DisplayOrder",
                table: "ContractTemplates",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractItems");

            migrationBuilder.DropTable(
                name: "ContractPartyASettings");

            migrationBuilder.DropTable(
                name: "ContractTemplates");

            migrationBuilder.DropTable(
                name: "Contracts");
        }
    }
}
