using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPerModuleDocumentLibraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // SHAREPOINT PER-MODULE DOCUMENT LIBRARIES
            // ================================================================
            // Remove old single-library columns
            migrationBuilder.DropColumn(
                name: "DocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "TakeoffsRootFolder",
                table: "CompanySharePointSettings");

            // Add new per-module library columns (nullable - users configure per module)
            migrationBuilder.AddColumn<string>(
                name: "AssetsDocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetsRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimateDocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimateRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabMateDocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FabMateRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QDocsDocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QDocsRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceDocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // ================================================================
            // NOTE: Routing table changes removed - database already updated
            // ================================================================
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // ROLLBACK: Remove per-module SharePoint columns
            // ================================================================
            migrationBuilder.DropColumn(
                name: "AssetsDocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "AssetsRootFolder",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "EstimateDocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "EstimateRootFolder",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "FabMateDocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "FabMateRootFolder",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "QDocsDocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "QDocsRootFolder",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "TraceDocumentLibrary",
                table: "CompanySharePointSettings");

            migrationBuilder.DropColumn(
                name: "TraceRootFolder",
                table: "CompanySharePointSettings");

            // Restore old columns
            migrationBuilder.AddColumn<string>(
                name: "DocumentLibrary",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TakeoffsRootFolder",
                table: "CompanySharePointSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
