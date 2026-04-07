using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "customer_id",
                table: "coupon",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_coupon_customer_id",
                table: "coupon",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_coupon_customer",
                table: "coupon",
                column: "customer_id",
                principalTable: "customer",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_coupon_customer",
                table: "coupon");

            migrationBuilder.DropIndex(
                name: "idx_coupon_customer_id",
                table: "coupon");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "coupon");
        }
    }
}
