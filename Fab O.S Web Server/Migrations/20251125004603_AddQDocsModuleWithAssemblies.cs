using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddQDocsModuleWithAssemblies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssemblyMark",
                table: "DrawingParts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssemblyName",
                table: "DrawingParts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Coating",
                table: "DrawingParts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaintedArea",
                table: "DrawingParts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentAssemblyId",
                table: "DrawingParts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "DrawingParts",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "DrawingParts",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DrawingAssemblies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrawingId = table.Column<int>(type: "int", nullable: false),
                    DrawingRevisionId = table.Column<int>(type: "int", nullable: true),
                    AssemblyMark = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssemblyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PartCount = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingAssemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrawingAssemblies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrawingAssemblies_DrawingRevisions_DrawingRevisionId",
                        column: x => x.DrawingRevisionId,
                        principalTable: "DrawingRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrawingAssemblies_QDocsDrawings_DrawingId",
                        column: x => x.DrawingId,
                        principalTable: "QDocsDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParts_AssemblyMark",
                table: "DrawingParts",
                column: "AssemblyMark");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParts_ParentAssemblyId",
                table: "DrawingParts",
                column: "ParentAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssemblies_AssemblyMark",
                table: "DrawingAssemblies",
                column: "AssemblyMark");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssemblies_CompanyId",
                table: "DrawingAssemblies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssemblies_DrawingId",
                table: "DrawingAssemblies",
                column: "DrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssemblies_DrawingId_AssemblyMark",
                table: "DrawingAssemblies",
                columns: new[] { "DrawingId", "AssemblyMark" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssemblies_DrawingRevisionId",
                table: "DrawingAssemblies",
                column: "DrawingRevisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrawingParts_DrawingAssemblies_ParentAssemblyId",
                table: "DrawingParts",
                column: "ParentAssemblyId",
                principalTable: "DrawingAssemblies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawingParts_DrawingAssemblies_ParentAssemblyId",
                table: "DrawingParts");

            migrationBuilder.DropTable(
                name: "DrawingAssemblies");

            migrationBuilder.DropIndex(
                name: "IX_DrawingParts_AssemblyMark",
                table: "DrawingParts");

            migrationBuilder.DropIndex(
                name: "IX_DrawingParts_ParentAssemblyId",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "AssemblyMark",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "AssemblyName",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "Coating",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "PaintedArea",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "ParentAssemblyId",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "DrawingParts");
        }
    }
}
