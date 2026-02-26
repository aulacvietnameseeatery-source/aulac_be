using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantTableAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "restaurant_table",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "qr_token",
                table: "restaurant_table",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "restaurant_table",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AddColumn<long>(
                name: "updated_by_staff_id",
                table: "restaurant_table",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_table_updated_by_staff_id",
                table: "restaurant_table",
                column: "updated_by_staff_id");

            migrationBuilder.AddForeignKey(
                name: "fk_restaurant_table_updated_by_staff",
                table: "restaurant_table",
                column: "updated_by_staff_id",
                principalTable: "staff_account",
                principalColumn: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_restaurant_table_updated_by_staff",
                table: "restaurant_table");

            migrationBuilder.DropIndex(
                name: "IX_restaurant_table_updated_by_staff_id",
                table: "restaurant_table");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "restaurant_table");

            migrationBuilder.DropColumn(
                name: "qr_token",
                table: "restaurant_table");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "restaurant_table");

            migrationBuilder.DropColumn(
                name: "updated_by_staff_id",
                table: "restaurant_table");
        }
    }
}
