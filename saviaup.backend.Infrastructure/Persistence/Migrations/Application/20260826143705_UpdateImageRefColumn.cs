using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class UpdateImageRefColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "categories");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageRef",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageRef",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_ImageRef",
                table: "products",
                column: "ImageRef");

            migrationBuilder.CreateIndex(
                name: "IX_categories_ImageRef",
                table: "categories",
                column: "ImageRef");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_stored_images_ImageRef",
                table: "categories",
                column: "ImageRef",
                principalTable: "stored_images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_products_stored_images_ImageRef",
                table: "products",
                column: "ImageRef",
                principalTable: "stored_images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_stored_images_ImageRef",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_products_stored_images_ImageRef",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ImageRef",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_ImageRef",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "ImageRef",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageRef",
                table: "categories");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "categories",
                type: "text",
                nullable: true);
        }
    }
}
