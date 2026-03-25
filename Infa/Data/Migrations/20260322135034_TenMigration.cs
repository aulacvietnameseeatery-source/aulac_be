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
            // _OLD: original AddColumn/CreateIndex/AddForeignKey calls replaced with idempotent SQL
            // because the column already exists in the DB but this migration was not recorded in __EFMigrationsHistory

            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ingredient' AND COLUMN_NAME = 'unit_lv_id');
                SET @stmt = IF(@col_exists = 0, 
                    'ALTER TABLE `ingredient` ADD `unit_lv_id` int unsigned NOT NULL DEFAULT 0', 
                    'SELECT 1');
                PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            migrationBuilder.Sql(@"
                SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ingredient' AND INDEX_NAME = 'FK_ingredient_unit_lv_id');
                SET @stmt = IF(@idx_exists = 0, 
                    'CREATE INDEX `FK_ingredient_unit_lv_id` ON `ingredient` (`unit_lv_id`)', 
                    'SELECT 1');
                PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
            ");

            migrationBuilder.Sql(@"
                SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ingredient' AND CONSTRAINT_NAME = 'FK_ingredient_unit_lv_id' AND CONSTRAINT_TYPE = 'FOREIGN KEY');
                SET @stmt = IF(@fk_exists = 0, 
                    'ALTER TABLE `ingredient` ADD CONSTRAINT `FK_ingredient_unit_lv_id` FOREIGN KEY (`unit_lv_id`) REFERENCES `lookup_value` (`value_id`) ON DELETE CASCADE', 
                    'SELECT 1');
                PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
            ");
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
