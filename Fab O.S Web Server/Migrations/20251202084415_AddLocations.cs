using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Equipment_Location",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "EquipmentKits",
                newName: "LocationLegacy");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Equipment",
                newName: "LocationLegacy");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "EquipmentKits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Equipment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_LocationId",
                table: "EquipmentKits",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_LocationId",
                table: "Equipment",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CompanyId_IsDeleted",
                table: "Locations",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CompanyId_LocationCode",
                table: "Locations",
                columns: new[] { "CompanyId", "LocationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_IsActive",
                table: "Locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Type",
                table: "Locations",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Locations_LocationId",
                table: "Equipment",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentKits_Locations_LocationId",
                table: "EquipmentKits",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Locations_LocationId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentKits_Locations_LocationId",
                table: "EquipmentKits");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentKits_LocationId",
                table: "EquipmentKits");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_LocationId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "EquipmentKits");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "LocationLegacy",
                table: "EquipmentKits",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "LocationLegacy",
                table: "Equipment",
                newName: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Location",
                table: "Equipment",
                column: "Location");
        }
    }
}
