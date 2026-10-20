using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddSupportUsersAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSupportUser",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "support_access_audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticket = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AccessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_access_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_access_audits_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_support_access_audits_users_SupportUserId",
                        column: x => x.SupportUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_access_audits_AccessedAt",
                table: "support_access_audits",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_support_access_audits_SupportUserId",
                table: "support_access_audits",
                column: "SupportUserId");

            migrationBuilder.CreateIndex(
                name: "IX_support_access_audits_TenantId",
                table: "support_access_audits",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_access_audits");

            migrationBuilder.DropColumn(
                name: "IsSupportUser",
                table: "users");
        }
    }
}
