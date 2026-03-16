using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderKebab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isDisabled",
                table: "dish_category",
                newName: "is_disable");

            migrationBuilder.RenameColumn(
                name: "DisPlayOrder",
                table: "dish_category",
                newName: "display_order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_disable",
                table: "dish_category",
                newName: "isDisabled");

            migrationBuilder.RenameColumn(
                name: "display_order",
                table: "dish_category",
                newName: "DisPlayOrder");
        }
    }
}
