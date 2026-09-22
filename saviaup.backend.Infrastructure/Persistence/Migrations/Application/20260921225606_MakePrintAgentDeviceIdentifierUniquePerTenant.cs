using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakePrintAgentDeviceIdentifierUniquePerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_print_agents_TenantId_LocationId_DeviceIdentifier",
                table: "print_agents");

            migrationBuilder.CreateIndex(
                name: "IX_print_agents_TenantId_DeviceIdentifier",
                table: "print_agents",
                columns: new[] { "TenantId", "DeviceIdentifier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_print_agents_TenantId_DeviceIdentifier",
                table: "print_agents");

            migrationBuilder.CreateIndex(
                name: "IX_print_agents_TenantId_LocationId_DeviceIdentifier",
                table: "print_agents",
                columns: new[] { "TenantId", "LocationId", "DeviceIdentifier" },
                unique: true);
        }
    }
}
