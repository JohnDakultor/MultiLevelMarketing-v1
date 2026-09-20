using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase8AgentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredLeg",
                table: "Agents",
                type: "integer",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OrganizationId_AgentCode",
                table: "Agents",
                columns: new[] { "OrganizationId", "AgentCode" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OrganizationId_Status_JoinedAt",
                table: "Agents",
                columns: new[] { "OrganizationId", "Status", "JoinedAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Agents_OrganizationId_AgentCode", table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_OrganizationId_Status_JoinedAt",
                table: "Agents"
            );

            migrationBuilder.DropColumn(name: "PreferredLeg", table: "Agents");
        }
    }
}
