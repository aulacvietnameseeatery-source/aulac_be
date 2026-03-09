using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingName_SystemSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "setting_name",
                table: "system_setting",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_value",
                table: "promotion",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "setting_name",
                table: "system_setting");

            migrationBuilder.DropColumn(
                name: "discount_value",
                table: "promotion");
        }
    }
}
