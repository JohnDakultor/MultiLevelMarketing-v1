using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteReleaseReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "OrganizationDomains",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "OrganizationDomains"
                SET "VerificationToken" = lower(md5(random()::text || clock_timestamp()::text || "Id"::text))
                WHERE "VerificationToken" = '';
                """
            );

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CustomerProfiles",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CustomerProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.Sql(
                """
                UPDATE "CustomerProfiles" AS customer
                SET "Email" = lower(identity_user."Email")
                FROM "AspNetUsers" AS identity_user
                WHERE customer."UserId" = identity_user."Id"
                  AND customer."Email" = ''
                  AND identity_user."Email" IS NOT NULL;
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_OrganizationId_Email",
                table: "CustomerProfiles",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_OrganizationId_Status",
                table: "CustomerProfiles",
                columns: new[] { "OrganizationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_OrganizationId_Email",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_OrganizationId_Status",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "OrganizationDomains");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CustomerProfiles");
        }
    }
}
