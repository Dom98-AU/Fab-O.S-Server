using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderResourceEntryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                table: "WorkOrderRoutingLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationName",
                table: "WorkOrderRoutingLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "WorkOrderResourceEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "WorkOrderResourceEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkOrderResourceEntries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WorkOrderResourceEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "WorkOrderResourceEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualRunTime",
                table: "RoutingLines",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSetupTime",
                table: "RoutingLines",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "RoutingLines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationCode",
                table: "RoutingLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationName",
                table: "RoutingLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityProcessed",
                table: "RoutingLines",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "RoutingLines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RoutingLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationCode",
                table: "WorkOrderRoutingLines");

            migrationBuilder.DropColumn(
                name: "OperationName",
                table: "WorkOrderRoutingLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "WorkOrderResourceEntries");

            migrationBuilder.DropColumn(
                name: "ActualRunTime",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "ActualSetupTime",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "OperationCode",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "OperationName",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "QuantityProcessed",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "RoutingLines");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RoutingLines");
        }
    }
}
