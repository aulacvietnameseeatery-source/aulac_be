using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Migrations;

public partial class AddCustomerIdToCoupon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            SET @col_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND COLUMN_NAME = 'customer_id'
            );
            SET @stmt = IF(@col_exists = 0,
                'ALTER TABLE `coupon` ADD `customer_id` bigint NULL AFTER `coupon_status_lv_id`',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");

        migrationBuilder.Sql(@"
            SET @idx_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND INDEX_NAME = 'idx_coupon_customer_id'
            );
            SET @stmt = IF(@idx_exists = 0,
                'CREATE INDEX `idx_coupon_customer_id` ON `coupon` (`customer_id`)',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");

        migrationBuilder.Sql(@"
            SET @fk_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND CONSTRAINT_NAME = 'fk_coupon_customer'
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
            );
            SET @stmt = IF(@fk_exists = 0,
                'ALTER TABLE `coupon` ADD CONSTRAINT `fk_coupon_customer` FOREIGN KEY (`customer_id`) REFERENCES `customer` (`customer_id`) ON DELETE SET NULL',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            SET @fk_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND CONSTRAINT_NAME = 'fk_coupon_customer'
                  AND CONSTRAINT_TYPE = 'FOREIGN KEY'
            );
            SET @stmt = IF(@fk_exists = 1,
                'ALTER TABLE `coupon` DROP FOREIGN KEY `fk_coupon_customer`',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");

        migrationBuilder.Sql(@"
            SET @idx_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND INDEX_NAME = 'idx_coupon_customer_id'
            );
            SET @stmt = IF(@idx_exists = 1,
                'DROP INDEX `idx_coupon_customer_id` ON `coupon`',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");

        migrationBuilder.Sql(@"
            SET @col_exists = (
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'coupon'
                  AND COLUMN_NAME = 'customer_id'
            );
            SET @stmt = IF(@col_exists = 1,
                'ALTER TABLE `coupon` DROP COLUMN `customer_id`',
                'SELECT 1');
            PREPARE s FROM @stmt; EXECUTE s; DEALLOCATE PREPARE s;
        ");
    }
}