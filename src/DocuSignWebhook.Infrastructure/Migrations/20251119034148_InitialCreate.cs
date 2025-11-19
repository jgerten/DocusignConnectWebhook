using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuSignWebhook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Envelopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocuSignEnvelopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SenderEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SenderName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentsDownloaded = table.Column<bool>(type: "boolean", nullable: false),
                    DocumentsDownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envelopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocuSignDocumentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    MinioBucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinioObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadedToMinIO = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvelopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnvelopeEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_Envelopes_EnvelopeEntityId",
                        column: x => x.EnvelopeEntityId,
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EnvelopeId_DocuSignDocumentId",
                table: "Documents",
                columns: new[] { "EnvelopeId", "DocuSignDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_MinioObjectKey",
                table: "Documents",
                column: "MinioObjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedToMinIO",
                table: "Documents",
                column: "UploadedToMinIO");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_CompletedAt",
                table: "Envelopes",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_DocuSignEnvelopeId",
                table: "Envelopes",
                column: "DocuSignEnvelopeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_Status",
                table: "Envelopes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EnvelopeEntityId",
                table: "WebhookEvents",
                column: "EnvelopeEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EnvelopeId",
                table: "WebhookEvents",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EnvelopeId_EventType",
                table: "WebhookEvents",
                columns: new[] { "EnvelopeId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ProcessingStatus",
                table: "WebhookEvents",
                column: "ProcessingStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "Envelopes");
        }
    }
}
