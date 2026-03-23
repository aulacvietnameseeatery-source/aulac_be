using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Migrations
{
    /// <inheritdoc />
    public partial class TenMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<uint>(
                name: "unit_lv_id",
                table: "ingredient",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_unit_lv_id",
                table: "ingredient",
                column: "unit_lv_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ingredient_unit_lv_id",
                table: "ingredient",
                column: "unit_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ingredient_unit_lv_id",
                table: "ingredient");

            migrationBuilder.DropIndex(
                name: "FK_ingredient_unit_lv_id",
                table: "ingredient");

            migrationBuilder.DropColumn(
                name: "unit_lv_id",
                table: "ingredient");

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "ingredient",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
