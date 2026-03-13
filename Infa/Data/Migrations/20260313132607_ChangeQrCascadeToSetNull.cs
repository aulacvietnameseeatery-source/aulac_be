using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeQrCascadeToSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_restaurant_table_table_qr_img",
                table: "restaurant_table");

            migrationBuilder.AlterColumn<long>(
                name: "ingredient_supplier_id",
                table: "ingredient_supplier",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_restaurant_table_table_qr_img",
                table: "restaurant_table",
                column: "table_qr_img",
                principalTable: "media_asset",
                principalColumn: "media_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_restaurant_table_table_qr_img",
                table: "restaurant_table");

            migrationBuilder.AlterColumn<long>(
                name: "ingredient_supplier_id",
                table: "ingredient_supplier",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_restaurant_table_table_qr_img",
                table: "restaurant_table",
                column: "table_qr_img",
                principalTable: "media_asset",
                principalColumn: "media_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
