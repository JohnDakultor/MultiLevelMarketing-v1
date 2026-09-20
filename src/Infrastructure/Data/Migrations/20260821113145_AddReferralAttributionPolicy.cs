using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralAttributionPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Referrals_AllowReferralOverride",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "Referrals_AttributionWindowDays",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 30
            );

            migrationBuilder.AddColumn<bool>(
                name: "Referrals_ReferralLockAfterFirstPurchase",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.CreateTable(
                name: "ReferralAttributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCode = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    CapturedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Source = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ReferralAttributions", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReferralAttributions_OrganizationId_AgentId_CapturedAt",
                table: "ReferralAttributions",
                columns: new[] { "OrganizationId", "AgentId", "CapturedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReferralAttributions_OrganizationId_CustomerId",
                table: "ReferralAttributions",
                columns: new[] { "OrganizationId", "CustomerId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReferralAttributions");

            migrationBuilder.DropColumn(
                name: "Referrals_AllowReferralOverride",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Referrals_AttributionWindowDays",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Referrals_ReferralLockAfterFirstPurchase",
                table: "Organizations"
            );
        }
    }
}
