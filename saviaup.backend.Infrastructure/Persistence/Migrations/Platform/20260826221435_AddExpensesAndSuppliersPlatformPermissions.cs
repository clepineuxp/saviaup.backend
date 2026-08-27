using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddExpensesAndSuppliersPlatformPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000011"), "expenses", true, "Expenses" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "suppliers", true, "Suppliers" }
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000039"), "expenses.read", "expenses.read", new Guid("10000000-0000-0000-0000-000000000011") },
                    { new Guid("20000000-0000-0000-0000-000000000040"), "expenses.create", "expenses.create", new Guid("10000000-0000-0000-0000-000000000011") },
                    { new Guid("20000000-0000-0000-0000-000000000041"), "expenses.edit", "expenses.edit", new Guid("10000000-0000-0000-0000-000000000011") },
                    { new Guid("20000000-0000-0000-0000-000000000042"), "expenses.annul", "expenses.annul", new Guid("10000000-0000-0000-0000-000000000011") },
                    { new Guid("20000000-0000-0000-0000-000000000043"), "suppliers.read", "suppliers.read", new Guid("10000000-0000-0000-0000-000000000012") },
                    { new Guid("20000000-0000-0000-0000-000000000044"), "suppliers.manage", "suppliers.manage", new Guid("10000000-0000-0000-0000-000000000012") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"));
        }
    }
}
