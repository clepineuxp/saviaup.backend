using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddReportsModuleToSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000016"), "reports", true, "Reports" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "statistics.read", "statistics.read" });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000059"), "reports.read", "reports.read", new Guid("10000000-0000-0000-0000-000000000016") },
                    { new Guid("20000000-0000-0000-0000-000000000060"), "reports.manage", "reports.manage", new Guid("10000000-0000-0000-0000-000000000016") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000059"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"));

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"),
                columns: new[] { "Code", "Description" },
                values: new object[] { "reports.read", "reports.read" });
        }
    }
}
