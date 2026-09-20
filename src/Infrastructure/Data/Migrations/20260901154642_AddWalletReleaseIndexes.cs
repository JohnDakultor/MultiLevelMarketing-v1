using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletReleaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_SourceId_Type",
                table: "WalletEntries",
                columns: new[] { "SourceId", "Type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_OrganizationId_Status_Created_Id",
                table: "CommissionTransactions",
                columns: new[] { "OrganizationId", "Status", "Created", "Id" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_SourceId_Type",
                table: "WalletEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_OrganizationId_Status_Created_Id",
                table: "CommissionTransactions"
            );
        }
    }
}
