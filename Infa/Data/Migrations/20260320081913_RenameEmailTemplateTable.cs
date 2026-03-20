using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailTemplateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailTemplates",
                table: "EmailTemplates");

            migrationBuilder.RenameTable(
                name: "EmailTemplates",
                newName: "email_template");

            migrationBuilder.RenameIndex(
                name: "IX_EmailTemplates_TemplateCode",
                table: "email_template",
                newName: "IX_email_template_TemplateCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_template",
                table: "email_template",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_email_template",
                table: "email_template");

            migrationBuilder.RenameTable(
                name: "email_template",
                newName: "EmailTemplates");

            migrationBuilder.RenameIndex(
                name: "IX_email_template_TemplateCode",
                table: "EmailTemplates",
                newName: "IX_EmailTemplates_TemplateCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailTemplates",
                table: "EmailTemplates",
                column: "TemplateId");
        }
    }
}
