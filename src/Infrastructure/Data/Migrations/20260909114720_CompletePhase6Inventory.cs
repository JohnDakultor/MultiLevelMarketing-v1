using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace modular_mlm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase6Inventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "InventoryAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                    AdjustmentType = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false
                    ),
                    Reason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    IdempotencyKey = table.Column<string>(
                        type: "character varying(128)",
                        maxLength: 128,
                        nullable: false
                    ),
                    ExpectedVersion = table.Column<int>(type: "integer", nullable: false),
                    BalanceBefore = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_InventoryAdjustments", x => x.Id);
                    table.CheckConstraint(
                        "CK_InventoryAdjustments_BalanceEquation",
                        "\"BalanceAfter\" = \"BalanceBefore\" + \"QuantityDelta\""
                    );
                    table.CheckConstraint(
                        "CK_InventoryAdjustments_Balances_NonNegative",
                        "\"BalanceBefore\" >= 0 AND \"BalanceAfter\" >= 0"
                    );
                    table.CheckConstraint(
                        "CK_InventoryAdjustments_QuantityDelta_NonZero",
                        "\"QuantityDelta\" <> 0"
                    );
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(
                        type: "character varying(24)",
                        maxLength: 24,
                        nullable: false
                    ),
                    ReservedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    FinalizedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ReleasedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    ReleaseReason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    Version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.CheckConstraint("CK_InventoryReservations_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryReservations_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity",
                table: "ProductVariants",
                sql: "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"StockQuantity\""
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_StockQuantity_NonNegative",
                table: "ProductVariants",
                sql: "\"StockQuantity\" >= 0"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_Org_Variant_Occurred",
                table: "InventoryAdjustments",
                columns: new[] { "OrganizationId", "ProductVariantId", "OccurredAt", "Id" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_ProductVariantId",
                table: "InventoryAdjustments",
                column: "ProductVariantId"
            );

            migrationBuilder.CreateIndex(
                name: "UX_InventoryAdjustments_Org_IdempotencyKey",
                table: "InventoryAdjustments",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations",
                column: "OrderId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrganizationId_OrderId_ProductVariant~",
                table: "InventoryReservations",
                columns: new[] { "OrganizationId", "OrderId", "ProductVariantId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrganizationId_Status_ExpiresAt",
                table: "InventoryReservations",
                columns: new[] { "OrganizationId", "Status", "ExpiresAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProductVariantId",
                table: "InventoryReservations",
                column: "ProductVariantId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InventoryAdjustments");

            migrationBuilder.DropTable(name: "InventoryReservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_ReservedQuantity",
                table: "ProductVariants"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_StockQuantity_NonNegative",
                table: "ProductVariants"
            );

            migrationBuilder.DropColumn(name: "ReservedQuantity", table: "ProductVariants");

            migrationBuilder.DropColumn(name: "Version", table: "ProductVariants");
        }
    }
}
