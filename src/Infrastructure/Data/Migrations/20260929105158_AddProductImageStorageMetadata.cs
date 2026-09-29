using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageStorageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultImageObjectKey",
                table: "Products",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultImageObjectKey",
                table: "Products");

        }
    }
}
