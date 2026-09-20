using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitializeOrganizationNetworkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Organizations"
                SET "Network_AllowAgentPreferredLeg" = TRUE,
                    "Network_AutoPlacementEnabled" = TRUE,
                    "Network_MaxQueryDepth" = 10,
                    "Network_RestrictPlacementChangesAfterActivation" = TRUE
                WHERE "Network_MaxQueryDepth" = 0;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data initialization is intentionally not reversed.
        }
    }
}
