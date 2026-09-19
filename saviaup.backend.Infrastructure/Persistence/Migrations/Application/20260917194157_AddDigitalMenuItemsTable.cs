using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddDigitalMenuItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_menu_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_menu_items", x => x.Id);
                    table.CheckConstraint("CK_digital_menu_items_ItemType", "\"ItemType\" IN ('CATEGORY', 'PRODUCT')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_digital_menu_items_TenantId_CategoryId",
                table: "digital_menu_items",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_menu_items_TenantId_ItemType_SortOrder",
                table: "digital_menu_items",
                columns: new[] { "TenantId", "ItemType", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_digital_menu_items_TenantId_TargetId",
                table: "digital_menu_items",
                columns: new[] { "TenantId", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digital_menu_items");
        }
    }
}
