using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToDishCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisPlayOrder",
                table: "dish_category",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisPlayOrder",
                table: "dish_category");
        }
    }
}
