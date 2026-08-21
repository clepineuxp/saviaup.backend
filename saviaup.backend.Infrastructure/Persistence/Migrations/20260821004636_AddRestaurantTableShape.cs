using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantTableShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "restaurant_tables",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SQUARE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_restaurant_tables_Shape",
                table: "restaurant_tables",
                sql: "\"Shape\" IN ('SQUARE', 'ROUND', 'RECTANGLEHORIZONTAL', 'RECTANGLEVERTICAL')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_restaurant_tables_Shape",
                table: "restaurant_tables");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "restaurant_tables");
        }
    }
}
