using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenWalletRefundReversalIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_WalletId_SourceType_SourceId_Type",
                table: "WalletEntries"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_ReversalOfEntryId_SourceId_Type",
                table: "WalletEntries",
                columns: new[] { "ReversalOfEntryId", "SourceId", "Type" },
                unique: true,
                filter: "\"ReversalOfEntryId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_WalletId_SourceType_SourceId_Type",
                table: "WalletEntries",
                columns: new[] { "WalletId", "SourceType", "SourceId", "Type" },
                unique: true,
                filter: "\"ReversalOfEntryId\" IS NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_ReversalOfEntryId_SourceId_Type",
                table: "WalletEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_WalletId_SourceType_SourceId_Type",
                table: "WalletEntries"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_WalletId_SourceType_SourceId_Type",
                table: "WalletEntries",
                columns: new[] { "WalletId", "SourceType", "SourceId", "Type" },
                unique: true
            );
        }
    }
}
