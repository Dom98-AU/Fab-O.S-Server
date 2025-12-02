using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddResourcesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: InspectionTestPlans and QualityDocuments tables don't exist yet - skipping FK operations
            // migrationBuilder.DropForeignKey(
            //     name: "FK_InspectionTestPlans_Packages_PackageId",
            //     table: "InspectionTestPlans");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_QualityDocuments_Packages_PackageId",
            //     table: "QualityDocuments");

            // NOTE: PackageId1 and FK don't exist in database yet - skipping
            // migrationBuilder.DropForeignKey(
            //     name: "FK_WorkOrders_Packages_PackageId1",
            //     table: "WorkOrders");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_WorkOrders_WorkPackages_PackageId",
            //     table: "WorkOrders");

            // migrationBuilder.DropIndex(
            //     name: "IX_WorkOrders_PackageId1",
            //     table: "WorkOrders");

            // NOTE: BillableValue doesn't exist in WorkPackages table yet
            // migrationBuilder.DropColumn(
            //     name: "BillableValue",
            //     table: "WorkPackages");

            // NOTE: PackageId1 doesn't exist in WorkOrders table
            // migrationBuilder.DropColumn(
            //     name: "PackageId1",
            //     table: "WorkOrders");

            // NOTE: QualityDocuments table doesn't exist yet
            // migrationBuilder.RenameColumn(
            //     name: "PackageId",
            //     table: "QualityDocuments",
            //     newName: "WorkPackageId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_QualityDocuments_PackageId",
            //     table: "QualityDocuments",
            //     newName: "IX_QualityDocuments_WorkPackageId");

            // NOTE: InspectionTestPlans table doesn't exist yet
            // migrationBuilder.RenameColumn(
            //     name: "PackageId",
            //     table: "InspectionTestPlans",
            //     newName: "WorkPackageId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_InspectionTestPlans_PackageId",
            //     table: "InspectionTestPlans",
            //     newName: "IX_InspectionTestPlans_WorkPackageId");

            // Create Resources table
            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Direct"),
                    PrimarySkill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SkillLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CertificationLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResourceGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StandardHoursPerDay = table.Column<decimal>(type: "decimal(4,2)", nullable: false, defaultValue: 8.00m),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DirectUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IndirectCostPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    PriceCalculation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "NoRelationship"),
                    PrimaryWorkCenterId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_WorkCenters_PrimaryWorkCenterId",
                        column: x => x.PrimaryWorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // NOTE: InspectionTestPlans table doesn't exist yet - skip FK
            // migrationBuilder.AddForeignKey(
            //     name: "FK_InspectionTestPlans_WorkPackages_WorkPackageId",
            //     table: "InspectionTestPlans",
            //     column: "WorkPackageId",
            //     principalTable: "WorkPackages",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);

            // NOTE: QualityDocuments table doesn't exist yet - skip FK
            // migrationBuilder.AddForeignKey(
            //     name: "FK_QualityDocuments_WorkPackages_WorkPackageId",
            //     table: "QualityDocuments",
            //     column: "WorkPackageId",
            //     principalTable: "WorkPackages",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);

            // NOTE: FK relationship already exists - skip
            // migrationBuilder.AddForeignKey(
            //     name: "FK_WorkOrders_WorkPackages_PackageId",
            //     table: "WorkOrders",
            //     column: "PackageId",
            //     principalTable: "WorkPackages",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionTestPlans_WorkPackages_WorkPackageId",
                table: "InspectionTestPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityDocuments_WorkPackages_WorkPackageId",
                table: "QualityDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_WorkPackages_PackageId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "DirectUnitCost",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IndirectCostPercentage",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PriceCalculation",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ResourceGroup",
                table: "Resources");

            migrationBuilder.RenameColumn(
                name: "WorkPackageId",
                table: "QualityDocuments",
                newName: "PackageId");

            migrationBuilder.RenameIndex(
                name: "IX_QualityDocuments_WorkPackageId",
                table: "QualityDocuments",
                newName: "IX_QualityDocuments_PackageId");

            migrationBuilder.RenameColumn(
                name: "WorkPackageId",
                table: "InspectionTestPlans",
                newName: "PackageId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionTestPlans_WorkPackageId",
                table: "InspectionTestPlans",
                newName: "IX_InspectionTestPlans_PackageId");

            migrationBuilder.AddColumn<decimal>(
                name: "BillableValue",
                table: "WorkPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PackageId1",
                table: "WorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PackageId1",
                table: "WorkOrders",
                column: "PackageId1");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionTestPlans_Packages_PackageId",
                table: "InspectionTestPlans",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityDocuments_Packages_PackageId",
                table: "QualityDocuments",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Packages_PackageId1",
                table: "WorkOrders",
                column: "PackageId1",
                principalTable: "Packages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_WorkPackages_PackageId",
                table: "WorkOrders",
                column: "PackageId",
                principalTable: "WorkPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
