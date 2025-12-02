using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Routings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routings_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Routings_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutingLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutingId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    WorkCenterId = table.Column<int>(type: "int", nullable: true),
                    ResourceId = table.Column<int>(type: "int", nullable: true),
                    ResourceGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetailedInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SetupTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RunTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RunTimeUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    WaitTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MoveTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NextOperationSequence = table.Column<int>(type: "int", nullable: true),
                    SendAheadQuantity = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RequiresInspection = table.Column<bool>(type: "bit", nullable: false),
                    InspectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingLines_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutingLines_Routings_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "Routings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutingLines_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderRoutings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    SourceRoutingId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderRoutings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutings_Routings_SourceRoutingId",
                        column: x => x.SourceRoutingId,
                        principalTable: "Routings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutings_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderRoutingLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderRoutingId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    WorkCenterId = table.Column<int>(type: "int", nullable: true),
                    AssignedResourceId = table.Column<int>(type: "int", nullable: true),
                    ResourceGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetailedInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PlannedSetupTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PlannedRunTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RunTimeUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PlannedWaitTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PlannedMoveTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualSetupTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualRunTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualWaitTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualMoveTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    QuantityToProcess = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    QuantityProcessed = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RequiresInspection = table.Column<bool>(type: "bit", nullable: false),
                    InspectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InspectionResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByResourceId = table.Column<int>(type: "int", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderRoutingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutingLines_Resources_AssignedResourceId",
                        column: x => x.AssignedResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutingLines_Resources_CompletedByResourceId",
                        column: x => x.CompletedByResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutingLines_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderRoutingLines_WorkOrderRoutings_WorkOrderRoutingId",
                        column: x => x.WorkOrderRoutingId,
                        principalTable: "WorkOrderRoutings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingLines_ResourceId",
                table: "RoutingLines",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingLines_RoutingId",
                table: "RoutingLines",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingLines_WorkCenterId",
                table: "RoutingLines",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Routings_CompanyId",
                table: "Routings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Routings_CreatedBy",
                table: "Routings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Routings_LastModifiedBy",
                table: "Routings",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutingLines_AssignedResourceId",
                table: "WorkOrderRoutingLines",
                column: "AssignedResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutingLines_CompletedByResourceId",
                table: "WorkOrderRoutingLines",
                column: "CompletedByResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutingLines_WorkCenterId",
                table: "WorkOrderRoutingLines",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutingLines_WorkOrderRoutingId",
                table: "WorkOrderRoutingLines",
                column: "WorkOrderRoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutings_SourceRoutingId",
                table: "WorkOrderRoutings",
                column: "SourceRoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderRoutings_WorkOrderId",
                table: "WorkOrderRoutings",
                column: "WorkOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutingLines");

            migrationBuilder.DropTable(
                name: "WorkOrderRoutingLines");

            migrationBuilder.DropTable(
                name: "WorkOrderRoutings");

            migrationBuilder.DropTable(
                name: "Routings");
        }
    }
}
