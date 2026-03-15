using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyShiftManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignment_role",
                table: "shift_assignment");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignment_schedule",
                table: "shift_assignment");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignment_status_lv",
                table: "shift_assignment");

            migrationBuilder.DropTable(
                name: "shift_schedule");

            migrationBuilder.DropIndex(
                name: "IX_shift_assignment_assignment_status_lv_id",
                table: "shift_assignment");

            migrationBuilder.DropIndex(
                name: "IX_shift_assignment_role_id",
                table: "shift_assignment");

            migrationBuilder.DropIndex(
                name: "uq_shift_assignment_schedule_staff",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "assignment_status_lv_id",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "shift_assignment");

            migrationBuilder.RenameColumn(
                name: "shift_schedule_id",
                table: "shift_assignment",
                newName: "shift_template_id");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "shift_assignment",
                newName: "notes");

            migrationBuilder.RenameIndex(
                name: "idx_shift_assignment_schedule",
                table: "shift_assignment",
                newName: "idx_shift_assignment_template");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "shift_assignment",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "shift_assignment",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "planned_end_at",
                table: "shift_assignment",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "planned_start_at",
                table: "shift_assignment",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "shift_assignment",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AddColumn<DateOnly>(
                name: "work_date",
                table: "shift_assignment",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_work_date",
                table: "shift_assignment",
                column: "work_date");

            migrationBuilder.CreateIndex(
                name: "uq_shift_assignment_template_date_staff",
                table: "shift_assignment",
                columns: new[] { "shift_template_id", "work_date", "staff_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignment_template",
                table: "shift_assignment",
                column: "shift_template_id",
                principalTable: "shift_template",
                principalColumn: "shift_template_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_assignment_template",
                table: "shift_assignment");

            migrationBuilder.DropIndex(
                name: "idx_shift_assignment_work_date",
                table: "shift_assignment");

            migrationBuilder.DropIndex(
                name: "uq_shift_assignment_template_date_staff",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "planned_end_at",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "planned_start_at",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "shift_assignment");

            migrationBuilder.DropColumn(
                name: "work_date",
                table: "shift_assignment");

            migrationBuilder.RenameColumn(
                name: "shift_template_id",
                table: "shift_assignment",
                newName: "shift_schedule_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "shift_assignment",
                newName: "remarks");

            migrationBuilder.RenameIndex(
                name: "idx_shift_assignment_template",
                table: "shift_assignment",
                newName: "idx_shift_assignment_schedule");

            migrationBuilder.AddColumn<uint>(
                name: "assignment_status_lv_id",
                table: "shift_assignment",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "role_id",
                table: "shift_assignment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "shift_schedule",
                columns: table => new
                {
                    shift_schedule_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    shift_template_id = table.Column<long>(type: "bigint", nullable: false),
                    status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    planned_end_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    planned_start_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.shift_schedule_id);
                    table.ForeignKey(
                        name: "fk_shift_schedule_created_by",
                        column: x => x.created_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_shift_schedule_status_lv",
                        column: x => x.status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_shift_schedule_template",
                        column: x => x.shift_template_id,
                        principalTable: "shift_template",
                        principalColumn: "shift_template_id");
                    table.ForeignKey(
                        name: "fk_shift_schedule_updated_by",
                        column: x => x.updated_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_assignment_status_lv_id",
                table: "shift_assignment",
                column: "assignment_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_role_id",
                table: "shift_assignment",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_shift_assignment_schedule_staff",
                table: "shift_assignment",
                columns: new[] { "shift_schedule_id", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shift_schedule_business_date",
                table: "shift_schedule",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "idx_shift_schedule_status_lv",
                table: "shift_schedule",
                column: "status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_schedule_template",
                table: "shift_schedule",
                column: "shift_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_schedule_created_by",
                table: "shift_schedule",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_shift_schedule_updated_by",
                table: "shift_schedule",
                column: "updated_by");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignment_role",
                table: "shift_assignment",
                column: "role_id",
                principalTable: "role",
                principalColumn: "role_id");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignment_schedule",
                table: "shift_assignment",
                column: "shift_schedule_id",
                principalTable: "shift_schedule",
                principalColumn: "shift_schedule_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_assignment_status_lv",
                table: "shift_assignment",
                column: "assignment_status_lv_id",
                principalTable: "lookup_value",
                principalColumn: "value_id");
        }
    }
}
