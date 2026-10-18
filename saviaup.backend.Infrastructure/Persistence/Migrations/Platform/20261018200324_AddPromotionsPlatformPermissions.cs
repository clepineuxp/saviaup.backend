using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddPromotionsPlatformPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000015"), "promotions", true, "Promotions" });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000057"), "promotions.read", "promotions.read", new Guid("10000000-0000-0000-0000-000000000015") },
                    { new Guid("20000000-0000-0000-0000-000000000058"), "promotions.manage", "promotions.manage", new Guid("10000000-0000-0000-0000-000000000015") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000057"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000058"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"));
        }
    }
}
