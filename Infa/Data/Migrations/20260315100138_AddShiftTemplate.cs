using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "shift_template_id",
                table: "shift_schedule",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shift_template",
                columns: table => new
                {
                    shift_template_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),

                    template_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    default_start_time = table.Column<TimeOnly>(type: "time", nullable: false),

                    default_end_time = table.Column<TimeOnly>(type: "time", nullable: false),

                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),

                    created_by = table.Column<long>(type: "bigint", nullable: false),

                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),

                    updated_by = table.Column<long>(type: "bigint", nullable: true),

                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.shift_template_id);

                    table.ForeignKey(
                        name: "fk_shift_template_created_by",
                        column: x => x.created_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");

                    table.ForeignKey(
                        name: "fk_shift_template_updated_by",
                        column: x => x.updated_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_shift_schedule_template",
                table: "shift_schedule",
                column: "shift_template_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_template_active",
                table: "shift_template",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_shift_template_name",
                table: "shift_template",
                column: "template_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_schedule_template",
                table: "shift_schedule",
                column: "shift_template_id",
                principalTable: "shift_template",
                principalColumn: "shift_template_id");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_schedule_template",
                table: "shift_schedule");

            migrationBuilder.DropTable(
                name: "shift_template");

            migrationBuilder.DropIndex(
                name: "idx_shift_schedule_template",
                table: "shift_schedule");

            migrationBuilder.DropColumn(
                name: "shift_template_id",
                table: "shift_schedule");

            migrationBuilder.AddColumn<uint>(
                name: "shift_type_lv_id",
                table: "shift_schedule",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "idx_shift_schedule_type_lv",
                table: "shift_schedule",
                column: "shift_type_lv_id");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_schedule_type_lv",
                table: "shift_schedule",
                column: "shift_type_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");
        }
    }
}
