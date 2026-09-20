using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4Compensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions"
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "DirectSalesRateOverride",
                table: "OrderItems",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AgentWallets",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions",
                columns: new[]
                {
                    "OrganizationId",
                    "BeneficiaryAgentId",
                    "SourceOrderId",
                    "SourceOrderItemId",
                    "RuleId",
                },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_SourceOrder~",
                table: "BinaryVolumeEntries",
                columns: new[]
                {
                    "OrganizationId",
                    "OwnerAgentId",
                    "SourceOrderItemId",
                    "EntryType",
                },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AgentWallets_OrganizationId_AgentId",
                table: "AgentWallets",
                columns: new[] { "OrganizationId", "AgentId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_SourceOrder~",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_AgentWallets_OrganizationId_AgentId",
                table: "AgentWallets"
            );

            migrationBuilder.DropColumn(name: "PaidAt", table: "Orders");

            migrationBuilder.DropColumn(name: "DirectSalesRateOverride", table: "OrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AgentWallets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3
            );

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions",
                columns: new[]
                {
                    "OrganizationId",
                    "BeneficiaryAgentId",
                    "SourceOrderId",
                    "RuleId",
                },
                unique: true
            );
        }
    }
}
