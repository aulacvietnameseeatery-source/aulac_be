using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon",
                columns: table => new
                {
                    coupon_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    coupon_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    coupon_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    discount_value = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    max_usage = table.Column<int>(type: "int", nullable: true),
                    used_count = table.Column<int>(type: "int", nullable: true, defaultValueSql: "'0'"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    type_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    coupon_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.coupon_id);
                    table.ForeignKey(
                        name: "fk_coupon_status_lv",
                        column: x => x.coupon_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_coupon_type_lv",
                        column: x => x.type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "order_coupon",
                columns: table => new
                {
                    order_coupon_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    coupon_id = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    applied_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.order_coupon_id);
                    table.ForeignKey(
                        name: "fk_order_coupon_coupon",
                        column: x => x.coupon_id,
                        principalTable: "coupon",
                        principalColumn: "coupon_id");
                    table.ForeignKey(
                        name: "fk_order_coupon_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "idx_coupon_status_lv",
                table: "coupon",
                column: "coupon_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_coupon_type_lv",
                table: "coupon",
                column: "type_lv_id");

            migrationBuilder.CreateIndex(
                name: "uq_coupon_code",
                table: "coupon",
                column: "coupon_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_order_coupon_coupon",
                table: "order_coupon",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "uq_order_coupon",
                table: "order_coupon",
                columns: new[] { "order_id", "coupon_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_coupon");

            migrationBuilder.DropTable(
                name: "coupon");
        }
    }
}
