using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReleasedFromEntryId",
                table: "WalletEntries",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "WalletSettings",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionReleaseTrigger = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    ReleaseDelayDays = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    ReturnWindowDays = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    MinimumPayoutAmount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false,
                        defaultValue: 10.00m
                    ),
                    AllowNegativeRecoverableBalance = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    MaximumNegativeBalance = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false,
                        defaultValue: 0m
                    ),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletSettings", x => x.OrganizationId);
                    table.CheckConstraint(
                        "CK_WalletSettings_CommissionReleaseTrigger_Valid",
                        "\"CommissionReleaseTrigger\" IN (0, 1, 2)"
                    );
                    table.CheckConstraint(
                        "CK_WalletSettings_MaximumNegativeBalance_NonNegative",
                        "\"MaximumNegativeBalance\" >= 0"
                    );
                    table.CheckConstraint(
                        "CK_WalletSettings_MinimumPayoutAmount_Positive",
                        "\"MinimumPayoutAmount\" > 0"
                    );
                    table.CheckConstraint(
                        "CK_WalletSettings_ReleaseDelayDays_NonNegative",
                        "\"ReleaseDelayDays\" >= 0"
                    );
                    table.CheckConstraint(
                        "CK_WalletSettings_ReturnWindowDays_NonNegative",
                        "\"ReturnWindowDays\" >= 0"
                    );
                    table.ForeignKey(
                        name: "FK_WalletSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.Sql(
                """
                INSERT INTO "WalletSettings" (
                    "OrganizationId",
                    "Id",
                    "Created",
                    "LastModified"
                )
                SELECT
                    "Id",
                    "Id",
                    NOW(),
                    NOW()
                FROM "Organizations";
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_ReleasedFromEntryId",
                table: "WalletEntries",
                column: "ReleasedFromEntryId",
                unique: true,
                filter: "\"ReleasedFromEntryId\" IS NOT NULL"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletEntries_Release_DoesNotReferenceSelf",
                table: "WalletEntries",
                sql: "\"ReleasedFromEntryId\" IS NULL OR \"ReleasedFromEntryId\" <> \"Id\""
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletEntries_Release_IsAvailableCredit",
                table: "WalletEntries",
                sql: "\"ReleasedFromEntryId\" IS NULL OR \"Type\" = 1"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_WalletEntries_WalletEntries_ReleasedFromEntryId",
                table: "WalletEntries",
                column: "ReleasedFromEntryId",
                principalTable: "WalletEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletEntries_WalletEntries_ReleasedFromEntryId",
                table: "WalletEntries"
            );

            migrationBuilder.DropTable(name: "WalletSettings");

            migrationBuilder.DropIndex(
                name: "IX_WalletEntries_ReleasedFromEntryId",
                table: "WalletEntries"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletEntries_Release_DoesNotReferenceSelf",
                table: "WalletEntries"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletEntries_Release_IsAvailableCredit",
                table: "WalletEntries"
            );

            migrationBuilder.DropColumn(name: "ReleasedFromEntryId", table: "WalletEntries");
        }
    }
}
