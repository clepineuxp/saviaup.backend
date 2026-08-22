using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegistersProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserName",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedByUserName",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashRegisterShiftId",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashRegisterId",
                table: "cash_register_shifts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                table: "cash_register_shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserName",
                table: "cash_register_shifts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosingSummaryJson",
                table: "cash_register_shifts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenedByUserName",
                table: "cash_register_shifts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpeningBalancesJson",
                table: "cash_register_shifts",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "cash_register_shifts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OPEN");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCollectedAmount",
                table: "cash_register_shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalExpensesAmount",
                table: "cash_register_shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSalesAmount",
                table: "cash_register_shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTipsAmount",
                table: "cash_register_shifts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_CashRegisterShiftId",
                table: "orders",
                column: "CashRegisterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_CashRegisterId",
                table: "cash_register_shifts",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_ClosedByUserId",
                table: "cash_register_shifts",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_shifts_TenantId_CashRegisterId_Status",
                table: "cash_register_shifts",
                columns: new[] { "TenantId", "CashRegisterId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_cash_register_shifts_cash_registers_CashRegisterId",
                table: "cash_register_shifts",
                column: "CashRegisterId",
                principalTable: "cash_registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cash_register_shifts_users_ClosedByUserId",
                table: "cash_register_shifts",
                column: "ClosedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_cash_register_shifts_CashRegisterShiftId",
                table: "orders",
                column: "CashRegisterShiftId",
                principalTable: "cash_register_shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cash_register_shifts_cash_registers_CashRegisterId",
                table: "cash_register_shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_cash_register_shifts_users_ClosedByUserId",
                table: "cash_register_shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_cash_register_shifts_CashRegisterShiftId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_CashRegisterShiftId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_cash_register_shifts_CashRegisterId",
                table: "cash_register_shifts");

            migrationBuilder.DropIndex(
                name: "IX_cash_register_shifts_ClosedByUserId",
                table: "cash_register_shifts");

            migrationBuilder.DropIndex(
                name: "IX_cash_register_shifts_TenantId_CashRegisterId_Status",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedByUserName",
                table: "products");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserName",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CashRegisterShiftId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CashRegisterId",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "ClosedByUserName",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "ClosingSummaryJson",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "OpenedByUserName",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "OpeningBalancesJson",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "TotalCollectedAmount",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "TotalExpensesAmount",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "TotalSalesAmount",
                table: "cash_register_shifts");

            migrationBuilder.DropColumn(
                name: "TotalTipsAmount",
                table: "cash_register_shifts");
        }
    }
}
