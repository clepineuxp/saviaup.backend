using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddComboProductVariations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductId",
                table: "product_combo_options");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariationId",
                table: "product_combo_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductId",
                table: "product_combo_options",
                columns: new[] { "ComboGroupId", "ProductId" },
                unique: true,
                filter: "\"ProductVariationId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductVariationId",
                table: "product_combo_options",
                columns: new[] { "ComboGroupId", "ProductVariationId" },
                unique: true,
                filter: "\"ProductVariationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ProductVariationId",
                table: "product_combo_options",
                column: "ProductVariationId");

            migrationBuilder.AddForeignKey(
                name: "FK_product_combo_options_product_variations_ProductVariationId",
                table: "product_combo_options",
                column: "ProductVariationId",
                principalTable: "product_variations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_combo_options_product_variations_ProductVariationId",
                table: "product_combo_options");

            migrationBuilder.DropIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductId",
                table: "product_combo_options");

            migrationBuilder.DropIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductVariationId",
                table: "product_combo_options");

            migrationBuilder.DropIndex(
                name: "IX_product_combo_options_ProductVariationId",
                table: "product_combo_options");

            migrationBuilder.DropColumn(
                name: "ProductVariationId",
                table: "product_combo_options");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_options_ComboGroupId_ProductId",
                table: "product_combo_options",
                columns: new[] { "ComboGroupId", "ProductId" },
                unique: true);
        }
    }
}
