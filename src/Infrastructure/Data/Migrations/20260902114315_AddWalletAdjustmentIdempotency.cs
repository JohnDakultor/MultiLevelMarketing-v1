using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAdjustmentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WalletEntries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_WalletId_IdempotencyKey",
                table: "WalletEntries",
                columns: new[] { "WalletId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletEntries_Adjustment_HasIdempotencyKey",
                table: "WalletEntries",
                sql: "\"Type\" <> 5 OR \"IdempotencyKey\" IS NOT NULL"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletEntries_IdempotencyKey_NotBlank",
                table: "WalletEntries",
                sql: "\"IdempotencyKey\" IS NULL OR length(btrim(\"IdempotencyKey\")) > 0"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletEntries_NonAdjustment_HasNoIdempotencyKey",
                table: "WalletEntries",
                sql: "\"Type\" = 5 OR \"IdempotencyKey\" IS NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_WalletId_IdempotencyKey",
                table: "WalletEntries"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletEntries_Adjustment_HasIdempotencyKey",
                table: "WalletEntries"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletEntries_IdempotencyKey_NotBlank",
                table: "WalletEntries"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletEntries_NonAdjustment_HasNoIdempotencyKey",
                table: "WalletEntries"
            );

            migrationBuilder.DropColumn(name: "IdempotencyKey", table: "WalletEntries");
        }
    }
}
