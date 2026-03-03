using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToRestaurantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to be idempotent
            // (column may already exist from a partial migration run)
            migrationBuilder.Sql("""
                SET @col_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'restaurant_table'
                    AND COLUMN_NAME = 'is_deleted'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `restaurant_table` ADD COLUMN `is_deleted` tinyint(1) NOT NULL DEFAULT 0',
                    'SELECT ''Column already exists''');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "restaurant_table");
        }
    }
}
