using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSurfaceCoatingsAndMeasurementDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "TraceTakeoffMeasurements",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "getutcdate()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "TraceTakeoffMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "TraceTakeoffMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "TraceTakeoffMeasurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TraceTakeoffMeasurements",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TraceTakeoffMeasurements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SurfaceCoatingId",
                table: "TraceTakeoffMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurfaceCoatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CoatingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CoatingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurfaceCoatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurfaceCoatings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_CreatedBy",
                table: "TraceTakeoffMeasurements",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_ModifiedBy",
                table: "TraceTakeoffMeasurements",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_SurfaceCoatingId",
                table: "TraceTakeoffMeasurements",
                column: "SurfaceCoatingId");

            migrationBuilder.CreateIndex(
                name: "IX_SurfaceCoatings_CompanyId_CoatingCode",
                table: "SurfaceCoatings",
                columns: new[] { "CompanyId", "CoatingCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurfaceCoatings_DisplayOrder",
                table: "SurfaceCoatings",
                column: "DisplayOrder");

            // Seed common surface coatings for Company 1 (default company)
            migrationBuilder.InsertData(
                table: "SurfaceCoatings",
                columns: new[] { "CompanyId", "CoatingCode", "CoatingName", "Description", "IsActive", "DisplayOrder" },
                values: new object[,]
                {
                    { 1, "NONE", "None", "No surface coating", true, 0 },
                    { 1, "HDG", "Hot-Dip Galvanized", "Z275 - Hot-dip galvanized zinc coating (275 g/m²)", true, 1 },
                    { 1, "CR", "Cold Rolled", "Bare cold rolled steel with no coating", true, 2 },
                    { 1, "PAINTED", "Painted", "Factory-applied paint coating", true, 3 },
                    { 1, "POWDER", "Powder Coated", "Electrostatic powder coating", true, 4 },
                    { 1, "STAINLESS", "Stainless Steel", "Corrosion-resistant stainless steel alloy", true, 5 },
                    { 1, "ZINC", "Zinc Plated", "Electro-galvanized zinc plating", true, 6 }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_TraceTakeoffMeasurements_SurfaceCoatings_SurfaceCoatingId",
                table: "TraceTakeoffMeasurements",
                column: "SurfaceCoatingId",
                principalTable: "SurfaceCoatings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceTakeoffMeasurements_Users_CreatedBy",
                table: "TraceTakeoffMeasurements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceTakeoffMeasurements_Users_ModifiedBy",
                table: "TraceTakeoffMeasurements",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TraceTakeoffMeasurements_SurfaceCoatings_SurfaceCoatingId",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceTakeoffMeasurements_Users_CreatedBy",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceTakeoffMeasurements_Users_ModifiedBy",
                table: "TraceTakeoffMeasurements");

            // Remove seed data before dropping table
            migrationBuilder.DeleteData(
                table: "SurfaceCoatings",
                keyColumn: "CoatingCode",
                keyValues: new object[] { "NONE", "HDG", "CR", "PAINTED", "POWDER", "STAINLESS", "ZINC" });

            migrationBuilder.DropTable(
                name: "SurfaceCoatings");

            migrationBuilder.DropIndex(
                name: "IX_TraceTakeoffMeasurements_CreatedBy",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_TraceTakeoffMeasurements_ModifiedBy",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_TraceTakeoffMeasurements_SurfaceCoatingId",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.DropColumn(
                name: "SurfaceCoatingId",
                table: "TraceTakeoffMeasurements");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "TraceTakeoffMeasurements",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "getutcdate()");
        }
    }
}
