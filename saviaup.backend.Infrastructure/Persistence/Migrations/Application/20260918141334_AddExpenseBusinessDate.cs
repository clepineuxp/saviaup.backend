using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddExpenseBusinessDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BusinessDate",
                table: "expenses",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_TenantId_BusinessDate",
                table: "expenses",
                columns: new[] { "TenantId", "BusinessDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_expenses_TenantId_BusinessDate",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "expenses");
        }
    }
}
