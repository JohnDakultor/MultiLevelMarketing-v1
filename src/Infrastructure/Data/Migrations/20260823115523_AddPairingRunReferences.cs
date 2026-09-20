using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPairingRunReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_PairingRunId",
                table: "CommissionTransactions",
                column: "PairingRunId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BinaryVolumeEntries_PairingRunId",
                table: "BinaryVolumeEntries",
                column: "PairingRunId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_BinaryVolumeEntries_BinaryPairingRuns_PairingRunId",
                table: "BinaryVolumeEntries",
                column: "PairingRunId",
                principalTable: "BinaryPairingRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionTransactions_BinaryPairingRuns_PairingRunId",
                table: "CommissionTransactions",
                column: "PairingRunId",
                principalTable: "BinaryPairingRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BinaryVolumeEntries_BinaryPairingRuns_PairingRunId",
                table: "BinaryVolumeEntries"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionTransactions_BinaryPairingRuns_PairingRunId",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_CommissionTransactions_PairingRunId",
                table: "CommissionTransactions"
            );

            migrationBuilder.DropIndex(
                name: "IX_BinaryVolumeEntries_PairingRunId",
                table: "BinaryVolumeEntries"
            );
        }
    }
}
