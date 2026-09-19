using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddDigitalMenuPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var digitalMenuModuleId = new Guid("10000000-0000-0000-0000-000000000013");

            migrationBuilder.InsertData(
                table: "modules",
                columns: ["Id", "Code", "IsActive", "Name"],
                values: [digitalMenuModuleId, "digital_menu", true, "Digital Menu"]);

            migrationBuilder.InsertData(
                table: "permissions",
                columns: ["Id", "Code", "Description", "ModuleId"],
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000045"), "digital-menu.access", "digital-menu.access", digitalMenuModuleId },
                    { new Guid("20000000-0000-0000-0000-000000000046"), "digital-menu.enable", "digital-menu.enable", digitalMenuModuleId },
                    { new Guid("20000000-0000-0000-0000-000000000047"), "digital-menu.style.manage", "digital-menu.style.manage", digitalMenuModuleId },
                    { new Guid("20000000-0000-0000-0000-000000000048"), "digital-menu.items.manage", "digital-menu.items.manage", digitalMenuModuleId }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "permissions", keyColumn: "Id", keyValue: new Guid("20000000-0000-0000-0000-000000000045"));
            migrationBuilder.DeleteData(table: "permissions", keyColumn: "Id", keyValue: new Guid("20000000-0000-0000-0000-000000000046"));
            migrationBuilder.DeleteData(table: "permissions", keyColumn: "Id", keyValue: new Guid("20000000-0000-0000-0000-000000000047"));
            migrationBuilder.DeleteData(table: "permissions", keyColumn: "Id", keyValue: new Guid("20000000-0000-0000-0000-000000000048"));
            migrationBuilder.DeleteData(table: "modules", keyColumn: "Id", keyValue: new Guid("10000000-0000-0000-0000-000000000013"));
        }
    }
}
