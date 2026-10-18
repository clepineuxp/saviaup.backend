using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalSubtotalAmount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "order_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalSubtotalAmount",
                table: "order_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariationId",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionName",
                table: "order_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiredQuantity = table.Column<int>(type: "integer", nullable: true),
                    DiscountedQuantity = table.Column<int>(type: "integer", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SpecificDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MonthOfYear = table.Column<int>(type: "integer", nullable: true),
                    WeekDaysMask = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.Id);
                    table.CheckConstraint("CK_promotions_Value_Positive", "\"Value\" > 0");
                    table.CheckConstraint("CK_promotions_WeekDaysMask_Valid", "\"WeekDaysMask\" >= 0 AND \"WeekDaysMask\" <= 127");
                });

            migrationBuilder.CreateTable(
                name: "promotion_targets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_targets_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_targets_product_variations_ProductVariationId",
                        column: x => x.ProductVariationId,
                        principalTable: "product_variations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_targets_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_targets_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductVariationId",
                table: "order_items",
                column: "ProductVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_PromotionId",
                table: "order_items",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_CategoryId",
                table: "promotion_targets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_ProductId",
                table: "promotion_targets",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_ProductVariationId",
                table: "promotion_targets",
                column: "ProductVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_PromotionId",
                table: "promotion_targets",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_TenantId_CategoryId",
                table: "promotion_targets",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_TenantId_ProductId",
                table: "promotion_targets",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_TenantId_ProductVariationId",
                table: "promotion_targets",
                columns: new[] { "TenantId", "ProductVariationId" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_TenantId_PromotionId",
                table: "promotion_targets",
                columns: new[] { "TenantId", "PromotionId" });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_TenantId_IsActive",
                table: "promotions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_promotions_TenantId_NormalizedName",
                table: "promotions",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_product_variations_ProductVariationId",
                table: "order_items",
                column: "ProductVariationId",
                principalTable: "product_variations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_promotions_PromotionId",
                table: "order_items",
                column: "PromotionId",
                principalTable: "promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_product_variations_ProductVariationId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_promotions_PromotionId",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "promotion_targets");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropIndex(
                name: "IX_order_items_ProductVariationId",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_PromotionId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OriginalSubtotalAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "order_receipts");

            migrationBuilder.DropColumn(
                name: "OriginalSubtotalAmount",
                table: "order_receipts");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "ProductVariationId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "PromotionName",
                table: "order_items");
        }
    }
}
