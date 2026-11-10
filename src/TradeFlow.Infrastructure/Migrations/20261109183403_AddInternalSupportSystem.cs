using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalSupportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    RelatedModule = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubmitterFullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SubmitterRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubmitterDepartment = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    CurrentRoute = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedToUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedToUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedToFullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportAuditLogs_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorFullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AuthorRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsInternalNote = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportComments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<int>(type: "integer", nullable: false),
                    CommentId = table.Column<int>(type: "integer", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportAttachments_SupportComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "SupportComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportAttachments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportAttachments_CommentId",
                table: "SupportAttachments",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportAttachments_TicketId",
                table: "SupportAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportAuditLogs_CreatedAt",
                table: "SupportAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportAuditLogs_TicketId",
                table: "SupportAuditLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportComments_CreatedAt",
                table: "SupportComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportComments_TicketId",
                table: "SupportComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedToUserId",
                table: "SupportTickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Category",
                table: "SupportTickets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Code",
                table: "SupportTickets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAt",
                table: "SupportTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedByUserId",
                table: "SupportTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Priority",
                table: "SupportTickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_RelatedModule",
                table: "SupportTickets",
                column: "RelatedModule");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");

            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportAttachments");

            migrationBuilder.DropTable(
                name: "SupportAuditLogs");

            migrationBuilder.DropTable(
                name: "SupportComments");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            }
    }
}
