using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFormDesignerEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FormTemplates - Page Margins
            migrationBuilder.AddColumn<decimal>(
                name: "MarginTopMm",
                table: "FormTemplates",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginRightMm",
                table: "FormTemplates",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 15m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginBottomMm",
                table: "FormTemplates",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginLeftMm",
                table: "FormTemplates",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 15m);

            // FormTemplateSections - Individual Padding
            migrationBuilder.AddColumn<int>(
                name: "PaddingTop",
                table: "FormTemplateSections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingRight",
                table: "FormTemplateSections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingBottom",
                table: "FormTemplateSections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingLeft",
                table: "FormTemplateSections",
                type: "int",
                nullable: true);

            // FormTemplateSections - Content Alignment
            migrationBuilder.AddColumn<string>(
                name: "ContentAlignHorizontal",
                table: "FormTemplateSections",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentAlignVertical",
                table: "FormTemplateSections",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // FormTemplateFields - Padding
            migrationBuilder.AddColumn<int>(
                name: "PaddingTop",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingRight",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingBottom",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaddingLeft",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            // FormTemplateFields - Margin
            migrationBuilder.AddColumn<int>(
                name: "MarginTop",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarginBottom",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            // FormTemplateFields - Layout
            migrationBuilder.AddColumn<string>(
                name: "TextAlign",
                table: "FormTemplateFields",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixedHeight",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FormTemplates - Page Margins
            migrationBuilder.DropColumn(
                name: "MarginTopMm",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "MarginRightMm",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "MarginBottomMm",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "MarginLeftMm",
                table: "FormTemplates");

            // FormTemplateSections - Individual Padding
            migrationBuilder.DropColumn(
                name: "PaddingTop",
                table: "FormTemplateSections");

            migrationBuilder.DropColumn(
                name: "PaddingRight",
                table: "FormTemplateSections");

            migrationBuilder.DropColumn(
                name: "PaddingBottom",
                table: "FormTemplateSections");

            migrationBuilder.DropColumn(
                name: "PaddingLeft",
                table: "FormTemplateSections");

            // FormTemplateSections - Content Alignment
            migrationBuilder.DropColumn(
                name: "ContentAlignHorizontal",
                table: "FormTemplateSections");

            migrationBuilder.DropColumn(
                name: "ContentAlignVertical",
                table: "FormTemplateSections");

            // FormTemplateFields - Padding
            migrationBuilder.DropColumn(
                name: "PaddingTop",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "PaddingRight",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "PaddingBottom",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "PaddingLeft",
                table: "FormTemplateFields");

            // FormTemplateFields - Margin
            migrationBuilder.DropColumn(
                name: "MarginTop",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "MarginBottom",
                table: "FormTemplateFields");

            // FormTemplateFields - Layout
            migrationBuilder.DropColumn(
                name: "TextAlign",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "FixedHeight",
                table: "FormTemplateFields");
        }
    }
}
