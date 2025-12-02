using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estimations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstimationNumber = table.Column<string>(maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(nullable: false),
                    ProjectName = table.Column<string>(maxLength: 200, nullable: false),
                    EstimationDate = table.Column<DateTime>(nullable: false),
                    ValidUntil = table.Column<DateTime>(nullable: false),
                    RevisionNumber = table.Column<int>(nullable: false),
                    TotalMaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverheadAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MarginAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Draft"),
                    ApprovedDate = table.Column<DateTime>(nullable: true),
                    ApprovedBy = table.Column<int>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    LastModifiedBy = table.Column<int>(nullable: false),
                    OrderId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estimations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estimations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estimations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimationPackages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstimationId = table.Column<int>(nullable: false),
                    PackageName = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    SequenceNumber = table.Column<int>(nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PackageTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDuration = table.Column<int>(nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationPackages_Estimations_EstimationId",
                        column: x => x.EstimationId,
                        principalTable: "Estimations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_CustomerId",
                table: "Estimations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_CreatedBy",
                table: "Estimations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_OrderId",
                table: "Estimations",
                column: "OrderId",
                unique: true,
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationPackages_EstimationId",
                table: "EstimationPackages",
                column: "EstimationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EstimationId",
                table: "Orders",
                column: "EstimationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Estimations_EstimationId",
                table: "Orders",
                column: "EstimationId",
                principalTable: "Estimations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Estimations_EstimationId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "EstimationPackages");

            migrationBuilder.DropTable(
                name: "Estimations");

            migrationBuilder.DropIndex(
                name: "IX_Orders_EstimationId",
                table: "Orders");
        }
    }
}
