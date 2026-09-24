using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakeProductBasePriceOptionalForVariations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_products_SalePrice_Positive",
                table: "products");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.Sql(
                """
                UPDATE "products" AS product
                SET "SalePrice" = NULL
                WHERE EXISTS (
                    SELECT 1
                    FROM "product_variations" AS variation
                    WHERE variation."ProductId" = product."Id"
                );
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_SalePrice_Positive",
                table: "products",
                sql: "\"SalePrice\" IS NULL OR \"SalePrice\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_products_SalePrice_Positive",
                table: "products");

            migrationBuilder.Sql(
                """
                UPDATE "products" AS product
                SET "SalePrice" = COALESCE((
                    SELECT MIN(variation."SalePrice")
                    FROM "product_variations" AS variation
                    WHERE variation."ProductId" = product."Id"
                ), 0.01)
                WHERE product."SalePrice" IS NULL;
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_SalePrice_Positive",
                table: "products",
                sql: "\"SalePrice\" > 0");
        }
    }
}
