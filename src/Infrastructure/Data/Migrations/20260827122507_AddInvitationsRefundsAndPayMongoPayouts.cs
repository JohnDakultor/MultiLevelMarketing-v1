using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationsRefundsAndPayMongoPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderReference",
                table: "PayoutRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PayoutRequests",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PayoutRequests",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric"
            );

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "PayoutRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "FailureMessage",
                table: "PayoutRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ProviderBatchId",
                table: "PayoutRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransferId",
                table: "PayoutRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "PayoutAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "MaskedAccountData",
                table: "PayoutAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "PayoutAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ProtectedAccountName",
                table: "PayoutAccounts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ProtectedAccountNumber",
                table: "PayoutAccounts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Rail",
                table: "PayoutAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m
            );

            migrationBuilder.CreateTable(
                name: "AdministratorInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    NormalizedEmail = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    TokenHash = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    RevocationReason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
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
                    table.PrimaryKey("PK_AdministratorInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdministratorInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    Currency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    Reason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    IdempotencyKey = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    ProviderRefundId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
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
                    FailureMessage = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
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
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequests_OrganizationId_Status_RequestedAt",
                table: "PayoutRequests",
                columns: new[] { "OrganizationId", "Status", "RequestedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequests_PayoutAccountId",
                table: "PayoutRequests",
                column: "PayoutAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequests_ProviderTransferId",
                table: "PayoutRequests",
                column: "ProviderTransferId",
                unique: true,
                filter: "\"ProviderTransferId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAccounts_AgentId",
                table: "PayoutAccounts",
                column: "AgentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PayoutAccounts_OrganizationId_AgentId",
                table: "PayoutAccounts",
                columns: new[] { "OrganizationId", "AgentId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AdministratorInvitations_OrganizationId_NormalizedEmail",
                table: "AdministratorInvitations",
                columns: new[] { "OrganizationId", "NormalizedEmail" },
                unique: true,
                filter: "\"Status\" = 0"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AdministratorInvitations_OrganizationId_Status_ExpiresAt",
                table: "AdministratorInvitations",
                columns: new[] { "OrganizationId", "Status", "ExpiresAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AdministratorInvitations_TokenHash",
                table: "AdministratorInvitations",
                column: "TokenHash",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_IdempotencyKey",
                table: "PaymentRefunds",
                column: "IdempotencyKey",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                table: "PaymentRefunds",
                column: "PaymentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_ProviderRefundId",
                table: "PaymentRefunds",
                column: "ProviderRefundId",
                unique: true,
                filter: "\"ProviderRefundId\" IS NOT NULL"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PayoutAccounts_Agents_AgentId",
                table: "PayoutAccounts",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_PayoutRequests_PayoutAccounts_PayoutAccountId",
                table: "PayoutRequests",
                column: "PayoutAccountId",
                principalTable: "PayoutAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayoutAccounts_Agents_AgentId",
                table: "PayoutAccounts"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_PayoutRequests_PayoutAccounts_PayoutAccountId",
                table: "PayoutRequests"
            );

            migrationBuilder.DropTable(name: "AdministratorInvitations");

            migrationBuilder.DropTable(name: "PaymentRefunds");

            migrationBuilder.DropIndex(
                name: "IX_PayoutRequests_OrganizationId_Status_RequestedAt",
                table: "PayoutRequests"
            );

            migrationBuilder.DropIndex(
                name: "IX_PayoutRequests_PayoutAccountId",
                table: "PayoutRequests"
            );

            migrationBuilder.DropIndex(
                name: "IX_PayoutRequests_ProviderTransferId",
                table: "PayoutRequests"
            );

            migrationBuilder.DropIndex(name: "IX_PayoutAccounts_AgentId", table: "PayoutAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PayoutAccounts_OrganizationId_AgentId",
                table: "PayoutAccounts"
            );

            migrationBuilder.DropColumn(name: "FailureCode", table: "PayoutRequests");

            migrationBuilder.DropColumn(name: "FailureMessage", table: "PayoutRequests");

            migrationBuilder.DropColumn(name: "ProviderBatchId", table: "PayoutRequests");

            migrationBuilder.DropColumn(name: "ProviderTransferId", table: "PayoutRequests");

            migrationBuilder.DropColumn(name: "BankCode", table: "PayoutAccounts");

            migrationBuilder.DropColumn(name: "ProtectedAccountName", table: "PayoutAccounts");

            migrationBuilder.DropColumn(name: "ProtectedAccountNumber", table: "PayoutAccounts");

            migrationBuilder.DropColumn(name: "Rail", table: "PayoutAccounts");

            migrationBuilder.DropColumn(name: "RefundedAmount", table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderReference",
                table: "PayoutRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PayoutRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3
            );

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PayoutRequests",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2
            );

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "PayoutAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50
            );

            migrationBuilder.AlterColumn<string>(
                name: "MaskedAccountData",
                table: "PayoutAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );
        }
    }
}
