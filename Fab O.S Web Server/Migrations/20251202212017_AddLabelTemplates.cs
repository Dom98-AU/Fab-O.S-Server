using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WidthMm = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    HeightMm = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    IncludeQRCode = table.Column<bool>(type: "bit", nullable: false),
                    QRCodePixelsPerModule = table.Column<int>(type: "int", nullable: false),
                    IncludeCode = table.Column<bool>(type: "bit", nullable: false),
                    IncludeName = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCategory = table.Column<bool>(type: "bit", nullable: false),
                    IncludeLocation = table.Column<bool>(type: "bit", nullable: false),
                    IncludeSerialNumber = table.Column<bool>(type: "bit", nullable: false),
                    IncludeServiceDate = table.Column<bool>(type: "bit", nullable: false),
                    IncludeContactInfo = table.Column<bool>(type: "bit", nullable: false),
                    PrimaryFontSize = table.Column<int>(type: "int", nullable: false),
                    SecondaryFontSize = table.Column<int>(type: "int", nullable: false),
                    MarginMm = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelTemplates_CompanyId_EntityType_IsDefault",
                table: "LabelTemplates",
                columns: new[] { "CompanyId", "EntityType", "IsDefault" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabelTemplates_CompanyId_Name",
                table: "LabelTemplates",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabelTemplates_IsSystemTemplate",
                table: "LabelTemplates",
                column: "IsSystemTemplate");

            // Seed system templates
            migrationBuilder.InsertData(
                table: "LabelTemplates",
                columns: new[] { "Name", "Description", "EntityType", "WidthMm", "HeightMm", "IncludeQRCode", "QRCodePixelsPerModule", "IncludeCode", "IncludeName", "IncludeCategory", "IncludeLocation", "IncludeSerialNumber", "IncludeServiceDate", "IncludeContactInfo", "PrimaryFontSize", "SecondaryFontSize", "MarginMm", "IsSystemTemplate", "IsDefault" },
                values: new object[] { "Small", "Small label (30mm x 15mm) - QR code and name only", "All", 30m, 15m, true, 8, true, true, false, false, false, false, false, 6, 5, 2m, true, false });

            migrationBuilder.InsertData(
                table: "LabelTemplates",
                columns: new[] { "Name", "Description", "EntityType", "WidthMm", "HeightMm", "IncludeQRCode", "QRCodePixelsPerModule", "IncludeCode", "IncludeName", "IncludeCategory", "IncludeLocation", "IncludeSerialNumber", "IncludeServiceDate", "IncludeContactInfo", "PrimaryFontSize", "SecondaryFontSize", "MarginMm", "IsSystemTemplate", "IsDefault" },
                values: new object[] { "Standard", "Standard label (50mm x 25mm) - QR code, name, category, location", "All", 50m, 25m, true, 10, true, true, true, true, false, false, false, 8, 6, 2m, true, true });

            migrationBuilder.InsertData(
                table: "LabelTemplates",
                columns: new[] { "Name", "Description", "EntityType", "WidthMm", "HeightMm", "IncludeQRCode", "QRCodePixelsPerModule", "IncludeCode", "IncludeName", "IncludeCategory", "IncludeLocation", "IncludeSerialNumber", "IncludeServiceDate", "IncludeContactInfo", "PrimaryFontSize", "SecondaryFontSize", "MarginMm", "IsSystemTemplate", "IsDefault" },
                values: new object[] { "Large", "Large label (100mm x 50mm) - All fields visible", "All", 100m, 50m, true, 15, true, true, true, true, true, true, true, 10, 8, 3m, true, false });

            migrationBuilder.InsertData(
                table: "LabelTemplates",
                columns: new[] { "Name", "Description", "EntityType", "WidthMm", "HeightMm", "IncludeQRCode", "QRCodePixelsPerModule", "IncludeCode", "IncludeName", "IncludeCategory", "IncludeLocation", "IncludeSerialNumber", "IncludeServiceDate", "IncludeContactInfo", "PrimaryFontSize", "SecondaryFontSize", "MarginMm", "IsSystemTemplate", "IsDefault" },
                values: new object[] { "Inventory", "Inventory tag label (75mm x 35mm)", "All", 75m, 35m, true, 12, true, true, true, true, false, false, false, 9, 7, 2m, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelTemplates");
        }
    }
}
