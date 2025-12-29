using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationWorksheetSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: The original Estimations table did not have FK_Estimations_Users_ApprovedBy
            // or FK_Estimations_Users_LastModifiedBy constraints - they were never created.
            // The model snapshot may have been out of sync. These drops are removed.

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Estimations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentRevisionLetter",
                table: "Estimations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentTotal",
                table: "Estimations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Estimations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Estimations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Estimations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Estimations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceTakeoffId",
                table: "Estimations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EstimationRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EstimationId = table.Column<int>(type: "int", nullable: false),
                    RevisionLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "A"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupersedesLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SupersededBy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TotalMaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 15.00m),
                    OverheadAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 20.00m),
                    MarginAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidUntilDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SubmittedBy = table.Column<int>(type: "int", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Estimations_EstimationId",
                        column: x => x.EstimationId,
                        principalTable: "Estimations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationRevisions_Users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheetTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorksheetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Custom"),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsCompanyDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    AllowColumnReorder = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowAddRows = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowDeleteRows = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowRowNumbers = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowColumnTotals = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DefaultFormulas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheetTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetTemplates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetTemplates_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EstimationRevisionPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    RevisionId = table.Column<int>(type: "int", nullable: false),
                    SourceTakeoffPackageId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverheadCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PackageTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDurationDays = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationRevisionPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationRevisionPackages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationRevisionPackages_EstimationRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "EstimationRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimationRevisionPackages_Packages_SourceTakeoffPackageId",
                        column: x => x.SourceTakeoffPackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheetColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorksheetTemplateId = table.Column<int>(type: "int", nullable: false),
                    ColumnKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Text"),
                    Width = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsFrozen = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FreezePosition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TextAlign = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    NumberFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SelectOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formula = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShowColumnTotal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ColumnTotalFunction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LinkToCatalogue = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CatalogueField = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AutoPopulateFromCatalogue = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LookupEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LookupDisplayField = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LookupFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PersonSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxImageSizeKb = table.Column<int>(type: "int", nullable: true),
                    AllowedImageTypes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocationFormat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MultilineRows = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    ValidationRegex = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheetColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetColumns_EstimationWorksheetTemplates_WorksheetTemplateId",
                        column: x => x.WorksheetTemplateId,
                        principalTable: "EstimationWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorksheetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Custom"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TotalMaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ColumnConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnTotals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheets_EstimationRevisionPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "EstimationRevisionPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheets_EstimationWorksheetTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EstimationWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheetChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorksheetId = table.Column<int>(type: "int", nullable: false),
                    RowId = table.Column<int>(type: "int", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    ChangedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheetChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetChanges_EstimationWorksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalTable: "EstimationWorksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetChanges_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheetInstanceColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorksheetId = table.Column<int>(type: "int", nullable: false),
                    ColumnKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Formula = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SelectOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Precision = table.Column<int>(type: "int", nullable: true),
                    LinkToCatalogue = table.Column<bool>(type: "bit", nullable: false),
                    CatalogueField = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AutoPopulateFromCatalogue = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheetInstanceColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetInstanceColumns_EstimationWorksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalTable: "EstimationWorksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimationWorksheetRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    WorksheetId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RowData = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    CalculatedTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    MatchStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MatchConfidence = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    ParentRowId = table.Column<int>(type: "int", nullable: true),
                    IsGroupHeader = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsExpanded = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    EstimationWorksheetId = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationWorksheetRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetRows_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetRows_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetRows_EstimationWorksheetRows_ParentRowId",
                        column: x => x.ParentRowId,
                        principalTable: "EstimationWorksheetRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimationWorksheetRows_EstimationWorksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalTable: "EstimationWorksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_CompanyId",
                table: "Estimations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_SourceTakeoffId",
                table: "Estimations",
                column: "SourceTakeoffId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisionPackages_CompanyId",
                table: "EstimationRevisionPackages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisionPackages_RevisionId",
                table: "EstimationRevisionPackages",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisionPackages_SortOrder",
                table: "EstimationRevisionPackages",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisionPackages_SourceTakeoffPackageId",
                table: "EstimationRevisionPackages",
                column: "SourceTakeoffPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_ApprovedBy",
                table: "EstimationRevisions",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_CompanyId_Status",
                table: "EstimationRevisions",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_CreatedBy",
                table: "EstimationRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_EstimationId_RevisionLetter",
                table: "EstimationRevisions",
                columns: new[] { "EstimationId", "RevisionLetter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_ReviewedBy",
                table: "EstimationRevisions",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_Status",
                table: "EstimationRevisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationRevisions_SubmittedBy",
                table: "EstimationRevisions",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetChanges_ChangedBy",
                table: "EstimationWorksheetChanges",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetChanges_ChangedDate",
                table: "EstimationWorksheetChanges",
                column: "ChangedDate");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetChanges_WorksheetId",
                table: "EstimationWorksheetChanges",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetChanges_WorksheetId_ChangedDate",
                table: "EstimationWorksheetChanges",
                columns: new[] { "WorksheetId", "ChangedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetColumns_DisplayOrder",
                table: "EstimationWorksheetColumns",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetColumns_WorksheetTemplateId_ColumnKey",
                table: "EstimationWorksheetColumns",
                columns: new[] { "WorksheetTemplateId", "ColumnKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetInstanceColumns_WorksheetId",
                table: "EstimationWorksheetInstanceColumns",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_CatalogueItemId",
                table: "EstimationWorksheetRows",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_CompanyId",
                table: "EstimationWorksheetRows",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_ParentRowId",
                table: "EstimationWorksheetRows",
                column: "ParentRowId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_SortOrder",
                table: "EstimationWorksheetRows",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_WorksheetId",
                table: "EstimationWorksheetRows",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetRows_WorksheetId_IsDeleted",
                table: "EstimationWorksheetRows",
                columns: new[] { "WorksheetId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheets_CompanyId",
                table: "EstimationWorksheets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheets_PackageId",
                table: "EstimationWorksheets",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheets_SortOrder",
                table: "EstimationWorksheets",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheets_TemplateId",
                table: "EstimationWorksheets",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetTemplates_CompanyId_Name",
                table: "EstimationWorksheetTemplates",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetTemplates_CreatedBy",
                table: "EstimationWorksheetTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetTemplates_IsSystemTemplate",
                table: "EstimationWorksheetTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetTemplates_ModifiedBy",
                table: "EstimationWorksheetTemplates",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationWorksheetTemplates_WorksheetType",
                table: "EstimationWorksheetTemplates",
                column: "WorksheetType");

            migrationBuilder.AddForeignKey(
                name: "FK_Estimations_Companies_CompanyId",
                table: "Estimations",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Estimations_TraceDrawings_SourceTakeoffId",
                table: "Estimations",
                column: "SourceTakeoffId",
                principalTable: "TraceDrawings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_Companies_CompanyId",
                table: "Estimations");

            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_TraceDrawings_SourceTakeoffId",
                table: "Estimations");

            migrationBuilder.DropTable(
                name: "EstimationWorksheetChanges");

            migrationBuilder.DropTable(
                name: "EstimationWorksheetColumns");

            migrationBuilder.DropTable(
                name: "EstimationWorksheetInstanceColumns");

            migrationBuilder.DropTable(
                name: "EstimationWorksheetRows");

            migrationBuilder.DropTable(
                name: "EstimationWorksheets");

            migrationBuilder.DropTable(
                name: "EstimationRevisionPackages");

            migrationBuilder.DropTable(
                name: "EstimationWorksheetTemplates");

            migrationBuilder.DropTable(
                name: "EstimationRevisions");

            migrationBuilder.DropIndex(
                name: "IX_Estimations_CompanyId",
                table: "Estimations");

            migrationBuilder.DropIndex(
                name: "IX_Estimations_SourceTakeoffId",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "CurrentRevisionLetter",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "CurrentTotal",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Estimations");

            migrationBuilder.DropColumn(
                name: "SourceTakeoffId",
                table: "Estimations");

            // Note: The original table did not have these indexes/FKs, so we don't recreate them
        }
    }
}
