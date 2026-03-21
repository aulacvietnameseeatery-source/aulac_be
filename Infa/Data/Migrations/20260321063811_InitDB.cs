using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infa.Migrations
{
    /// <inheritdoc />
    public partial class InitDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    customer_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    full_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_member = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    loyalty_points = table.Column<int>(type: "int", nullable: true, defaultValueSql: "'0'"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.customer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "email_template",
                columns: table => new
                {
                    TemplateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyHtml = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_template", x => x.TemplateId);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "i18n_language",
                columns: table => new
                {
                    lang_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lang_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.lang_code);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    notification_preference_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    notification_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    sound_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.notification_preference_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    require_ack = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sound_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metadata_json = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_permissions = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_user_ids = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.notification_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    permission_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    screen_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.permission_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "supplier",
                columns: table => new
                {
                    supplier_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    supplier_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.supplier_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "i18n_text",
                columns: table => new
                {
                    text_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    text_key = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_lang_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'en'", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_text = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    context = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.text_id);
                    table.ForeignKey(
                        name: "fk_i18n_text_lang",
                        column: x => x.source_lang_code,
                        principalTable: "i18n_language",
                        principalColumn: "lang_code");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "notification_read_states",
                columns: table => new
                {
                    notification_read_state_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    is_read = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    is_acknowledged = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.notification_read_state_id);
                    table.ForeignKey(
                        name: "fk_nrs_notification",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "dish_category",
                columns: table => new
                {
                    category_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_text_id = table.Column<long>(type: "bigint", nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_disable = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.category_id);
                    table.ForeignKey(
                        name: "FK_dish_category_i18n_text_text_id",
                        column: x => x.description_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_cat_name_text",
                        column: x => x.category_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "i18n_translation",
                columns: table => new
                {
                    text_id = table.Column<long>(type: "bigint", nullable: false),
                    lang_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    translated_text = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.text_id, x.lang_code })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "fk_i18n_tr_lang",
                        column: x => x.lang_code,
                        principalTable: "i18n_language",
                        principalColumn: "lang_code");
                    table.ForeignKey(
                        name: "fk_i18n_tr_text",
                        column: x => x.text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "lookup_type",
                columns: table => new
                {
                    type_id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_configurable = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "1 = admin can add/remove values,0 = controlled enum (statuses, workflows)"),
                    is_system = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "1 = system-defined enum type,0 = user-defined/custom type"),
                    type_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    type_desc_text_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.type_id);
                    table.ForeignKey(
                        name: "fk_lookup_type_desc_text",
                        column: x => x.type_desc_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_lookup_type_name_text",
                        column: x => x.type_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "service_error_category",
                columns: table => new
                {
                    category_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    category_desc_text_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.category_id);
                    table.ForeignKey(
                        name: "fk_sec_desc_text",
                        column: x => x.category_desc_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_sec_name_text",
                        column: x => x.category_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "lookup_value",
                columns: table => new
                {
                    value_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    value_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'"),
                    meta = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_system = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "1 = system/seeded value,0 = user-added value"),
                    locked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "1 = value_code cannot be changed and value cannot be deleted"),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "Soft delete timestamp; never hard delete lookup values"),
                    description = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    update_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    value_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    value_desc_text_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.value_id);
                    table.ForeignKey(
                        name: "fk_lookup_value_desc_text",
                        column: x => x.value_desc_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_lookup_value_name_text",
                        column: x => x.value_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_lookup_value_type",
                        column: x => x.type_id,
                        principalTable: "lookup_type",
                        principalColumn: "type_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

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
                name: "dish",
                columns: table => new
                {
                    dish_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    dish_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    dish_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    slogan = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true, collation: "utf8mb3_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb3"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb3_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb3"),
                    calories = table.Column<int>(type: "int", nullable: true),
                    short_description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb3_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb3"),
                    display_order = table.Column<sbyte>(type: "tinyint", nullable: true),
                    chef_recommended = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    prep_time_minutes = table.Column<int>(type: "int", nullable: true),
                    cook_time_minutes = table.Column<int>(type: "int", nullable: true),
                    isOnline = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'"),
                    description_text_id = table.Column<long>(type: "bigint", nullable: true),
                    slogan_text_id = table.Column<long>(type: "bigint", nullable: true),
                    note_text_id = table.Column<long>(type: "bigint", nullable: true),
                    short_description_text_id = table.Column<long>(type: "bigint", nullable: true),
                    dish_name_text_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.dish_id);
                    table.ForeignKey(
                        name: "dish_ibfk_1",
                        column: x => x.category_id,
                        principalTable: "dish_category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_dish_desc_text",
                        column: x => x.description_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_dish_name_text",
                        column: x => x.dish_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_dish_note_text",
                        column: x => x.note_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_dish_short_desc_text",
                        column: x => x.short_description_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_dish_slogan_text",
                        column: x => x.slogan_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_dish_status_lv",
                        column: x => x.dish_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "media_asset",
                columns: table => new
                {
                    media_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mime_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    width = table.Column<int>(type: "int", nullable: true),
                    height = table.Column<int>(type: "int", nullable: true),
                    duration_sec = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    media_type_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.media_id);
                    table.ForeignKey(
                        name: "fk_media_asset_type_lv",
                        column: x => x.media_type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "promotion",
                columns: table => new
                {
                    promotion_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    promo_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    promo_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
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
                    promotion_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    promo_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    promo_desc_text_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.promotion_id);
                    table.ForeignKey(
                        name: "fk_promo_desc_text",
                        column: x => x.promo_desc_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_promo_name_text",
                        column: x => x.promo_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                    table.ForeignKey(
                        name: "fk_promotion_status_lv",
                        column: x => x.promotion_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_promotion_type_lv",
                        column: x => x.type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "reservation",
                columns: table => new
                {
                    reservation_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    customer_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    party_size = table.Column<int>(type: "int", nullable: false),
                    reserved_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    source_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reservation_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.reservation_id);
                    table.ForeignKey(
                        name: "fk_reservation_source_lv",
                        column: x => x.source_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_reservation_status_lv",
                        column: x => x.reservation_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "reservation_ibfk_1",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "customer_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    role_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.role_id);
                    table.ForeignKey(
                        name: "FK_role_lookup_value_role_status_lv_id",
                        column: x => x.role_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "dish_tag",
                columns: table => new
                {
                    dish_tag_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    dish_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.dish_tag_id);
                    table.ForeignKey(
                        name: "FK_dish_tag_dish_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                    table.ForeignKey(
                        name: "FK_dish_tag_lookup_value_value_id",
                        column: x => x.tag_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "dish_media",
                columns: table => new
                {
                    dish_id = table.Column<long>(type: "bigint", nullable: false),
                    media_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.dish_id, x.media_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "dish_media_ibfk_1",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                    table.ForeignKey(
                        name: "dish_media_ibfk_2",
                        column: x => x.media_id,
                        principalTable: "media_asset",
                        principalColumn: "media_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "ingredient",
                columns: table => new
                {
                    ingredient_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ingredient_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_lv_id = table.Column<uint>(type: "int unsigned", nullable: true),
                    ingredient_name_text_id = table.Column<long>(type: "bigint", nullable: true),
                    image_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.ingredient_id);
                    table.ForeignKey(
                        name: "FK_ingredient_image_id",
                        column: x => x.image_id,
                        principalTable: "media_asset",
                        principalColumn: "media_id");
                    table.ForeignKey(
                        name: "FK_ingredient_type_lv_id",
                        column: x => x.type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_ingredient_name_text",
                        column: x => x.ingredient_name_text_id,
                        principalTable: "i18n_text",
                        principalColumn: "text_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "promotion_rule",
                columns: table => new
                {
                    rule_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    promotion_id = table.Column<long>(type: "bigint", nullable: false),
                    min_order_value = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    min_quantity = table.Column<int>(type: "int", nullable: true),
                    required_dish_id = table.Column<long>(type: "bigint", nullable: true),
                    required_category_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.rule_id);
                    table.ForeignKey(
                        name: "promotion_rule_ibfk_1",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "promotion_id");
                    table.ForeignKey(
                        name: "promotion_rule_ibfk_2",
                        column: x => x.required_dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                    table.ForeignKey(
                        name: "promotion_rule_ibfk_3",
                        column: x => x.required_category_id,
                        principalTable: "dish_category",
                        principalColumn: "category_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "promotion_target",
                columns: table => new
                {
                    target_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    promotion_id = table.Column<long>(type: "bigint", nullable: false),
                    dish_id = table.Column<long>(type: "bigint", nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.target_id);
                    table.ForeignKey(
                        name: "promotion_target_ibfk_1",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "promotion_id");
                    table.ForeignKey(
                        name: "promotion_target_ibfk_2",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                    table.ForeignKey(
                        name: "promotion_target_ibfk_3",
                        column: x => x.category_id,
                        principalTable: "dish_category",
                        principalColumn: "category_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.role_id, x.permission_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "role_permission_ibfk_1",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "role_permission_ibfk_2",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "permission_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "staff_account",
                columns: table => new
                {
                    account_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    full_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    RegisteredDeviceId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.account_id);
                    table.ForeignKey(
                        name: "staff_account_ibfk_1",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "current_stock",
                columns: table => new
                {
                    ingredient_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    min_stock_level = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.ingredient_id);
                    table.ForeignKey(
                        name: "fk_current_stock_ingredient",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient",
                        principalColumn: "ingredient_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "ingredient_supplier",
                columns: table => new
                {
                    ingredient_supplier_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    ingredient_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.ingredient_supplier_id);
                    table.ForeignKey(
                        name: "FK_ingredient_supplier_ingredient_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient",
                        principalColumn: "ingredient_id");
                    table.ForeignKey(
                        name: "FK_ingredient_supplier_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "supplier_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "recipe",
                columns: table => new
                {
                    dish_id = table.Column<long>(type: "bigint", nullable: false),
                    ingredient_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.dish_id, x.ingredient_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "fk_recipe_dish",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                    table.ForeignKey(
                        name: "fk_recipe_ingredient",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient",
                        principalColumn: "ingredient_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: true),
                    action_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_table = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.log_id);
                    table.ForeignKey(
                        name: "audit_log_ibfk_1",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    session_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    device_info = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.session_id);
                    table.ForeignKey(
                        name: "auth_session_ibfk_1",
                        column: x => x.user_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "inventory_transaction",
                columns: table => new
                {
                    transaction_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.transaction_id);
                    table.ForeignKey(
                        name: "FK_inventory_transaction_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_inventory_transaction_staff",
                        column: x => x.created_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_inventory_tx_status_lv",
                        column: x => x.status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_inventory_tx_type_lv",
                        column: x => x.type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "restaurant_table",
                columns: table => new
                {
                    table_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    table_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    table_qr_img = table.Column<long>(type: "bigint", nullable: true),
                    table_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    table_type_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    zone_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    isOnline = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'"),
                    qr_token = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    updated_by_staff_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.table_id);
                    table.ForeignKey(
                        name: "FK_restaurant_table_table_qr_img",
                        column: x => x.table_qr_img,
                        principalTable: "media_asset",
                        principalColumn: "media_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_restaurant_table_status_lv",
                        column: x => x.table_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_restaurant_table_type_lv",
                        column: x => x.table_type_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_restaurant_table_updated_by_staff",
                        column: x => x.updated_by_staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_restaurant_table_zone_lv",
                        column: x => x.zone_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "shift_template",
                columns: table => new
                {
                    shift_template_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    template_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    default_start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    default_end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    buffer_before_minutes = table.Column<int>(type: "int", nullable: true),
                    buffer_after_minutes = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "system_setting",
                columns: table => new
                {
                    setting_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    setting_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    setting_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_type = table.Column<string>(type: "enum('STRING','INT','DECIMAL','BOOL','JSON','DATETIME')", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_string = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_int = table.Column<long>(type: "bigint", nullable: true),
                    value_decimal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    value_bool = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    value_json = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_sensitive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    updated_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.setting_id);
                    table.ForeignKey(
                        name: "fk_setting_updated_by",
                        column: x => x.updated_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

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
                name: "inventory_transaction_item",
                columns: table => new
                {
                    transaction_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    ingredient_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.transaction_item_id);
                    table.ForeignKey(
                        name: "fk_inventory_transaction_item_ingredient",
                        column: x => x.ingredient_id,
                        principalTable: "ingredient",
                        principalColumn: "ingredient_id");
                    table.ForeignKey(
                        name: "fk_inventory_transaction_item_transaction",
                        column: x => x.transaction_id,
                        principalTable: "inventory_transaction",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "inventory_transaction_media",
                columns: table => new
                {
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    media_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.transaction_id, x.media_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "fk_inventory_transaction_media_asset",
                        column: x => x.media_id,
                        principalTable: "media_asset",
                        principalColumn: "media_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_transaction_media_transaction",
                        column: x => x.transaction_id,
                        principalTable: "inventory_transaction",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    table_id = table.Column<long>(type: "bigint", nullable: true),
                    staff_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    tip_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    source_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    order_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_orders_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_orders_source_lv",
                        column: x => x.source_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_orders_status_lv",
                        column: x => x.order_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "orders_ibfk_1",
                        column: x => x.table_id,
                        principalTable: "restaurant_table",
                        principalColumn: "table_id");
                    table.ForeignKey(
                        name: "orders_ibfk_2",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "reservation_table",
                columns: table => new
                {
                    reservation_id = table.Column<long>(type: "bigint", nullable: false),
                    table_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.reservation_id, x.table_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "reservation_table_ibfk_1",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "reservation_id");
                    table.ForeignKey(
                        name: "reservation_table_ibfk_2",
                        column: x => x.table_id,
                        principalTable: "restaurant_table",
                        principalColumn: "table_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "table_media",
                columns: table => new
                {
                    table_id = table.Column<long>(type: "bigint", nullable: false),
                    media_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.table_id, x.media_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "table_media_ibfk_1",
                        column: x => x.table_id,
                        principalTable: "restaurant_table",
                        principalColumn: "table_id");
                    table.ForeignKey(
                        name: "table_media_ibfk_2",
                        column: x => x.media_id,
                        principalTable: "media_asset",
                        principalColumn: "media_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "shift_assignment",
                columns: table => new
                {
                    shift_assignment_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    shift_template_id = table.Column<long>(type: "bigint", nullable: false),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    planned_end_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    assignment_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    tags = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    assigned_by = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
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
                        name: "fk_shift_assignment_staff",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_shift_assignment_status_lv",
                        column: x => x.assignment_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "fk_shift_assignment_template",
                        column: x => x.shift_template_id,
                        principalTable: "shift_template",
                        principalColumn: "shift_template_id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "order_item",
                columns: table => new
                {
                    order_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    dish_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    item_status = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'", comment: "OrderItemStatus:1=CREATED,2=IN_PROGRESS,3=READY,4=SERVED,5=REJECTED"),
                    reject_reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_status_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.order_item_id);
                    table.ForeignKey(
                        name: "fk_order_item_status_lv",
                        column: x => x.item_status_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "order_item_ibfk_1",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "order_item_ibfk_2",
                        column: x => x.dish_id,
                        principalTable: "dish",
                        principalColumn: "dish_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "order_promotion",
                columns: table => new
                {
                    order_promotion_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    promotion_id = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    applied_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.order_promotion_id);
                    table.ForeignKey(
                        name: "FK_invoice_promotion_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "order_promotion_ibfk_2",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "promotion_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    payment_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    received_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    method_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.payment_id);
                    table.ForeignKey(
                        name: "fk_payment_method_lv",
                        column: x => x.method_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "payment_ibfk_1",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
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

            migrationBuilder.CreateTable(
                name: "service_error",
                columns: table => new
                {
                    error_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    order_item_id = table.Column<long>(type: "bigint", nullable: true),
                    table_id = table.Column<long>(type: "bigint", nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    penalty_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true, defaultValueSql: "'0.00'"),
                    is_resolved = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    resolved_by = table.Column<long>(type: "bigint", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    severity_lv_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.error_id);
                    table.ForeignKey(
                        name: "fk_service_error_severity_lv",
                        column: x => x.severity_lv_id,
                        principalTable: "lookup_value",
                        principalColumn: "value_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_1",
                        column: x => x.staff_id,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_2",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_3",
                        column: x => x.order_item_id,
                        principalTable: "order_item",
                        principalColumn: "order_item_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_4",
                        column: x => x.table_id,
                        principalTable: "restaurant_table",
                        principalColumn: "table_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_5",
                        column: x => x.category_id,
                        principalTable: "service_error_category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "service_error_ibfk_7",
                        column: x => x.resolved_by,
                        principalTable: "staff_account",
                        principalColumn: "account_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "time_log",
                columns: table => new
                {
                    time_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    attendance_record_id = table.Column<long>(type: "bigint", nullable: false),
                    punch_in_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    punch_out_time = table.Column<DateTime>(type: "datetime", nullable: true),
                    gps_location_in = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gps_location_out = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_id_in = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_id_out = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    validation_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "Valid", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    punch_duration_minutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.time_log_id);
                    table.ForeignKey(
                        name: "fk_time_log_attendance",
                        column: x => x.attendance_record_id,
                        principalTable: "attendance_record",
                        principalColumn: "attendance_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.InsertData(
                table: "email_template",
                columns: new[] { "TemplateId", "BodyHtml", "CreatedAt", "Description", "Subject", "TemplateCode", "TemplateName" },
                values: new object[,]
                {
                    { 1L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .button { \r\n            display: inline-block; \r\n            padding: 12px 24px; \r\n            background-color: #007bff; \r\n            color: #ffffff; \r\n            text-decoration: none; \r\n            border-radius: 4px; \r\n            margin: 20px 0;\r\n        }\r\n        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2>Password Reset Request</h2>\r\n        <p>Hello {{username}},</p>\r\n        <p>We received a request to reset your password. Click the button below to create a new password:</p>\r\n        <a href=\"{{resetLink}}\" class=\"button\">Reset Password</a>\r\n        <p>Or copy and paste this link into your browser:</p>\r\n        <p><a href=\"{{resetLink}}\">{{resetLink}}</a></p>\r\n        <div class=\"warning\">\r\n            <strong>Security Notice:</strong>\r\n            <ul>\r\n                <li>This link will expire in {{expiryMinutes}} minutes</li>\r\n                <li>If you didn't request a password reset, you can safely ignore this email</li>\r\n                <li>Never share this link with anyone</li>\r\n            </ul>\r\n        </div>\r\n        <p>Best regards,<br>Your Application Team</p>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent when a user forgets their password.", "Password Reset Request", "FORGOT_PASSWORD", "Forgot Password" },
                    { 2L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .credentials { background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }\r\n        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 20px 0; }\r\n        .code { font-family: 'Courier New', monospace; font-size: 16px; font-weight: bold; color: #007bff; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2>Welcome! Your Account Has Been Created</h2>\r\n        <p>Hello {{fullName}},</p>\r\n        <p>Your account has been successfully created. Here are your login credentials:</p>\r\n        <div class=\"credentials\">\r\n            <p><strong>Username:</strong> <span class=\"code\">{{username}}</span></p>\r\n            <p><strong>Temporary Password:</strong> <span class=\"code\">{{temporaryPassword}}</span></p>\r\n        </div>\r\n        <div class=\"warning\">\r\n            <strong>Important Security Notice:</strong>\r\n            <ul>\r\n                <li>This is a temporary password that must be changed on your first login</li>\r\n                <li>Your account is currently locked and will be activated after you change your password</li>\r\n                <li>Never share your password with anyone</li>\r\n            </ul>\r\n        </div>\r\n        <p>Best regards,<br>Restaurant Management Team</p>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent to new staff members with their temporary credentials.", "Welcome! Your Account Has Been Created", "ACCOUNT_CREATED", "Account Created" },
                    { 3L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .details { background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #dee2e6; }\r\n        .highlight { color: #d97706; font-weight: bold; }\r\n        .footer { margin-top: 30px; font-size: 12px; color: #666; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2 style=\"color: #1A3A51;\">Reservation Confirmation</h2>\r\n        <p>Hello <span class=\"highlight\">{{CustomerName}}</span>,</p>\r\n        <p>Thank you for choosing An Lac Restaurant. We are pleased to confirm your reservation:</p>\r\n        <div class=\"details\">\r\n            <p><strong>Reservation ID:</strong> #{{ReservationId}}</p>\r\n            <p><strong>Date & Time:</strong> {{ReservedTime}}</p>\r\n            <p><strong>Party Size:</strong> {{PartySize}} people</p>\r\n            <p><strong>Table(s):</strong> {{TableCodes}}</p>\r\n            <p><strong>Zone:</strong> {{Zone}}</p>\r\n        </div>\r\n        <p>If you need to change or cancel your reservation, please contact us at least 2 hours in advance.</p>\r\n        <p>We look forward to serving you!</p>\r\n        <div class=\"footer\">\r\n            <p>An Lac Restaurant<br>123 Restaurant Street, City<br>Phone: (+84) 123-456-789</p>\r\n        </div>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent to customers after a successful online reservation.", "Reservation Confirmed - An Lac Restaurant", "RESERVATION_CONFIRM", "Reservation Confirmation" }
                });

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
                name: "audit_log_ibfk_1",
                table: "audit_log",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_time",
                table: "audit_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_session_user",
                table: "auth_session",
                columns: new[] { "user_id", "expires_at" });

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
                name: "phone",
                table: "customer",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "category_id",
                table: "dish",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "fk_dish_desc_text",
                table: "dish",
                column: "description_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_dish_name_text",
                table: "dish",
                column: "dish_name_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_dish_note_text",
                table: "dish",
                column: "note_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_dish_short_desc_text",
                table: "dish",
                column: "short_description_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_dish_slogan_text",
                table: "dish",
                column: "slogan_text_id");

            migrationBuilder.CreateIndex(
                name: "idx_dish_status_lv",
                table: "dish",
                column: "dish_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "fk_cat_name_text",
                table: "dish_category",
                column: "category_name_text_id");

            migrationBuilder.CreateIndex(
                name: "FK_dish_category_i18n_text_text_id",
                table: "dish_category",
                column: "description_text_id");

            migrationBuilder.CreateIndex(
                name: "media_id",
                table: "dish_media",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "FK_dish_tag_dish_dish_id",
                table: "dish_tag",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "FK_dish_tag_lookup_value_value_id",
                table: "dish_tag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_template_TemplateCode",
                table: "email_template",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_i18n_text_source_lang",
                table: "i18n_text",
                column: "source_lang_code");

            migrationBuilder.CreateIndex(
                name: "uq_i18n_text_key",
                table: "i18n_text",
                column: "text_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_i18n_tr_lang",
                table: "i18n_translation",
                column: "lang_code");

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_image_id",
                table: "ingredient",
                column: "image_id");

            migrationBuilder.CreateIndex(
                name: "fk_ingredient_name_text",
                table: "ingredient",
                column: "ingredient_name_text_id");

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_type_lv_id",
                table: "ingredient",
                column: "type_lv_id");

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_supplier_ingredient_ingredient_id",
                table: "ingredient_supplier",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "FK_ingredient_supplier_supplier_supplier_id",
                table: "ingredient_supplier",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "fk_inventory_transaction_staff",
                table: "inventory_transaction",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "FK_inventory_transaction_supplier_supplier_id",
                table: "inventory_transaction",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_transaction_dir_time",
                table: "inventory_transaction",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_tx_status_lv",
                table: "inventory_transaction",
                column: "status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_tx_type_lv",
                table: "inventory_transaction",
                column: "type_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_transaction_item_ingredient",
                table: "inventory_transaction_item",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_transaction_item_transaction",
                table: "inventory_transaction_item",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_transaction_item",
                table: "inventory_transaction_item",
                columns: new[] { "transaction_id", "ingredient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inventory_transaction_media_media",
                table: "inventory_transaction_media",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "idx_inventory_transaction_media_primary",
                table: "inventory_transaction_media",
                columns: new[] { "transaction_id", "is_primary" });

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
                name: "fk_lookup_type_desc_text",
                table: "lookup_type",
                column: "type_desc_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_lookup_type_name_text",
                table: "lookup_type",
                column: "type_name_text_id");

            migrationBuilder.CreateIndex(
                name: "uq_lookup_type_code",
                table: "lookup_type",
                column: "type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_lookup_value_desc_text",
                table: "lookup_value",
                column: "value_desc_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_lookup_value_name_text",
                table: "lookup_value",
                column: "value_name_text_id");

            migrationBuilder.CreateIndex(
                name: "idx_lookup_value_type_active",
                table: "lookup_value",
                columns: new[] { "type_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "uq_lookup_value",
                table: "lookup_value",
                columns: new[] { "type_id", "value_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_media_asset_type_lv",
                table: "media_asset",
                column: "media_type_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_notification_pref_user_id",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_pref_user_type",
                table: "notification_preferences",
                columns: new[] { "user_id", "notification_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_nrs_notification_id",
                table: "notification_read_states",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "idx_nrs_user_is_read",
                table: "notification_read_states",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_user",
                table: "notification_read_states",
                columns: new[] { "notification_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_created_at",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_type",
                table: "notifications",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "idx_order_coupon_coupon",
                table: "order_coupon",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "uq_order_coupon",
                table: "order_coupon",
                columns: new[] { "order_id", "coupon_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "dish_id",
                table: "order_item",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_item_status_lv",
                table: "order_item",
                column: "item_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "order_id",
                table: "order_item",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_invoice_promo_promo",
                table: "order_promotion",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "uq_invoice_promo",
                table: "order_promotion",
                columns: new[] { "order_id", "promotion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "FK_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_status",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_source_lv",
                table: "orders",
                column: "source_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status_lv",
                table: "orders",
                column: "order_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "orders_ibfk_2",
                table: "orders",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "table_id",
                table: "orders",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "idx_payment_method_lv",
                table: "payment",
                column: "method_lv_id");

            migrationBuilder.CreateIndex(
                name: "payment_ibfk_1",
                table: "payment",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "fk_promo_desc_text",
                table: "promotion",
                column: "promo_desc_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_promo_name_text",
                table: "promotion",
                column: "promo_name_text_id");

            migrationBuilder.CreateIndex(
                name: "idx_promotion_status_lv",
                table: "promotion",
                column: "promotion_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_promotion_type_lv",
                table: "promotion",
                column: "type_lv_id");

            migrationBuilder.CreateIndex(
                name: "promo_code",
                table: "promotion",
                column: "promo_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "promotion_id",
                table: "promotion_rule",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "required_category_id",
                table: "promotion_rule",
                column: "required_category_id");

            migrationBuilder.CreateIndex(
                name: "required_dish_id",
                table: "promotion_rule",
                column: "required_dish_id");

            migrationBuilder.CreateIndex(
                name: "category_id1",
                table: "promotion_target",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "dish_id1",
                table: "promotion_target",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "promotion_id1",
                table: "promotion_target",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "idx_recipe_ingredient",
                table: "recipe",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "customer_id",
                table: "reservation",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_reservation_source_lv",
                table: "reservation",
                column: "source_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_reservation_status_lv",
                table: "reservation",
                column: "reservation_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_reservation_time",
                table: "reservation",
                column: "reserved_time");

            migrationBuilder.CreateIndex(
                name: "table_id2",
                table: "reservation_table",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "FK_restaurant_table_table_qr_img",
                table: "restaurant_table",
                column: "table_qr_img");

            migrationBuilder.CreateIndex(
                name: "idx_restaurant_table_status_lv",
                table: "restaurant_table",
                column: "table_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_restaurant_table_type_lv",
                table: "restaurant_table",
                column: "table_type_lv_id");

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_table_updated_by_staff_id",
                table: "restaurant_table",
                column: "updated_by_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_restaurant_table_zone_lv_id",
                table: "restaurant_table",
                column: "zone_lv_id");

            migrationBuilder.CreateIndex(
                name: "table_code",
                table: "restaurant_table",
                column: "table_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_role_status_lv_id",
                table: "role",
                column: "role_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "role_code",
                table: "role",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "category_id2",
                table: "service_error",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_service_error_order",
                table: "service_error",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_service_error_severity_lv",
                table: "service_error",
                column: "severity_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_service_error_staff",
                table: "service_error",
                columns: new[] { "staff_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "order_item_id",
                table: "service_error",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "service_error_ibfk_7",
                table: "service_error",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "table_id1",
                table: "service_error",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "category_code",
                table: "service_error_category",
                column: "category_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_sec_desc_text",
                table: "service_error_category",
                column: "category_desc_text_id");

            migrationBuilder.CreateIndex(
                name: "fk_sec_name_text",
                table: "service_error_category",
                column: "category_name_text_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_staff",
                table: "shift_assignment",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_status_lv",
                table: "shift_assignment",
                column: "assignment_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_template",
                table: "shift_assignment",
                column: "shift_template_id");

            migrationBuilder.CreateIndex(
                name: "idx_shift_assignment_work_date",
                table: "shift_assignment",
                column: "work_date");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_assigned_by",
                table: "shift_assignment",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "uq_shift_assignment_template_date_staff",
                table: "shift_assignment",
                columns: new[] { "shift_template_id", "work_date", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shift_template_active",
                table: "shift_template",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_shift_template_created_by",
                table: "shift_template",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_shift_template_updated_by",
                table: "shift_template",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_shift_template_name",
                table: "shift_template",
                column: "template_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "email",
                table: "staff_account",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_staff_account_status_lv",
                table: "staff_account",
                column: "account_status_lv_id");

            migrationBuilder.CreateIndex(
                name: "role_id",
                table: "staff_account",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "username",
                table: "staff_account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_setting_updated_by",
                table: "system_setting",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_setting_key",
                table: "system_setting",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "media_id1",
                table: "table_media",
                column: "media_id");

            migrationBuilder.CreateIndex(
                name: "idx_time_log_attendance",
                table: "time_log",
                column: "attendance_record_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "current_stock");

            migrationBuilder.DropTable(
                name: "dish_media");

            migrationBuilder.DropTable(
                name: "dish_tag");

            migrationBuilder.DropTable(
                name: "email_template");

            migrationBuilder.DropTable(
                name: "i18n_translation");

            migrationBuilder.DropTable(
                name: "ingredient_supplier");

            migrationBuilder.DropTable(
                name: "inventory_transaction_item");

            migrationBuilder.DropTable(
                name: "inventory_transaction_media");

            migrationBuilder.DropTable(
                name: "login_activity");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_read_states");

            migrationBuilder.DropTable(
                name: "order_coupon");

            migrationBuilder.DropTable(
                name: "order_promotion");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "promotion_rule");

            migrationBuilder.DropTable(
                name: "promotion_target");

            migrationBuilder.DropTable(
                name: "recipe");

            migrationBuilder.DropTable(
                name: "reservation_table");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "service_error");

            migrationBuilder.DropTable(
                name: "system_setting");

            migrationBuilder.DropTable(
                name: "table_media");

            migrationBuilder.DropTable(
                name: "time_log");

            migrationBuilder.DropTable(
                name: "inventory_transaction");

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "coupon");

            migrationBuilder.DropTable(
                name: "promotion");

            migrationBuilder.DropTable(
                name: "ingredient");

            migrationBuilder.DropTable(
                name: "reservation");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "order_item");

            migrationBuilder.DropTable(
                name: "service_error_category");

            migrationBuilder.DropTable(
                name: "attendance_record");

            migrationBuilder.DropTable(
                name: "supplier");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "dish");

            migrationBuilder.DropTable(
                name: "shift_assignment");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "restaurant_table");

            migrationBuilder.DropTable(
                name: "dish_category");

            migrationBuilder.DropTable(
                name: "shift_template");

            migrationBuilder.DropTable(
                name: "media_asset");

            migrationBuilder.DropTable(
                name: "staff_account");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "lookup_value");

            migrationBuilder.DropTable(
                name: "lookup_type");

            migrationBuilder.DropTable(
                name: "i18n_text");

            migrationBuilder.DropTable(
                name: "i18n_language");
        }
    }
}
