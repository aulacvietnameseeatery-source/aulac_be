using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Migrations
{
    /// <inheritdoc />
    public partial class InventoryRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit",
                table: "inventory_transaction_item");

            migrationBuilder.AddColumn<decimal>(
                name: "actual_quantity",
                table: "inventory_transaction_item",
                type: "decimal(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "system_quantity",
                table: "inventory_transaction_item",
                type: "decimal(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "unit_lv_id",
                table: "inventory_transaction_item",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "inventory_transaction_item",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "variance_reason_lv_id",
                table: "inventory_transaction_item",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "note",
                table: "inventory_transaction",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "inventory_transaction",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "approved_by",
                table: "inventory_transaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "export_reason_lv_id",
                table: "inventory_transaction",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stock_check_area_note",
                table: "inventory_transaction",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "inventory_transaction",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transaction_code",
                table: "inventory_transaction",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<uint>(
                name: "category_lv_id",
                table: "ingredient",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_inventory_tx_item_unit_lv",
                table: "inventory_transaction_item",
                column: "unit_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_tx_item_variance_reason_lv",
                table: "inventory_transaction_item",
                column: "variance_reason_lv_id");

            migrationBuilder.CreateIndex(
                name: "fk_inventory_transaction_approved_by",
                table: "inventory_transaction",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_tx_export_reason_lv",
                table: "inventory_transaction",
                column: "export_reason_lv_id");

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_category_lv_id",
                table: "ingredient",
                column: "category_lv_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ingredient_category_lv_id",
                table: "ingredient",
                column: "category_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_transaction_approved_by",
                table: "inventory_transaction",
                column: "approved_by",
                principalTable: "staff_account",
                principalColumn: "account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_tx_export_reason_lv",
                table: "inventory_transaction",
                column: "export_reason_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_tx_item_unit_lv",
                table: "inventory_transaction_item",
                column: "unit_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_tx_item_variance_reason_lv",
                table: "inventory_transaction_item",
                column: "variance_reason_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ingredient_category_lv_id",
                table: "ingredient");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_transaction_approved_by",
                table: "inventory_transaction");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_tx_export_reason_lv",
                table: "inventory_transaction");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_tx_item_unit_lv",
                table: "inventory_transaction_item");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_tx_item_variance_reason_lv",
                table: "inventory_transaction_item");

            migrationBuilder.DropIndex(
                name: "idx_inventory_tx_item_unit_lv",
                table: "inventory_transaction_item");

            migrationBuilder.DropIndex(
                name: "idx_inventory_tx_item_variance_reason_lv",
                table: "inventory_transaction_item");

            migrationBuilder.DropIndex(
                name: "fk_inventory_transaction_approved_by",
                table: "inventory_transaction");

            migrationBuilder.DropIndex(
                name: "idx_inventory_tx_export_reason_lv",
                table: "inventory_transaction");

            migrationBuilder.DropIndex(
                name: "FK_ingredient_category_lv_id",
                table: "ingredient");

            migrationBuilder.DropColumn(
                name: "actual_quantity",
                table: "inventory_transaction_item");

            migrationBuilder.DropColumn(
                name: "system_quantity",
                table: "inventory_transaction_item");

            migrationBuilder.DropColumn(
                name: "unit_lv_id",
                table: "inventory_transaction_item");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "inventory_transaction_item");

            migrationBuilder.DropColumn(
                name: "variance_reason_lv_id",
                table: "inventory_transaction_item");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "export_reason_lv_id",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "stock_check_area_note",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "transaction_code",
                table: "inventory_transaction");

            migrationBuilder.DropColumn(
                name: "category_lv_id",
                table: "ingredient");

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "inventory_transaction_item",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "note",
                table: "inventory_transaction",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");
        }
    }
}
