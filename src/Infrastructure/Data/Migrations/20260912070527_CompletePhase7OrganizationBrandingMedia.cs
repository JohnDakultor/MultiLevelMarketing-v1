using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase7OrganizationBrandingMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZone",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BrandingPublishedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "BrandingRevision",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<bool>(
                name: "Commerce_AllowGuestCheckout",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Commerce_InventoryReservationMinutes",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 30
            );

            migrationBuilder.AddColumn<bool>(
                name: "Commerce_RequireBillingAddress",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "Commerce_RequireShippingAddress",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "PublishedBrandingRevision",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "OrganizationDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostName = table.Column<string>(
                        type: "character varying(253)",
                        maxLength: 253,
                        nullable: false
                    ),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    VerifiedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
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
                    table.PrimaryKey("PK_OrganizationDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationDomains_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDomains_HostName",
                table: "OrganizationDomains",
                column: "HostName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDomains_OrganizationId",
                table: "OrganizationDomains",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OrganizationDomains");

            migrationBuilder.DropColumn(name: "BrandingPublishedAt", table: "Organizations");

            migrationBuilder.DropColumn(name: "BrandingRevision", table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Commerce_AllowGuestCheckout",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Commerce_InventoryReservationMinutes",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Commerce_RequireBillingAddress",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Commerce_RequireShippingAddress",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(name: "PublishedBrandingRevision", table: "Organizations");

            migrationBuilder.AlterColumn<string>(
                name: "TimeZone",
                table: "Organizations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );

            migrationBuilder.AlterColumn<string>(
                name: "Locale",
                table: "Organizations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20
            );
        }
    }
}
