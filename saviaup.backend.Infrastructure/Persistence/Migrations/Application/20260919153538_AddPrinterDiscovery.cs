using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPrinterDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "print_agent_discovered_printers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_agent_discovered_printers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_print_agent_discovered_printers_print_agents_PrintAgentId",
                        column: x => x.PrintAgentId,
                        principalTable: "print_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discovered_printers_PrintAgentId",
                table: "print_agent_discovered_printers",
                column: "PrintAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discovered_printers_TenantId_PrintAgentId_IsAva~",
                table: "print_agent_discovered_printers",
                columns: new[] { "TenantId", "PrintAgentId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discovered_printers_TenantId_PrintAgentId_Norma~",
                table: "print_agent_discovered_printers",
                columns: new[] { "TenantId", "PrintAgentId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_agent_discovered_printers");
        }
    }
}
