using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCartOwnershipConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CartItems_CartId", table: "CartItems");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "Carts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "ReferralCode",
                table: "Carts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AttributionSource",
                table: "Carts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Carts_OrganizationId_CustomerId",
                table: "Carts",
                columns: new[] { "OrganizationId", "CustomerId" },
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Carts_OrganizationId_SessionId",
                table: "Carts",
                columns: new[] { "OrganizationId", "SessionId" },
                unique: true,
                filter: "\"SessionId\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductVariantId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductVariantId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Carts_OrganizationId_CustomerId", table: "Carts");

            migrationBuilder.DropIndex(name: "IX_Carts_OrganizationId_SessionId", table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductVariantId",
                table: "CartItems"
            );

            migrationBuilder.DropColumn(name: "Status", table: "ProductVariants");

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                table: "Carts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "ReferralCode",
                table: "Carts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AttributionSource",
                table: "Carts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId"
            );
        }
    }
}
