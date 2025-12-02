using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class RenameToEntriesAndAddGeometryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // COMMENTED OUT - These tables don't exist yet (QDocs module not created)
            // migrationBuilder.DropForeignKey(
            //     name: "FK_ITPAssemblies_WorkOrderAssembly_WorkOrderAssemblyId",
            //     table: "ITPAssemblies");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_MaterialTraceability_WorkOrderInventoryItem_WorkOrderInventoryItemId",
            //     table: "MaterialTraceability");

            // COMMENTED OUT - These tables don't exist yet (old names never created)
            // migrationBuilder.DropTable(
            //     name: "WorkOrderAssembly");

            // migrationBuilder.DropTable(
            //     name: "WorkOrderInventoryItem");

            // migrationBuilder.DropTable(
            //     name: "WorkOrderResources");

            // COMMENTED OUT - WorkCenters already has Code and Name columns (never had WorkCenterCode/WorkCenterName)
            // migrationBuilder.RenameColumn(
            //     name: "WorkCenterName",
            //     table: "WorkCenters",
            //     newName: "Name");

            // migrationBuilder.RenameColumn(
            //     name: "WorkCenterCode",
            //     table: "WorkCenters",
            //     newName: "Code");

            // migrationBuilder.RenameIndex(
            //     name: "IX_WorkCenters_WorkCenterCode",
            //     table: "WorkCenters",
            //     newName: "IX_WorkCenters_Code");

            // COMMENTED OUT - MaterialTraceability table doesn't exist yet (QDocs module not created)
            // migrationBuilder.RenameColumn(
            //     name: "WorkOrderInventoryItemId",
            //     table: "MaterialTraceability",
            //     newName: "WorkOrderMaterialEntryId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_MaterialTraceability_WorkOrderInventoryItemId",
            //     table: "MaterialTraceability",
            //     newName: "IX_MaterialTraceability_WorkOrderMaterialEntryId");

            // COMMENTED OUT - ITPAssemblies table doesn't exist yet (QDocs module not created)
            // migrationBuilder.RenameColumn(
            //     name: "WorkOrderAssemblyId",
            //     table: "ITPAssemblies",
            //     newName: "WorkOrderAssemblyEntryId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_ITPAssemblies_WorkOrderAssemblyId",
            //     table: "ITPAssemblies",
            //     newName: "IX_ITPAssemblies_WorkOrderAssemblyEntryId");

            migrationBuilder.AddColumn<decimal>(
                name: "Diameter",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlangeThickness",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlangeWidth",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LegA",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LegB",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutsideDiameter",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Thickness",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WallThickness",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WebDepth",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WebThickness",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: true);

            // COMMENTED OUT - DrawingParts table doesn't exist yet (QDocs module not created)
            // Will be added when QDocs tables are created
            // migrationBuilder.AddColumn<decimal>(
            //     name: "Diameter",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "FlangeThickness",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "FlangeWidth",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "LegA",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "LegB",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "Length",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "OutsideDiameter",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "Thickness",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "WallThickness",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "WebDepth",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "WebThickness",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "Width",
            //     table: "DrawingParts",
            //     type: "decimal(18,2)",
            //     nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkOrderAssemblyEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    PackageAssemblyId = table.Column<int>(type: "int", nullable: false),
                    AssemblyId = table.Column<int>(type: "int", nullable: false),
                    QuantityToBuild = table.Column<int>(type: "int", nullable: false),
                    QuantityCompleted = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderAssemblyEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssemblyEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderMaterialEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    PackageItemId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProcessedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequiredOperations = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeatNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Certificate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InventoryItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderMaterialEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterialEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderResourceEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderResourceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderResourceEntries_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrderResourceEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssemblyEntries_WorkOrderId",
                table: "WorkOrderAssemblyEntries",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterialEntries_WorkOrderId",
                table: "WorkOrderMaterialEntries",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResourceEntries_ResourceId",
                table: "WorkOrderResourceEntries",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResourceEntries_WorkOrderId",
                table: "WorkOrderResourceEntries",
                column: "WorkOrderId");

            // COMMENTED OUT - ITPAssemblies table doesn't exist yet (QDocs module not created)
            // Will be added when QDocs tables are created
            // migrationBuilder.AddForeignKey(
            //     name: "FK_ITPAssemblies_WorkOrderAssemblyEntries_WorkOrderAssemblyEntryId",
            //     table: "ITPAssemblies",
            //     column: "WorkOrderAssemblyEntryId",
            //     principalTable: "WorkOrderAssemblyEntries",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);

            // COMMENTED OUT - MaterialTraceability table doesn't exist yet (QDocs module not created)
            // Will be added when QDocs tables are created
            // migrationBuilder.AddForeignKey(
            //     name: "FK_MaterialTraceability_WorkOrderMaterialEntries_WorkOrderMaterialEntryId",
            //     table: "MaterialTraceability",
            //     column: "WorkOrderMaterialEntryId",
            //     principalTable: "WorkOrderMaterialEntries",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // COMMENTED OUT - These tables don't exist yet (QDocs module not created)
            // migrationBuilder.DropForeignKey(
            //     name: "FK_ITPAssemblies_WorkOrderAssemblyEntries_WorkOrderAssemblyEntryId",
            //     table: "ITPAssemblies");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_MaterialTraceability_WorkOrderMaterialEntries_WorkOrderMaterialEntryId",
            //     table: "MaterialTraceability");

            migrationBuilder.DropTable(
                name: "WorkOrderAssemblyEntries");

            migrationBuilder.DropTable(
                name: "WorkOrderMaterialEntries");

            migrationBuilder.DropTable(
                name: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "Diameter",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "FlangeThickness",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "FlangeWidth",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "LegA",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "LegB",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "OutsideDiameter",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Thickness",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "WallThickness",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "WebDepth",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "WebThickness",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "InventoryItems");

            // COMMENTED OUT - DrawingParts table doesn't exist yet (QDocs module not created)
            // migrationBuilder.DropColumn(
            //     name: "Diameter",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "FlangeThickness",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "FlangeWidth",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "LegA",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "LegB",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "Length",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "OutsideDiameter",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "Thickness",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "WallThickness",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "WebDepth",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "WebThickness",
            //     table: "DrawingParts");

            // migrationBuilder.DropColumn(
            //     name: "Width",
            //     table: "DrawingParts");

            // COMMENTED OUT - WorkCenters already has Code and Name columns (never had WorkCenterCode/WorkCenterName)
            // migrationBuilder.RenameColumn(
            //     name: "Name",
            //     table: "WorkCenters",
            //     newName: "WorkCenterName");

            // migrationBuilder.RenameColumn(
            //     name: "Code",
            //     table: "WorkCenters",
            //     newName: "WorkCenterCode");

            // migrationBuilder.RenameIndex(
            //     name: "IX_WorkCenters_Code",
            //     table: "WorkCenters",
            //     newName: "IX_WorkCenters_WorkCenterCode");

            // COMMENTED OUT - MaterialTraceability table doesn't exist yet (QDocs module not created)
            // migrationBuilder.RenameColumn(
            //     name: "WorkOrderMaterialEntryId",
            //     table: "MaterialTraceability",
            //     newName: "WorkOrderInventoryItemId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_MaterialTraceability_WorkOrderMaterialEntryId",
            //     table: "MaterialTraceability",
            //     newName: "IX_MaterialTraceability_WorkOrderInventoryItemId");

            // COMMENTED OUT - ITPAssemblies table doesn't exist yet (QDocs module not created)
            // migrationBuilder.RenameColumn(
            //     name: "WorkOrderAssemblyEntryId",
            //     table: "ITPAssemblies",
            //     newName: "WorkOrderAssemblyId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_ITPAssemblies_WorkOrderAssemblyEntryId",
            //     table: "ITPAssemblies",
            //     newName: "IX_ITPAssemblies_WorkOrderAssemblyId");

            migrationBuilder.CreateTable(
                name: "WorkOrderAssembly",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    AssemblyId = table.Column<int>(type: "int", nullable: false),
                    PackageAssemblyId = table.Column<int>(type: "int", nullable: false),
                    QuantityCompleted = table.Column<int>(type: "int", nullable: false),
                    QuantityToBuild = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderAssembly", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssembly_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderInventoryItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: false),
                    Certificate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HeatNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InventoryItemId = table.Column<int>(type: "int", nullable: true),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PackageItemId = table.Column<int>(type: "int", nullable: false),
                    ProcessedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RequiredOperations = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderInventoryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderInventoryItem_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrderResources_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssembly_WorkOrderId",
                table: "WorkOrderAssembly",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderInventoryItem_WorkOrderId",
                table: "WorkOrderInventoryItem",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResources_ResourceId",
                table: "WorkOrderResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResources_WorkOrderId",
                table: "WorkOrderResources",
                column: "WorkOrderId");

            // COMMENTED OUT - These tables don't exist yet (QDocs module not created)
            // migrationBuilder.AddForeignKey(
            //     name: "FK_ITPAssemblies_WorkOrderAssembly_WorkOrderAssemblyId",
            //     table: "ITPAssemblies",
            //     column: "WorkOrderAssemblyId",
            //     principalTable: "WorkOrderAssembly",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_MaterialTraceability_WorkOrderInventoryItem_WorkOrderInventoryItemId",
            //     table: "MaterialTraceability",
            //     column: "WorkOrderInventoryItemId",
            //     principalTable: "WorkOrderInventoryItem",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);
        }
    }
}
