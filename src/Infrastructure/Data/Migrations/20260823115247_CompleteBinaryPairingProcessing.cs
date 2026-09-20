using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteBinaryPairingProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_SourceOrder~",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceOrderId",
                table: "CommissionTransactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "PairingRunId",
                table: "CommissionTransactions",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceOrderItemId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid"
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceAgentId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "PairingRunId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_Pa~",
                table: "CommissionTransactions",
                columns: new[] { "OrganizationId", "BeneficiaryAgentId", "PairingRunId", "RuleId" },
                unique: true,
                filter: "\"PairingRunId\" IS NOT NULL"
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
                unique: true,
                filter: "\"SourceOrderId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_PairingRunI~",
                table: "BinaryVolumeEntries",
                columns: new[]
                {
                    "OrganizationId",
                    "OwnerAgentId",
                    "PairingRunId",
                    "Side",
                    "EntryType",
                },
                unique: true,
                filter: "\"PairingRunId\" IS NOT NULL"
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
                unique: true,
                filter: "\"SourceOrderItemId\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_Pa~",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_So~",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_PairingRunI~",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_SourceOrder~",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.DropColumn(name: "PairingRunId", table: "CommissionTransactions");

            migrationBuilder.DropColumn(name: "PairingRunId", table: "BinaryVolumeEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceOrderId",
                table: "CommissionTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceOrderItemId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceAgentId",
                table: "BinaryVolumeEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true
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
        }
    }
}
