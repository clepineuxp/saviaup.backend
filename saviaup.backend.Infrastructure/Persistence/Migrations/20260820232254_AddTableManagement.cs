using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresOpenCashRegister",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "cash_register_shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_register_shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_register_shifts_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cash_register_shifts_users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dining_areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dining_areas", x => x.Id);
                    table.CheckConstraint("CK_dining_areas_Order_Positive", "\"Order\" > 0");
                    table.ForeignKey(
                        name: "FK_dining_areas_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "restaurant_tables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiningAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    PositionX = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PositionY = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    IsCashRegister = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActiveOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveOrderTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccupiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurant_tables", x => x.Id);
                    table.CheckConstraint("CK_restaurant_tables_ActiveOrderTotal", "\"ActiveOrderTotal\" >= 0");
                    table.CheckConstraint("CK_restaurant_tables_Capacity", "\"Capacity\" BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_restaurant_tables_PositionX", "\"PositionX\" BETWEEN -100000 AND 100000");
                    table.CheckConstraint("CK_restaurant_tables_PositionY", "\"PositionY\" BETWEEN -100000 AND 100000");
                    table.ForeignKey(
                        name: "FK_restaurant_tables_dining_areas_DiningAreaId",
                        column: x => x.DiningAreaId,
                        principalTable: "dining_areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_restaurant_tables_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000023"), "tables.operate", "tables.operate", new Guid("10000000-0000-0000-0000-000000000002") });

            migrationBuilder.Sql(
                """
                INSERT INTO role_permissions ("RoleId", "PermissionId")
                SELECT r."Id", '20000000-0000-0000-0000-000000000023'::uuid
                FROM roles AS r
                WHERE r."Code" = 'TENANT_OWNER'
                ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_OpenedByUserId",
                table: "cash_register_shifts",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_TenantId_ClosedAt",
                table: "cash_register_shifts",
                columns: new[] { "TenantId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_dining_areas_TenantId_IsActive",
                table: "dining_areas",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_dining_areas_TenantId_NormalizedName",
                table: "dining_areas",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dining_areas_TenantId_Order",
                table: "dining_areas",
                columns: new[] { "TenantId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_tables_DiningAreaId",
                table: "restaurant_tables",
                column: "DiningAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_tables_TenantId_DiningAreaId_NormalizedName",
                table: "restaurant_tables",
                columns: new[] { "TenantId", "DiningAreaId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_tables_TenantId_Status",
                table: "restaurant_tables",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM role_permissions
                WHERE "PermissionId" = '20000000-0000-0000-0000-000000000023'::uuid;
                """);

            migrationBuilder.DropTable(
                name: "cash_register_shifts");

            migrationBuilder.DropTable(
                name: "restaurant_tables");

            migrationBuilder.DropTable(
                name: "dining_areas");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000023"));

            migrationBuilder.DropColumn(
                name: "RequiresOpenCashRegister",
                table: "tenants");
        }
    }
}
