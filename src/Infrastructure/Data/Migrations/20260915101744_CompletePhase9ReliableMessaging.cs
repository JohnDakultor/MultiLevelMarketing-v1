using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase9ReliableMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DurableBackgroundJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ClaimedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClaimedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DurableBackgroundJobs", x => x.Id);
                    table.CheckConstraint("CK_DurableBackgroundJobs_FinalState", "NOT (\"CompletedAt\" IS NOT NULL AND \"DeadLetteredAt\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeyHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    ProtectedOutcome = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.CheckConstraint("CK_IdempotencyRecords_CompletedState", "(\"Status\" = 'Completed' AND \"CompletedAt\" IS NOT NULL AND \"StatusCode\" IS NOT NULL) OR (\"Status\" <> 'Completed' AND \"CompletedAt\" IS NULL AND \"StatusCode\" IS NULL AND \"ProtectedOutcome\" IS NULL)");
                    table.CheckConstraint("CK_IdempotencyRecords_ExpiresAfterStart", "\"ExpiresAt\" > \"StartedAt\"");
                    table.CheckConstraint("CK_IdempotencyRecords_StatusCode", "\"StatusCode\" IS NULL OR (\"StatusCode\" >= 100 AND \"StatusCode\" <= 599)");
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryEnvelopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ProviderIdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryEnvelopes", x => x.Id);
                    table.CheckConstraint("CK_NotificationDeliveryEnvelopes_Source", "(\"NotificationId\" IS NULL) <> (\"InvitationId\" IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PlainTextBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ActionPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Culture = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DurableBackgroundJobs_CompletedAt_DeadLetteredAt_NextAttemp~",
                table: "DurableBackgroundJobs",
                columns: new[] { "CompletedAt", "DeadLetteredAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DurableBackgroundJobs_Global_Job_Key",
                table: "DurableBackgroundJobs",
                columns: new[] { "JobName", "IdempotencyKey" },
                unique: true,
                filter: "\"OrganizationId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_DurableBackgroundJobs_Tenant_Job_Key",
                table: "DurableBackgroundJobs",
                columns: new[] { "OrganizationId", "JobName", "IdempotencyKey" },
                unique: true,
                filter: "\"OrganizationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Status_ExpiresAt",
                table: "IdempotencyRecords",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Status_StartedAt",
                table: "IdempotencyRecords",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_Global_Scope_KeyHash",
                table: "IdempotencyRecords",
                columns: new[] { "Scope", "KeyHash" },
                unique: true,
                filter: "\"OrganizationId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_Tenant_Scope_KeyHash",
                table: "IdempotencyRecords",
                columns: new[] { "OrganizationId", "Scope", "KeyHash" },
                unique: true,
                filter: "\"OrganizationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryEnvelopes_DeliveredAt_DeadLetteredAt_Ne~",
                table: "NotificationDeliveryEnvelopes",
                columns: new[] { "DeliveredAt", "DeadLetteredAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryEnvelopes_InvitationId_Channel",
                table: "NotificationDeliveryEnvelopes",
                columns: new[] { "InvitationId", "Channel" },
                unique: true,
                filter: "\"InvitationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryEnvelopes_NotificationId_Channel",
                table: "NotificationDeliveryEnvelopes",
                columns: new[] { "NotificationId", "Channel" },
                unique: true,
                filter: "\"NotificationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeliveryStatus_CreatedAt",
                table: "Notifications",
                columns: new[] { "DeliveryStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId_IdempotencyKey",
                table: "Notifications",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId_RecipientUserId_CreatedAt_Id",
                table: "Notifications",
                columns: new[] { "OrganizationId", "RecipientUserId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId_RecipientUserId_ReadAt",
                table: "Notifications",
                columns: new[] { "OrganizationId", "RecipientUserId", "ReadAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DurableBackgroundJobs");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryEnvelopes");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
