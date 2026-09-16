using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddProductRecipeItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_recipe_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomIngredientName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_recipe_items", x => x.Id);
                    table.CheckConstraint("CK_product_recipe_items_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_product_recipe_items_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_product_recipe_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_recipe_items_IngredientId",
                table: "product_recipe_items",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_product_recipe_items_ProductId",
                table: "product_recipe_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_recipe_items_TenantId_IngredientId",
                table: "product_recipe_items",
                columns: new[] { "TenantId", "IngredientId" });

            migrationBuilder.CreateIndex(
                name: "IX_product_recipe_items_TenantId_ProductId",
                table: "product_recipe_items",
                columns: new[] { "TenantId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_recipe_items");
        }
    }
}
