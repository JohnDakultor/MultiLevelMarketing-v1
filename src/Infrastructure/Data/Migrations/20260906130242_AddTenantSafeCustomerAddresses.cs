using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSafeCustomerAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CustomerProfiles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "CustomerProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CustomerProfiles_Id_OrganizationId",
                table: "CustomerProfiles",
                columns: new[] { "Id", "OrganizationId" }
            );

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    RecipientName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    PhoneNumber = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
                    AddressLine1 = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    AddressLine2 = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: true
                    ),
                    Barangay = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    CityOrMunicipality = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Province = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    PostalCode = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CountryCode = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
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
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_CustomerProfiles_CustomerProfileId_Organi~",
                        columns: x => new { x.CustomerProfileId, x.OrganizationId },
                        principalTable: "CustomerProfiles",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_OrganizationId_UserId",
                table: "CustomerProfiles",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerProfileId_OrganizationId",
                table: "CustomerAddresses",
                columns: new[] { "CustomerProfileId", "OrganizationId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_Org_Profile_Active",
                table: "CustomerAddresses",
                columns: new[] { "OrganizationId", "CustomerProfileId", "IsActive" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CustomerAddresses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CustomerProfiles_Id_OrganizationId",
                table: "CustomerProfiles"
            );

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_OrganizationId_UserId",
                table: "CustomerProfiles"
            );

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CustomerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450
            );

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "CustomerProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200
            );
        }
    }
}
