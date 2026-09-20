using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationNetworkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Network_AllowAgentPreferredLeg",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "Network_AutoPlacementEnabled",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Network_DefaultPlacementStrategy",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Network_MaxQueryDepth",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 10
            );

            migrationBuilder.AddColumn<bool>(
                name: "Network_RestrictPlacementChangesAfterActivation",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Network_AllowAgentPreferredLeg",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Network_AutoPlacementEnabled",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(
                name: "Network_DefaultPlacementStrategy",
                table: "Organizations"
            );

            migrationBuilder.DropColumn(name: "Network_MaxQueryDepth", table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Network_RestrictPlacementChangesAfterActivation",
                table: "Organizations"
            );
        }
    }
}
