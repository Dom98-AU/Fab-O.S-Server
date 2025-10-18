using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfCalibrationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Commented out - these constraints don't exist in database
            // migrationBuilder.DropForeignKey(
            //     name: "FK_Projects_Orders_OrderId",
            //     table: "Projects");

            // migrationBuilder.DropIndex(
            //     name: "IX_Projects_OrderId",
            //     table: "Projects");

            // migrationBuilder.DropColumn(
            //     name: "OrderId",
            //     table: "Projects");

            // migrationBuilder.DropColumn(
            //     name: "ProjectType",
            //     table: "Projects");

            // Commented out - these columns already exist in the Customers table
            // migrationBuilder.AddColumn<int>(
            //     name: "CompanyId",
            //     table: "Customers",
            //     type: "int",
            //     nullable: false,
            //     defaultValue: 0);

            // migrationBuilder.AddColumn<string>(
            //     name: "CompanyName",
            //     table: "Customers",
            //     type: "nvarchar(200)",
            //     maxLength: 200,
            //     nullable: false,
            //     defaultValue: "");

            // migrationBuilder.AddColumn<int>(
            //     name: "CreatedById",
            //     table: "Customers",
            //     type: "int",
            //     nullable: true);

            migrationBuilder.CreateTable(
                name: "PdfAnnotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageDrawingId = table.Column<int>(type: "int", nullable: false),
                    AnnotationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnnotationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageIndex = table.Column<int>(type: "int", nullable: false),
                    InstantJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMeasurement = table.Column<bool>(type: "bit", nullable: false),
                    IsCalibration = table.Column<bool>(type: "bit", nullable: false),
                    TraceTakeoffMeasurementId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfAnnotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfAnnotations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdfAnnotations_PackageDrawings_PackageDrawingId",
                        column: x => x.PackageDrawingId,
                        principalTable: "PackageDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdfAnnotations_TraceTakeoffMeasurements_TraceTakeoffMeasurementId",
                        column: x => x.TraceTakeoffMeasurementId,
                        principalTable: "TraceTakeoffMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PdfAnnotations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PdfScaleCalibrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageDrawingId = table.Column<int>(type: "int", nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    KnownDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MeasuredDistance = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PageIndex = table.Column<int>(type: "int", nullable: false),
                    CalibrationLineStart = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CalibrationLineEnd = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfScaleCalibrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfScaleCalibrations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdfScaleCalibrations_PackageDrawings_PackageDrawingId",
                        column: x => x.PackageDrawingId,
                        principalTable: "PackageDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdfScaleCalibrations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfAnnotations_CompanyId",
                table: "PdfAnnotations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfAnnotations_CreatedByUserId",
                table: "PdfAnnotations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfAnnotations_PackageDrawingId",
                table: "PdfAnnotations",
                column: "PackageDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfAnnotations_PackageDrawingId_AnnotationId",
                table: "PdfAnnotations",
                columns: new[] { "PackageDrawingId", "AnnotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PdfAnnotations_TraceTakeoffMeasurementId",
                table: "PdfAnnotations",
                column: "TraceTakeoffMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfScaleCalibrations_CompanyId",
                table: "PdfScaleCalibrations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfScaleCalibrations_CreatedByUserId",
                table: "PdfScaleCalibrations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfScaleCalibrations_PackageDrawingId",
                table: "PdfScaleCalibrations",
                column: "PackageDrawingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfAnnotations");

            migrationBuilder.DropTable(
                name: "PdfScaleCalibrations");

            // Commented out - these columns were not added in Up(), so don't drop them
            // migrationBuilder.DropColumn(
            //     name: "CompanyId",
            //     table: "Customers");

            // migrationBuilder.DropColumn(
            //     name: "CompanyName",
            //     table: "Customers");

            // migrationBuilder.DropColumn(
            //     name: "CreatedById",
            //     table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectType",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrderId",
                table: "Projects",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Orders_OrderId",
                table: "Projects",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
