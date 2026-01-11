using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFormTemplateSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColumnIndex",
                table: "FormTemplateFields",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormTemplateSectionId",
                table: "FormTemplateFields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowIndex",
                table: "FormTemplateFields",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FormTemplateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormTemplateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LayoutType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PageBreakBefore = table.Column<bool>(type: "bit", nullable: false),
                    KeepTogether = table.Column<bool>(type: "bit", nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BorderColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeaderBackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeaderTextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BorderWidth = table.Column<int>(type: "int", nullable: true),
                    BorderRadius = table.Column<int>(type: "int", nullable: true),
                    Padding = table.Column<int>(type: "int", nullable: true),
                    IsCollapsible = table.Column<bool>(type: "bit", nullable: false),
                    IsCollapsedByDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplateSections_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormTemplateSections_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormTemplateSections_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateFields_FormTemplateSectionId",
                table: "FormTemplateFields",
                column: "FormTemplateSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateSections_CreatedByUserId",
                table: "FormTemplateSections",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateSections_FormTemplateId",
                table: "FormTemplateSections",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateSections_ModifiedByUserId",
                table: "FormTemplateSections",
                column: "ModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormTemplateFields_FormTemplateSections_FormTemplateSectionId",
                table: "FormTemplateFields",
                column: "FormTemplateSectionId",
                principalTable: "FormTemplateSections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormTemplateFields_FormTemplateSections_FormTemplateSectionId",
                table: "FormTemplateFields");

            migrationBuilder.DropTable(
                name: "FormTemplateSections");

            migrationBuilder.DropIndex(
                name: "IX_FormTemplateFields_FormTemplateSectionId",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "ColumnIndex",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "FormTemplateSectionId",
                table: "FormTemplateFields");

            migrationBuilder.DropColumn(
                name: "RowIndex",
                table: "FormTemplateFields");
        }
    }
}
