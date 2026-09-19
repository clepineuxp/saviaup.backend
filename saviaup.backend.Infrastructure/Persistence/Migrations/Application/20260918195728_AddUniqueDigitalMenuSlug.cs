using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaviaUp.Backend.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddUniqueDigitalMenuSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_organization_parameters_digital_menu_slug",
                table: "organization_parameters",
                column: "Value",
                unique: true,
                filter: "\"Key\" = 'business.digitalMenuSlug' AND btrim(\"Value\") <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_organization_parameters_digital_menu_slug",
                table: "organization_parameters");
        }
    }
}
