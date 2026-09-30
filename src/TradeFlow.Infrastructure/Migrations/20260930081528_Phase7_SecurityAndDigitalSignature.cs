using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_SecurityAndDigitalSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateSerialNumber",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentHash",
                table: "Invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastVerifiedBy",
                table: "Invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignerIdentityId",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignerPosition",
                table: "Invoices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignerRole",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SigningProvider",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanySecuritySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequireSignerPin = table.Column<bool>(type: "boolean", nullable: false),
                    PinMinLength = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentCodeExpirationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CertExpirationWarningDays = table.Column<int>(type: "integer", nullable: false),
                    AllowAdminSignerEnrollment = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRevokeOnTermination = table.Column<bool>(type: "boolean", nullable: false),
                    MaxFailedSignAttempts = table.Column<int>(type: "integer", nullable: false),
                    DefaultSigningProvider = table.Column<int>(type: "integer", nullable: false),
                    XmlDsigCanonicalizationMethod = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    XmlDsigSignatureMethod = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    XmlDsigDigestMethod = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySecuritySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSignatureAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignerIdentityId = table.Column<int>(type: "integer", nullable: true),
                    SignerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SignerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignerPosition = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SignerRole = table.Column<int>(type: "integer", nullable: false),
                    SigningTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignatureStatus = table.Column<int>(type: "integer", nullable: false),
                    DocumentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SignatureValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CertificateSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificateSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificationResult = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastVerifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSignatureAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignerEnrollmentCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetPosition = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TargetSignerRole = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignerEnrollmentCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignerIdentities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Position = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SignerRole = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    CertificateSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CertificateSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CertificateIssuer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CertificateThumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EncryptedPrivateKey = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    KeySalt = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PinVerificationHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PublicKeyXml = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PublicKeyPem = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EnrollmentCodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EnrolledBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignerIdentities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SignerIdentityId",
                table: "Invoices",
                column: "SignerIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSignatureAudits_DocumentType_DocumentId",
                table: "DocumentSignatureAudits",
                columns: new[] { "DocumentType", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSignatureAudits_SignatureStatus",
                table: "DocumentSignatureAudits",
                column: "SignatureStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSignatureAudits_SignerIdentityId",
                table: "DocumentSignatureAudits",
                column: "SignerIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSignatureAudits_SigningTime",
                table: "DocumentSignatureAudits",
                column: "SigningTime");

            migrationBuilder.CreateIndex(
                name: "IX_SignerEnrollmentCodes_CodeHash",
                table: "SignerEnrollmentCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignerEnrollmentCodes_IsUsed_IsRevoked_ExpiresAt",
                table: "SignerEnrollmentCodes",
                columns: new[] { "IsUsed", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SignerEnrollmentCodes_TargetUserId",
                table: "SignerEnrollmentCodes",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SignerIdentities_CertificateSerialNumber",
                table: "SignerIdentities",
                column: "CertificateSerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignerIdentities_Status",
                table: "SignerIdentities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SignerIdentities_UserId",
                table: "SignerIdentities",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SignerIdentities_SignerIdentityId",
                table: "Invoices",
                column: "SignerIdentityId",
                principalTable: "SignerIdentities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SignerIdentities_SignerIdentityId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "CompanySecuritySettings");

            migrationBuilder.DropTable(
                name: "DocumentSignatureAudits");

            migrationBuilder.DropTable(
                name: "SignerEnrollmentCodes");

            migrationBuilder.DropTable(
                name: "SignerIdentities");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SignerIdentityId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CertificateSerialNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DocumentHash",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastVerifiedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignerIdentityId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignerPosition",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SignerRole",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SigningProvider",
                table: "Invoices");
        }
    }
}
