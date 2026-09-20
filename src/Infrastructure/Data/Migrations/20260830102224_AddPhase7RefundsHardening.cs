using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase7RefundsHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceOrderItemRefundId",
                table: "CommissionTransactions",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOrderItemRefundId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    EntityType = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    Reason = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    IpAddress = table.Column<string>(
                        type: "character varying(45)",
                        maxLength: 45,
                        nullable: true
                    ),
                    UserAgent = table.Column<string>(
                        type: "character varying(1024)",
                        maxLength: 1024,
                        nullable: true
                    ),
                    TraceId = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OrderItemRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentRefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(
                        type: "numeric(18,4)",
                        precision: 18,
                        scale: 4,
                        nullable: false
                    ),
                    RefundAmount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    CommissionableAmountToReverse = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    BusinessVolumeToReverse = table.Column<decimal>(
                        type: "numeric(18,4)",
                        precision: 18,
                        scale: 4,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CompletedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ReversalFailure = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                    Created = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemRefunds_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_OrderItemRefunds_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_OrderItemRefunds_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_OrderItemRefunds_PaymentRefunds_PaymentRefundId",
                        column: x => x.PaymentRefundId,
                        principalTable: "PaymentRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    MessageType = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ProcessedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    DeadLetteredAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ClaimedUntil = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ClaimedBy = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    LastError = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ProcessedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ProcessedMessages",
                        x => new { x.MessageId, x.ConsumerName }
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebhookProcessingAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    ProviderEventId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    EventType = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    ProviderResourceId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ResourceKind = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    PayloadHash = table.Column<string>(
                        type: "character varying(128)",
                        maxLength: 128,
                        nullable: false
                    ),
                    ReceivedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastAttemptAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    NextAttemptAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    LastError = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookProcessingAttempts", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_ReversalOfCommissionId_SourceOrderIt~",
                table: "CommissionTransactions",
                columns: new[] { "ReversalOfCommissionId", "SourceOrderItemRefundId" },
                unique: true,
                filter: "\"SourceOrderItemRefundId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BinaryVolumeEntries_ReversalOfEntryId_SourceOrderItemRefund~",
                table: "BinaryVolumeEntries",
                columns: new[] { "ReversalOfEntryId", "SourceOrderItemRefundId" },
                unique: true,
                filter: "\"SourceOrderItemRefundId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId_ActorUserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "OrganizationId", "ActorUserId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "OrganizationId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "OrganizationId", "EntityType", "EntityId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_OrderId",
                table: "OrderItemRefunds",
                column: "OrderId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_OrderItemId",
                table: "OrderItemRefunds",
                column: "OrderItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_OrganizationId_OrderId_RequestedAt",
                table: "OrderItemRefunds",
                columns: new[] { "OrganizationId", "OrderId", "RequestedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_OrganizationId_OrderItemId_Status",
                table: "OrderItemRefunds",
                columns: new[] { "OrganizationId", "OrderItemId", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_OrganizationId_Status_RequestedAt",
                table: "OrderItemRefunds",
                columns: new[] { "OrganizationId", "Status", "RequestedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemRefunds_PaymentRefundId",
                table: "OrderItemRefunds",
                column: "PaymentRefundId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OrganizationId_OccurredAt",
                table: "OutboxMessages",
                columns: new[] { "OrganizationId", "OccurredAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "NextAttemptAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedMessages_ProcessedAt",
                table: "ProcessedMessages",
                column: "ProcessedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebhookProcessingAttempts_OrganizationId_ReceivedAt",
                table: "WebhookProcessingAttempts",
                columns: new[] { "OrganizationId", "ReceivedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebhookProcessingAttempts_Provider_ProviderEventId",
                table: "WebhookProcessingAttempts",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebhookProcessingAttempts_Status_NextAttemptAt",
                table: "WebhookProcessingAttempts",
                columns: new[] { "Status", "NextAttemptAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");

            migrationBuilder.DropTable(name: "OrderItemRefunds");

            migrationBuilder.DropTable(name: "OutboxMessages");

            migrationBuilder.DropTable(name: "ProcessedMessages");

            migrationBuilder.DropTable(name: "WebhookProcessingAttempts");

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_ReversalOfCommissionId_SourceOrderIt~",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_ReversalOfEntryId_SourceOrderItemRefund~",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.DropColumn(
                name: "SourceOrderItemRefundId",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropColumn(
                name: "SourceOrderItemRefundId",
                table: "BinaryVolumeEntries"
            );
        }
    }
}
