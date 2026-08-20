using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "measurement_units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_measurement_units_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CurrentStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingredients_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ingredients_measurement_units_MeasurementUnitId",
                        column: x => x.MeasurementUnitId,
                        principalTable: "measurement_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ingredients_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO measurement_units
                    ("Id", "TenantId", "Code", "NormalizedCode", "Name", "NormalizedName", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT (md5(t."Id"::text || ':' || unit."Code"))::uuid,
                       t."Id", unit."Code", upper(unit."Code"), unit."Name", upper(unit."Name"), TRUE,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM tenants AS t
                CROSS JOIN (VALUES
                    ('gr', 'gramos'),
                    ('kg', 'kilogramos'),
                    ('und', 'unidades')
                ) AS unit("Code", "Name");
                """);

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    StockBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    StockAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000016"), "inventory.stock.read", "inventory.stock.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000017"), "inventory.ingredients.read", "inventory.ingredients.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000018"), "inventory.ingredients.manage", "inventory.ingredients.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000019"), "inventory.movements.read", "inventory.movements.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000020"), "inventory.movements.manage", "inventory.movements.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000021"), "inventory.complements.read", "inventory.complements.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000022"), "inventory.complements.manage", "inventory.complements.manage", new Guid("10000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO role_permissions ("RoleId", "PermissionId")
                SELECT r."Id", permission."PermissionId"
                FROM roles AS r
                CROSS JOIN (VALUES
                    ('20000000-0000-0000-0000-000000000016'::uuid),
                    ('20000000-0000-0000-0000-000000000017'::uuid),
                    ('20000000-0000-0000-0000-000000000018'::uuid),
                    ('20000000-0000-0000-0000-000000000019'::uuid),
                    ('20000000-0000-0000-0000-000000000020'::uuid),
                    ('20000000-0000-0000-0000-000000000021'::uuid),
                    ('20000000-0000-0000-0000-000000000022'::uuid)
                ) AS permission("PermissionId")
                WHERE r."Code" = 'TENANT_OWNER'
                ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_CategoryId",
                table: "ingredients",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_MeasurementUnitId",
                table: "ingredients",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_TenantId_CategoryId",
                table: "ingredients",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_TenantId_IsActive_NormalizedName",
                table: "ingredients",
                columns: new[] { "TenantId", "IsActive", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_TenantId_MeasurementUnitId",
                table: "ingredients",
                columns: new[] { "TenantId", "MeasurementUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_CreatedByUserId",
                table: "inventory_movements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_IngredientId_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "IngredientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_TenantId_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_measurement_units_TenantId_IsActive_Name",
                table: "measurement_units",
                columns: new[] { "TenantId", "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_measurement_units_TenantId_NormalizedCode",
                table: "measurement_units",
                columns: new[] { "TenantId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_measurement_units_TenantId_NormalizedName",
                table: "measurement_units",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM role_permissions
                WHERE "PermissionId" IN (
                    '20000000-0000-0000-0000-000000000016'::uuid,
                    '20000000-0000-0000-0000-000000000017'::uuid,
                    '20000000-0000-0000-0000-000000000018'::uuid,
                    '20000000-0000-0000-0000-000000000019'::uuid,
                    '20000000-0000-0000-0000-000000000020'::uuid,
                    '20000000-0000-0000-0000-000000000021'::uuid,
                    '20000000-0000-0000-0000-000000000022'::uuid
                );
                """);

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "measurement_units");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000022"));
        }
    }
}
