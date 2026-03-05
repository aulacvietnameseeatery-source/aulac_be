using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinStockKebabLevelToCurrentStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinStockLevel",
                table: "current_stock",
                newName: "min_stock_level");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_stock_level",
                table: "current_stock",
                type: "decimal(14,3)",
                precision: 14,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "min_stock_level",
                table: "current_stock",
                newName: "MinStockLevel");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStockLevel",
                table: "current_stock",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(14,3)",
                oldPrecision: 14,
                oldScale: 3,
                oldDefaultValue: 0m);
        }
    }
}
