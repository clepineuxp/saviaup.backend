using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddProductComboComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "order_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "product_combo_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComboProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SelectionType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false),
                    MaxSelections = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_combo_groups", x => x.Id);
                    table.CheckConstraint("CK_product_combo_groups_Limits", "\"MinSelections\" >= 0 AND \"MaxSelections\" >= 1 AND \"MinSelections\" <= \"MaxSelections\"");
                    table.CheckConstraint("CK_product_combo_groups_SelectionType", "\"SelectionType\" IN ('SINGLE', 'MULTIPLE', 'FIXED')");
                    table.ForeignKey(
                        name: "FK_product_combo_groups_products_ComboProductId",
                        column: x => x.ComboProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_combo_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComboGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductQuantity = table.Column<int>(type: "integer", nullable: false),
                    PriceAdjustment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_combo_options", x => x.Id);
                    table.CheckConstraint("CK_product_combo_options_ProductQuantity", "\"ProductQuantity\" >= 1");
                    table.ForeignKey(
                        name: "FK_product_combo_options_product_combo_groups_ComboGroupId",
                        column: x => x.ComboGroupId,
                        principalTable: "product_combo_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_combo_options_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_combo_selections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComboGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComboOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProductQuantity = table.Column<int>(type: "integer", nullable: false),
                    SelectionQuantity = table.Column<int>(type: "integer", nullable: false),
                    PriceAdjustment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_combo_selections", x => x.Id);
                    table.CheckConstraint("CK_order_item_combo_selections_Quantities", "\"ProductQuantity\" >= 1 AND \"SelectionQuantity\" >= 1");
                    table.ForeignKey(
                        name: "FK_order_item_combo_selections_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_item_combo_selections_product_combo_groups_ComboGroup~",
                        column: x => x.ComboGroupId,
                        principalTable: "product_combo_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_item_combo_selections_product_combo_options_ComboOpti~",
                        column: x => x.ComboOptionId,
                        principalTable: "product_combo_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_item_combo_selections_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_combo_selections_ComboGroupId",
                table: "order_item_combo_selections",
                column: "ComboGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_combo_selections_ComboOptionId",
                table: "order_item_combo_selections",
                column: "ComboOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_combo_selections_OrderItemId",
                table: "order_item_combo_selections",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_combo_selections_ProductId",
                table: "order_item_combo_selections",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_combo_selections_TenantId_OrderItemId",
                table: "order_item_combo_selections",
                columns: new[] { "TenantId", "OrderItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_groups_ComboProductId",
                table: "product_combo_groups",
                column: "ComboProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_groups_TenantId_ComboProductId_Order",
                table: "product_combo_groups",
                columns: new[] { "TenantId", "ComboProductId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductId",
                table: "product_combo_options",
                columns: new[] { "ComboGroupId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ProductId",
                table: "product_combo_options",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_TenantId_ComboGroupId_Order",
                table: "product_combo_options",
                columns: new[] { "TenantId", "ComboGroupId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_combo_selections");

            migrationBuilder.DropTable(
                name: "product_combo_options");

            migrationBuilder.DropTable(
                name: "product_combo_groups");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
