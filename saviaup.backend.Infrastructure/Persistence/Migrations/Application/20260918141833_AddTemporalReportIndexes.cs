using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddTemporalReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId_CreatedAt",
                table: "orders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId_PaidAt",
                table: "orders",
                columns: new[] { "TenantId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_order_receipts_TenantId_CreatedAt",
                table: "order_receipts",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_TenantId_CreatedAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_TenantId_PaidAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_receipts_TenantId_CreatedAt",
                table: "order_receipts");
        }
    }
}
