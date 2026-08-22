using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class InitialPlatformDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ResponsibleName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Document = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LogoData = table.Column<byte[]>(type: "bytea", nullable: true),
                    LogoContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresOpenCashRegister = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_permissions_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "refresh_tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_invitations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_invitations_users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisabledUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_memberships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_permissions",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_permissions", x => new { x.TenantId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_tenant_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_permissions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "modules",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "orders", true, "Orders" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "tables", true, "Tables" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "inventory", true, "Inventory" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "products", true, "Products" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "kitchen", true, "Kitchen" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "reports", true, "Reports" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "billing", true, "Billing" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "settings", true, "Settings" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "categories", true, "Categories" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "cash_registers", true, "Cash Registers" }
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "orders.read", "orders.read", new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "orders.create", "orders.create", new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "orders.cancel", "orders.cancel", new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "tables.read", "tables.read", new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "tables.manage", "tables.manage", new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "inventory.read", "inventory.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "inventory.manage", "inventory.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000008"), "products.read", "products.read", new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("20000000-0000-0000-0000-000000000009"), "products.manage", "products.manage", new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("20000000-0000-0000-0000-000000000010"), "kitchen.read", "kitchen.read", new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("20000000-0000-0000-0000-000000000011"), "reports.read", "reports.read", new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("20000000-0000-0000-0000-000000000012"), "billing.manage", "billing.manage", new Guid("10000000-0000-0000-0000-000000000007") },
                    { new Guid("20000000-0000-0000-0000-000000000013"), "settings.manage", "settings.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000014"), "categories.read", "categories.read", new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("20000000-0000-0000-0000-000000000015"), "categories.manage", "categories.manage", new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("20000000-0000-0000-0000-000000000016"), "inventory.stock.read", "inventory.stock.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000017"), "inventory.ingredients.read", "inventory.ingredients.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000018"), "inventory.ingredients.manage", "inventory.ingredients.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000019"), "inventory.movements.read", "inventory.movements.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000020"), "inventory.movements.manage", "inventory.movements.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000021"), "inventory.complements.read", "inventory.complements.read", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000022"), "inventory.complements.manage", "inventory.complements.manage", new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000023"), "tables.operate", "tables.operate", new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000024"), "settings.organization.read", "settings.organization.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000025"), "settings.organization.manage", "settings.organization.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000026"), "settings.business.read", "settings.business.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000027"), "settings.business.manage", "settings.business.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000028"), "settings.payment-methods.read", "settings.payment-methods.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000029"), "settings.payment-methods.manage", "settings.payment-methods.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000030"), "settings.users.read", "settings.users.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000031"), "settings.users.manage", "settings.users.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000032"), "settings.roles.read", "settings.roles.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000033"), "settings.roles.manage", "settings.roles.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000034"), "cash-registers.read", "cash-registers.read", new Guid("10000000-0000-0000-0000-000000000010") },
                    { new Guid("20000000-0000-0000-0000-000000000035"), "cash-registers.operate", "cash-registers.operate", new Guid("10000000-0000-0000-0000-000000000010") },
                    { new Guid("20000000-0000-0000-0000-000000000036"), "cash-registers.manage", "cash-registers.manage", new Guid("10000000-0000-0000-0000-000000000010") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_modules_Code",
                table: "modules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_ExpiresAt",
                table: "password_reset_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TokenHash",
                table: "password_reset_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_ModuleId",
                table: "permissions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ReplacedByTokenId",
                table: "refresh_tokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_RoleId",
                table: "refresh_tokens",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_SessionId",
                table: "refresh_tokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_InvitedByUserId",
                table: "tenant_invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_NormalizedEmail",
                table: "tenant_invitations",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_RoleId",
                table: "tenant_invitations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_TenantId_NormalizedEmail",
                table: "tenant_invitations",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_DisabledUntil",
                table: "tenant_memberships",
                column: "DisabledUntil");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_RoleId",
                table: "tenant_memberships",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_TenantId",
                table: "tenant_memberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_UserId",
                table: "tenant_memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_UserId_TenantId",
                table: "tenant_memberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_permissions_PermissionId",
                table: "tenant_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_IsActive",
                table: "tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_users_LastTenantId",
                table: "users",
                column: "LastTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedEmail",
                table: "users",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "tenant_invitations");

            migrationBuilder.DropTable(
                name: "tenant_memberships");

            migrationBuilder.DropTable(
                name: "tenant_permissions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "modules");
        }
    }
}
