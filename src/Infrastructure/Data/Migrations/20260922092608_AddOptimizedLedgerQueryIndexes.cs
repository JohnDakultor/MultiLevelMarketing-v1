using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizedLedgerQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_Wallet_Type",
                table: "WalletEntries",
                columns: new[] { "WalletId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_Org_Created_Id",
                table: "CommissionTransactions",
                columns: new[] { "OrganizationId", "Created", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_Org_Type_Created_Id",
                table: "CommissionTransactions",
                columns: new[] { "OrganizationId", "Type", "Created", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_Wallet_Type",
                table: "WalletEntries");

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_Org_Created_Id",
                table: "CommissionTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_Org_Type_Created_Id",
                table: "CommissionTransactions");
        }
    }
}
