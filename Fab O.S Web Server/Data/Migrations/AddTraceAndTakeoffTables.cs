using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace FabOS.WebServer.Data.Migrations
{
    public partial class AddTraceAndTakeoffTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create TraceRecords table
            migrationBuilder.CreateTable(
                name: "TraceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceId = table.Column<Guid>(nullable: false),
                    TraceNumber = table.Column<string>(maxLength: 50, nullable: false),
                    EntityType = table.Column<int>(nullable: false),
                    EntityId = table.Column<int>(nullable: false),
                    EntityReference = table.Column<string>(maxLength: 100, nullable: true),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    ParentTraceId = table.Column<Guid>(nullable: true),
                    CaptureDateTime = table.Column<DateTime>(nullable: false),
                    EventDateTime = table.Column<DateTime>(nullable: true),
                    UserId = table.Column<int>(nullable: true),
                    OperatorName = table.Column<string>(maxLength: 100, nullable: true),
                    WorkCenterId = table.Column<int>(nullable: true),
                    Location = table.Column<string>(maxLength: 100, nullable: true),
                    MachineId = table.Column<string>(maxLength: 50, nullable: true),
                    Status = table.Column<int>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CompanyId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraceRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraceRecords_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceMaterials table
            migrationBuilder.CreateTable(
                name: "TraceMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(nullable: false),
                    CatalogueItemId = table.Column<int>(nullable: true),
                    MaterialCode = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    HeatNumber = table.Column<string>(maxLength: 50, nullable: true),
                    BatchNumber = table.Column<string>(maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(maxLength: 50, nullable: true),
                    Supplier = table.Column<string>(maxLength: 100, nullable: true),
                    SupplierBatch = table.Column<string>(maxLength: 50, nullable: true),
                    MillCertificate = table.Column<string>(maxLength: 100, nullable: true),
                    CertType = table.Column<int>(nullable: true),
                    CertDate = table.Column<DateTime>(nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ChemicalComposition = table.Column<string>(maxLength: 500, nullable: true),
                    MechanicalProperties = table.Column<string>(maxLength: 500, nullable: true),
                    TestResults = table.Column<string>(maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceMaterials_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceMaterials_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create TraceProcesses table
            migrationBuilder.CreateTable(
                name: "TraceProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(nullable: false),
                    WorkOrderOperationId = table.Column<int>(nullable: true),
                    OperationCode = table.Column<string>(maxLength: 50, nullable: false),
                    OperationDescription = table.Column<string>(maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: true),
                    DurationMinutes = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OperatorId = table.Column<int>(nullable: true),
                    OperatorName = table.Column<string>(maxLength: 100, nullable: true),
                    MachineId = table.Column<int>(nullable: true),
                    MachineName = table.Column<string>(maxLength: 100, nullable: true),
                    PassedInspection = table.Column<bool>(nullable: true),
                    InspectionNotes = table.Column<string>(maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceProcesses_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceProcesses_WorkOrderOperations_WorkOrderOperationId",
                        column: x => x.WorkOrderOperationId,
                        principalTable: "WorkOrderOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraceProcesses_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create TraceParameters table
            migrationBuilder.CreateTable(
                name: "TraceParameters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceProcessId = table.Column<int>(nullable: false),
                    ParameterName = table.Column<string>(maxLength: 100, nullable: false),
                    ParameterValue = table.Column<string>(maxLength: 200, nullable: false),
                    Unit = table.Column<string>(maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceParameters_TraceProcesses_TraceProcessId",
                        column: x => x.TraceProcessId,
                        principalTable: "TraceProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceAssemblies table
            migrationBuilder.CreateTable(
                name: "TraceAssemblies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(nullable: false),
                    AssemblyCode = table.Column<string>(maxLength: 50, nullable: false),
                    AssemblyName = table.Column<string>(maxLength: 200, nullable: false),
                    DrawingNumber = table.Column<string>(maxLength: 100, nullable: true),
                    Revision = table.Column<string>(maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceAssemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceAssemblies_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceComponents table
            migrationBuilder.CreateTable(
                name: "TraceComponents",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceAssemblyId = table.Column<int>(nullable: false),
                    ComponentTraceId = table.Column<Guid>(nullable: false),
                    ComponentCode = table.Column<string>(maxLength: 50, nullable: true),
                    Description = table.Column<string>(maxLength: 200, nullable: true),
                    QuantityUsed = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceComponents_TraceAssemblies_TraceAssemblyId",
                        column: x => x.TraceAssemblyId,
                        principalTable: "TraceAssemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceDocuments table
            migrationBuilder.CreateTable(
                name: "TraceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(nullable: false),
                    DocumentType = table.Column<int>(nullable: false),
                    DocumentName = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(nullable: false),
                    MimeType = table.Column<string>(maxLength: 100, nullable: false),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    UploadedBy = table.Column<int>(nullable: false),
                    IsVerified = table.Column<bool>(nullable: false),
                    VerifiedDate = table.Column<DateTime>(nullable: true),
                    VerifiedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceDocuments_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceTakeoffs table
            migrationBuilder.CreateTable(
                name: "TraceTakeoffs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(nullable: false),
                    PdfUrl = table.Column<string>(maxLength: 500, nullable: false),
                    DrawingId = table.Column<int>(nullable: true),
                    Scale = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ScaleUnit = table.Column<string>(maxLength: 20, nullable: true),
                    Status = table.Column<string>(maxLength: 50, nullable: false),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CompletedDate = table.Column<DateTime>(nullable: true),
                    CompanyId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffs_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create TraceTakeoffMeasurements table
            migrationBuilder.CreateTable(
                name: "TraceTakeoffMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceTakeoffId = table.Column<int>(nullable: false),
                    CatalogueItemId = table.Column<int>(nullable: true),
                    MeasurementType = table.Column<string>(maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(maxLength: 20, nullable: false),
                    Coordinates = table.Column<string>(nullable: true),
                    Label = table.Column<string>(maxLength: 200, nullable: true),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    Color = table.Column<string>(maxLength: 50, nullable: true),
                    PageNumber = table.Column<int>(nullable: true),
                    CalculatedWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffMeasurements_TraceTakeoffs_TraceTakeoffId",
                        column: x => x.TraceTakeoffId,
                        principalTable: "TraceTakeoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffMeasurements_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_TraceId",
                table: "TraceRecords",
                column: "TraceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_TraceNumber",
                table: "TraceRecords",
                column: "TraceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_EntityType_EntityId",
                table: "TraceRecords",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_CompanyId",
                table: "TraceRecords",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_UserId",
                table: "TraceRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_WorkCenterId",
                table: "TraceRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterials_TraceRecordId",
                table: "TraceMaterials",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterials_CatalogueItemId",
                table: "TraceMaterials",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_TraceRecordId",
                table: "TraceProcesses",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_WorkOrderOperationId",
                table: "TraceProcesses",
                column: "WorkOrderOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_OperatorId",
                table: "TraceProcesses",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceParameters_TraceProcessId",
                table: "TraceParameters",
                column: "TraceProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAssemblies_TraceRecordId",
                table: "TraceAssemblies",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceComponents_TraceAssemblyId",
                table: "TraceComponents",
                column: "TraceAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDocuments_TraceRecordId",
                table: "TraceDocuments",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffs_TraceRecordId",
                table: "TraceTakeoffs",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_TraceTakeoffId",
                table: "TraceTakeoffMeasurements",
                column: "TraceTakeoffId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_CatalogueItemId",
                table: "TraceTakeoffMeasurements",
                column: "CatalogueItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TraceTakeoffMeasurements");
            migrationBuilder.DropTable(name: "TraceTakeoffs");
            migrationBuilder.DropTable(name: "TraceDocuments");
            migrationBuilder.DropTable(name: "TraceComponents");
            migrationBuilder.DropTable(name: "TraceAssemblies");
            migrationBuilder.DropTable(name: "TraceParameters");
            migrationBuilder.DropTable(name: "TraceProcesses");
            migrationBuilder.DropTable(name: "TraceMaterials");
            migrationBuilder.DropTable(name: "TraceRecords");
        }
    }
}