using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "tenants",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "tenants",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Document",
                table: "tenants",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "tenants",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFileName",
                table: "tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleName",
                table: "tenants",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "tenants",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "tenants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledUntil",
                table: "tenant_memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_parameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_parameters_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsIncludedInCashOpening = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_methods_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
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
                        name: "FK_tenant_invitations_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                table: "permissions",
                columns: new[] { "Id", "Code", "Description", "ModuleId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000024"), "settings.organization.read", "settings.organization.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000025"), "settings.organization.manage", "settings.organization.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000026"), "settings.business.read", "settings.business.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000027"), "settings.business.manage", "settings.business.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000028"), "settings.payment-methods.read", "settings.payment-methods.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000029"), "settings.payment-methods.manage", "settings.payment-methods.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000030"), "settings.users.read", "settings.users.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000031"), "settings.users.manage", "settings.users.manage", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000032"), "settings.roles.read", "settings.roles.read", new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("20000000-0000-0000-0000-000000000033"), "settings.roles.manage", "settings.roles.manage", new Guid("10000000-0000-0000-0000-000000000008") }
                });

            migrationBuilder.Sql("""
                INSERT INTO "tenant_permissions" ("TenantId", "PermissionId")
                SELECT t."Id", p."Id" FROM "tenants" t CROSS JOIN "permissions" p
                ON CONFLICT DO NOTHING;

                INSERT INTO "role_permissions" ("RoleId", "PermissionId")
                SELECT r."Id", p."Id"
                FROM "roles" r CROSS JOIN "permissions" p
                WHERE p."Code" LIKE 'settings.%'
                  AND (r."Code" = 'TENANT_OWNER' OR EXISTS (
                    SELECT 1 FROM "role_permissions" current_rp
                    JOIN "permissions" current_p ON current_p."Id" = current_rp."PermissionId"
                    WHERE current_rp."RoleId" = r."Id" AND current_p."Code" = 'settings.manage'
                  ))
                ON CONFLICT DO NOTHING;

                INSERT INTO "organization_parameters" ("Id", "TenantId", "Key", "Value", "ValueType", "CreatedAt", "UpdatedAt")
                SELECT md5(t."Id"::text || v.key)::uuid, t."Id", v.key, v.value, v.type, NOW(), NOW()
                FROM "tenants" t
                CROSS JOIN (VALUES
                    ('business.usesTables', 'true', 'boolean'),
                    ('business.deliveryEnabled', 'false', 'boolean'),
                    ('business.requiresOpenCashRegister', CASE WHEN false THEN 'true' ELSE 'false' END, 'boolean'),
                    ('business.showVoluntaryTip', 'true', 'boolean'),
                    ('business.tipMessage', 'Servicio Voluntario', 'string'),
                    ('business.suggestedTipPercentage', '10', 'integer')
                ) AS v(key, value, type)
                ON CONFLICT DO NOTHING;

                UPDATE "organization_parameters" p
                SET "Value" = CASE WHEN t."RequiresOpenCashRegister" THEN 'true' ELSE 'false' END
                FROM "tenants" t
                WHERE p."TenantId" = t."Id" AND p."Key" = 'business.requiresOpenCashRegister';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_memberships_DisabledUntil",
                table: "tenant_memberships",
                column: "DisabledUntil");

            migrationBuilder.CreateIndex(
                name: "IX_organization_parameters_TenantId_Key",
                table: "organization_parameters",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_TenantId_IsActive",
                table: "payment_methods",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_TenantId_NormalizedName",
                table: "payment_methods",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

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
                name: "IX_tenant_permissions_PermissionId",
                table: "tenant_permissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "role_permissions"
                WHERE "PermissionId" IN (
                    SELECT "Id" FROM "permissions" WHERE "Code" LIKE 'settings.%' AND "Code" <> 'settings.manage'
                );
                """);
            migrationBuilder.DropTable(
                name: "organization_parameters");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "tenant_invitations");

            migrationBuilder.DropTable(
                name: "tenant_permissions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_memberships_DisabledUntil",
                table: "tenant_memberships");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000033"));

            migrationBuilder.DropColumn(
                name: "Address",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "City",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Document",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LogoFileName",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ResponsibleName",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "State",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DisabledUntil",
                table: "tenant_memberships");
        }
    }
}
