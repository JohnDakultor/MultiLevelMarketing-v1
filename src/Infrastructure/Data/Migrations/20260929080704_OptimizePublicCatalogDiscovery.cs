using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePublicCatalogDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_OrganizationId_CategoryId_Status",
                table: "Products",
                columns: new[] { "OrganizationId", "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_OrganizationId_Status_Created",
                table: "Products",
                columns: new[] { "OrganizationId", "Status", "Created" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_OrganizationId_CategoryId_Status",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_OrganizationId_Status_Created",
                table: "Products");
        }
    }
}
