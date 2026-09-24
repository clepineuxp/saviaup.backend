using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPersistentPrintAgentDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "print_agent_discoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NetworkFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hostname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocalIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCredentialIssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_agent_discoveries", x => x.Id);
                    table.CheckConstraint("CK_print_agent_discoveries_Status", "\"Status\" IN ('PENDING', 'AUTHORIZED', 'CONSUMED')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discoveries_DeviceIdentifier_ExpiresAt",
                table: "print_agent_discoveries",
                columns: new[] { "DeviceIdentifier", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discoveries_NetworkFingerprint_Status_ExpiresAt",
                table: "print_agent_discoveries",
                columns: new[] { "NetworkFingerprint", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_discoveries_SecretHash",
                table: "print_agent_discoveries",
                column: "SecretHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_agent_discoveries");
        }
    }
}
