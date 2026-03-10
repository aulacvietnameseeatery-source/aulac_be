using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_activity",
                columns: table => new
                {
                    login_activity_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<long>(type: "bigint", nullable: true),
                    event_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.login_activity_id);
                    table.ForeignKey(
                        name: "fk_login_activity_session",
                        column: x => x.session_id,
                        principalTable: "auth_session",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_login_activity_staff",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "shift_schedule",
                columns: table => new
                {
                    shift_schedule_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_type_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    planned_start_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    planned_end_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
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
                        name: "fk_shift_schedule_type_lv",
                        column: x => x.shift_type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_shift_schedule_updated_by",
                        column: x => x.updated_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "shift_assignment",
                columns: table => new
                {
                    shift_assignment_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    shift_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    assignment_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    assigned_by = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.shift_assignment_id);
                    table.ForeignKey(
                        name: "fk_shift_assignment_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_shift_assignment_role",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "fk_shift_assignment_schedule",
                        column: x => x.shift_schedule_id,
                        principalTable: "shift_schedule",
                        principalColumn: "shift_schedule_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shift_assignment_staff",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_shift_assignment_status_lv",
                        column: x => x.assignment_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "attendance_record",
                columns: table => new
                {
                    attendance_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    shift_assignment_id = table.Column<long>(type: "bigint", nullable: false),
                    attendance_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    actual_check_in_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    actual_check_out_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    late_minutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    early_leave_minutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    worked_minutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_manual_adjustment = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    adjustment_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<long>(type: "bigint", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.attendance_id);
                    table.ForeignKey(
                        name: "fk_attendance_assignment",
                        column: x => x.shift_assignment_id,
                        principalTable: "shift_assignment",
                        principalColumn: "shift_assignment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attendance_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_attendance_status_lv",
                        column: x => x.attendance_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "idx_attendance_status_lv",
                table: "attendance_record",
                column: "attendance_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_record_reviewed_by",
                table: "attendance_record",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "uq_attendance_assignment",
                table: "attendance_record",
                column: "shift_assignment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_login_activity_occurred_at",
                table: "login_activity",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "idx_login_activity_staff",
                table: "login_activity",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_activity_session_id",
                table: "login_activity",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_schedule",
                table: "shift_assignment",
                column: "shift_schedule_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_staff",
                table: "shift_assignment",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_assigned_by",
                table: "shift_assignment",
                column: "assigned_by");

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
                name: "idx_shift_schedule_type_lv",
                table: "shift_schedule",
                column: "shift_type_lv_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_schedule_created_by",
                table: "shift_schedule",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_shift_schedule_updated_by",
                table: "shift_schedule",
                column: "updated_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_record");

            migrationBuilder.DropTable(
                name: "login_activity");

            migrationBuilder.DropTable(
                name: "shift_assignment");

            migrationBuilder.DropTable(
                name: "shift_schedule");
        }
    }
}
