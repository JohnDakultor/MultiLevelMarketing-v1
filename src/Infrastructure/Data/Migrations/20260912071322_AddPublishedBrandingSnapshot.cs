using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedBrandingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_AccentColor",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_FaviconUrl",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_FooterText",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_LogoUrl",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_PrimaryColor",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_SecondaryColor",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_StoreTitle",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_SupportEmail",
                table: "Organizations",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PublishedBranding_SupportPhone",
                table: "Organizations",
                type: "text",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedBranding_AccentColor",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_FaviconUrl",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_FooterText",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(name: "PublishedBranding_LogoUrl", table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PublishedBranding_PrimaryColor",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_SecondaryColor",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_StoreTitle",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_SupportEmail",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "PublishedBranding_SupportPhone",
                table: "Organizations"
            );
        }
    }
}
