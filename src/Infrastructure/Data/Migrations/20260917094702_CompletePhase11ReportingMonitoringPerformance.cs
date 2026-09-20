using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase11ReportingMonitoringPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_WalletId_Created_Id",
                table: "WalletEntries",
                columns: new[] { "WalletId", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequests_OrganizationId_AgentId_RequestedAt_Id",
                table: "PayoutRequests",
                columns: new[] { "OrganizationId", "AgentId", "RequestedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_AttributedAgentId_Created_Id",
                table: "Orders",
                columns: new[] { "OrganizationId", "AttributedAgentId", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_CustomerId_Created_Id",
                table: "Orders",
                columns: new[] { "OrganizationId", "CustomerId", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_PaymentStatus_Created_Id",
                table: "Orders",
                columns: new[] { "OrganizationId", "PaymentStatus", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_Cr~",
                table: "CommissionTransactions",
                columns: new[] { "OrganizationId", "BeneficiaryAgentId", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_EffectiveAt~",
                table: "BinaryVolumeEntries",
                columns: new[] { "OrganizationId", "OwnerAgentId", "EffectiveAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OrganizationId_SponsorAgentId_JoinedAt_Id",
                table: "Agents",
                columns: new[] { "OrganizationId", "SponsorAgentId", "JoinedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_WalletId_Created_Id",
                table: "WalletEntries");

            migrationBuilder.DropIndex(
                name: "IX_PayoutRequests_OrganizationId_AgentId_RequestedAt_Id",
                table: "PayoutRequests");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrganizationId_AttributedAgentId_Created_Id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrganizationId_CustomerId_Created_Id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrganizationId_PaymentStatus_Created_Id",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_BeneficiaryAgentId_Cr~",
                table: "CommissionTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_OrganizationId_OwnerAgentId_EffectiveAt~",
                table: "BinaryVolumeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Agents_OrganizationId_SponsorAgentId_JoinedAt_Id",
                table: "Agents");
        }
    }
}
