using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPrintingManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "print_agent_pairing_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_agent_pairing_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_print_agent_pairing_codes_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "print_agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hostname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LocalIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_print_agents_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "print_agent_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_agent_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_print_agent_credentials_print_agents_PrintAgentId",
                        column: x => x.PrintAgentId,
                        principalTable: "print_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "printers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConnectionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LocalPrinterName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: true),
                    PaperWidth = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.Id);
                    table.CheckConstraint("CK_printers_PaperWidth", "\"PaperWidth\" IN (58, 80)");
                    table.CheckConstraint("CK_printers_Port", "\"Port\" IS NULL OR (\"Port\" BETWEEN 1 AND 65535)");
                    table.ForeignKey(
                        name: "FK_printers_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_printers_print_agents_PrintAgentId",
                        column: x => x.PrintAgentId,
                        principalTable: "print_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "printing_zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printing_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printing_zones_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_printing_zones_print_agents_PrintAgentId",
                        column: x => x.PrintAgentId,
                        principalTable: "print_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category_printing_routes",
                columns: table => new
                {
                    PrintingZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_printing_routes", x => new { x.PrintingZoneId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_category_printing_routes_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_category_printing_routes_printing_zones_PrintingZoneId",
                        column: x => x.PrintingZoneId,
                        principalTable: "printing_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "print_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintingZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrintingAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrintedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OriginalPrintJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsReprint = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReprintRequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReprintRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_print_jobs_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_jobs_print_agents_PrintAgentId",
                        column: x => x.PrintAgentId,
                        principalTable: "print_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_jobs_print_jobs_OriginalPrintJobId",
                        column: x => x.OriginalPrintJobId,
                        principalTable: "print_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_jobs_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_jobs_printing_zones_PrintingZoneId",
                        column: x => x.PrintingZoneId,
                        principalTable: "printing_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "printing_zone_printers",
                columns: table => new
                {
                    PrintingZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printing_zone_printers", x => new { x.PrintingZoneId, x.PrinterId });
                    table.ForeignKey(
                        name: "FK_printing_zone_printers_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_printing_zone_printers_printing_zones_PrintingZoneId",
                        column: x => x.PrintingZoneId,
                        principalTable: "printing_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_printing_routes",
                columns: table => new
                {
                    PrintingZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_printing_routes", x => new { x.PrintingZoneId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_product_printing_routes_printing_zones_PrintingZoneId",
                        column: x => x.PrintingZoneId,
                        principalTable: "printing_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_printing_routes_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_printing_routes_CategoryId",
                table: "category_printing_routes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_category_printing_routes_TenantId_CategoryId",
                table: "category_printing_routes",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_locations_TenantId_IsDefault",
                table: "locations",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_locations_TenantId_NormalizedName",
                table: "locations",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_credentials_PrintAgentId",
                table: "print_agent_credentials",
                column: "PrintAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_credentials_TenantId_PrintAgentId_ExpiresAt",
                table: "print_agent_credentials",
                columns: new[] { "TenantId", "PrintAgentId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_credentials_TokenHash",
                table: "print_agent_credentials",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_pairing_codes_CodeHash",
                table: "print_agent_pairing_codes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_pairing_codes_LocationId",
                table: "print_agent_pairing_codes",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_print_agent_pairing_codes_TenantId_ExpiresAt_ConsumedAt",
                table: "print_agent_pairing_codes",
                columns: new[] { "TenantId", "ExpiresAt", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agents_LocationId",
                table: "print_agents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_print_agents_TenantId_Enabled_LastSeenAt",
                table: "print_agents",
                columns: new[] { "TenantId", "Enabled", "LastSeenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_print_agents_TenantId_LocationId_DeviceIdentifier",
                table: "print_agents",
                columns: new[] { "TenantId", "LocationId", "DeviceIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_LocationId",
                table: "print_jobs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_OriginalPrintJobId",
                table: "print_jobs",
                column: "OriginalPrintJobId");

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_PrintAgentId",
                table: "print_jobs",
                column: "PrintAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_PrinterId",
                table: "print_jobs",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_PrintingZoneId",
                table: "print_jobs",
                column: "PrintingZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_TenantId_PrintAgentId_Status",
                table: "print_jobs",
                columns: new[] { "TenantId", "PrintAgentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_TenantId_SourceType_SourceId",
                table: "print_jobs",
                columns: new[] { "TenantId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_print_jobs_TenantId_Status_CreatedAt",
                table: "print_jobs",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_printers_LocationId",
                table: "printers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_printers_PrintAgentId",
                table: "printers",
                column: "PrintAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_printers_TenantId_PrintAgentId_Name",
                table: "printers",
                columns: new[] { "TenantId", "PrintAgentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printing_zone_printers_PrinterId",
                table: "printing_zone_printers",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_printing_zone_printers_TenantId_PrintingZoneId",
                table: "printing_zone_printers",
                columns: new[] { "TenantId", "PrintingZoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_printing_zones_LocationId",
                table: "printing_zones",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_printing_zones_PrintAgentId",
                table: "printing_zones",
                column: "PrintAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_printing_zones_TenantId_LocationId_NormalizedName",
                table: "printing_zones",
                columns: new[] { "TenantId", "LocationId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_printing_routes_ProductId",
                table: "product_printing_routes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_printing_routes_TenantId_ProductId",
                table: "product_printing_routes",
                columns: new[] { "TenantId", "ProductId" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_printing_routes");

            migrationBuilder.DropTable(
                name: "print_agent_credentials");

            migrationBuilder.DropTable(
                name: "print_agent_pairing_codes");

            migrationBuilder.DropTable(
                name: "print_jobs");

            migrationBuilder.DropTable(
                name: "printing_zone_printers");

            migrationBuilder.DropTable(
                name: "product_printing_routes");

            migrationBuilder.DropTable(
                name: "printers");

            migrationBuilder.DropTable(
                name: "printing_zones");

            migrationBuilder.DropTable(
                name: "print_agents");

            migrationBuilder.DropTable(
                name: "locations");
        }
    }
}
