using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddPrintingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000014"), "printing", true, "Printing" });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000049"), "printing.agents.read", "printing.agents.read", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000050"), "printing.agents.manage", "printing.agents.manage", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000051"), "printing.zones.read", "printing.zones.read", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000052"), "printing.zones.manage", "printing.zones.manage", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000053"), "printing.queue.read", "printing.queue.read", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000054"), "printing.queue.retry", "printing.queue.retry", new Guid("10000000-0000-0000-0000-000000000014") },
                    { new Guid("20000000-0000-0000-0000-000000000055"), "printing.queue.reprint", "printing.queue.reprint", new Guid("10000000-0000-0000-0000-000000000014") }
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000049"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000054"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000055"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"));
        }
    }
}
