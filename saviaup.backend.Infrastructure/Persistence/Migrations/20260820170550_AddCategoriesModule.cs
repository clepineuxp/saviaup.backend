using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000009"), "categories", true, "Categories" });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000014"), "categories.read", "categories.read", new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("20000000-0000-0000-0000-000000000015"), "categories.manage", "categories.manage", new Guid("10000000-0000-0000-0000-000000000009") }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "role_permissions" ("RoleId", "PermissionId")
                SELECT r."Id", permission."PermissionId"
                FROM "roles" AS r
                CROSS JOIN (
                    VALUES
                        ('20000000-0000-0000-0000-000000000014'::uuid),
                        ('20000000-0000-0000-0000-000000000015'::uuid)
                ) AS permission("PermissionId")
                WHERE r."Code" = 'TENANT_OWNER'
                ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "role_permissions"
                WHERE "PermissionId" IN (
                    '20000000-0000-0000-0000-000000000014'::uuid,
                    '20000000-0000-0000-0000-000000000015'::uuid
                );
                """);

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "modules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"));
        }
    }
}
