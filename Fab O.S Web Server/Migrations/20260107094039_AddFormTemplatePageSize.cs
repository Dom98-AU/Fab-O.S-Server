using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFormTemplatePageSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCreateForm",
                table: "EstimationWorksheetColumns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FormDisplayField",
                table: "EstimationWorksheetColumns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkedFormTemplateId",
                table: "EstimationWorksheetColumns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModuleContext = table.Column<int>(type: "int", nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsCompanyDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    NumberPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShowSectionHeaders = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowNotes = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PageWidthMm = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    PageHeightMm = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    PageOrientation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormTemplates_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // NOTE: TemplateImportMappings table already exists in database, skipping creation

            migrationBuilder.CreateTable(
                name: "FormInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FormTemplateId = table.Column<int>(type: "int", nullable: false),
                    FormNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LinkedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LinkedEntityId = table.Column<int>(type: "int", nullable: true),
                    LinkedEntityDisplay = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormInstances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormTemplateFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormTemplateId = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SectionOrder = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "full"),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationRegex = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SelectOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formula = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LinkedWorksheetTemplateId = table.Column<int>(type: "int", nullable: true),
                    MaxPhotos = table.Column<int>(type: "int", nullable: true),
                    RequirePhotoLocation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplateFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplateFields_EstimationWorksheetTemplates_LinkedWorksheetTemplateId",
                        column: x => x.LinkedWorksheetTemplateId,
                        principalTable: "EstimationWorksheetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormTemplateFields_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormInstanceValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormInstanceId = table.Column<int>(type: "int", nullable: false),
                    FormTemplateFieldId = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    JsonValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureDataUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassFailValue = table.Column<bool>(type: "bit", nullable: true),
                    PassFailComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormInstanceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormInstanceValues_FormInstances_FormInstanceId",
                        column: x => x.FormInstanceId,
                        principalTable: "FormInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormInstanceValues_FormTemplateFields_FormTemplateFieldId",
                        column: x => x.FormTemplateFieldId,
                        principalTable: "FormTemplateFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstanceValues_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormInstanceAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormInstanceId = table.Column<int>(type: "int", nullable: false),
                    FormInstanceValueId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormInstanceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormInstanceAttachments_FormInstanceValues_FormInstanceValueId",
                        column: x => x.FormInstanceValueId,
                        principalTable: "FormInstanceValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormInstanceAttachments_FormInstances_FormInstanceId",
                        column: x => x.FormInstanceId,
                        principalTable: "FormInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormInstanceAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceAttachments_FormInstanceId",
                table: "FormInstanceAttachments",
                column: "FormInstanceId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceAttachments_FormInstanceValueId",
                table: "FormInstanceAttachments",
                column: "FormInstanceValueId",
                filter: "[FormInstanceValueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceAttachments_UploadedByUserId",
                table: "FormInstanceAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_ApprovedByUserId",
                table: "FormInstances",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_CompanyId_FormNumber",
                table: "FormInstances",
                columns: new[] { "CompanyId", "FormNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_CompanyId_Status",
                table: "FormInstances",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_CreatedByUserId",
                table: "FormInstances",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_FormTemplateId",
                table: "FormInstances",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_LinkedEntityType_LinkedEntityId",
                table: "FormInstances",
                columns: new[] { "LinkedEntityType", "LinkedEntityId" },
                filter: "[LinkedEntityType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_ModifiedByUserId",
                table: "FormInstances",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_ReviewedByUserId",
                table: "FormInstances",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_SubmittedByUserId",
                table: "FormInstances",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceValues_FormInstanceId_FieldKey",
                table: "FormInstanceValues",
                columns: new[] { "FormInstanceId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceValues_FormTemplateFieldId",
                table: "FormInstanceValues",
                column: "FormTemplateFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstanceValues_ModifiedByUserId",
                table: "FormInstanceValues",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateFields_FormTemplateId_FieldKey",
                table: "FormTemplateFields",
                columns: new[] { "FormTemplateId", "FieldKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateFields_FormTemplateId_SectionOrder_DisplayOrder",
                table: "FormTemplateFields",
                columns: new[] { "FormTemplateId", "SectionOrder", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateFields_LinkedWorksheetTemplateId",
                table: "FormTemplateFields",
                column: "LinkedWorksheetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CompanyId_ModuleContext",
                table: "FormTemplates",
                columns: new[] { "CompanyId", "ModuleContext" });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CompanyId_Name",
                table: "FormTemplates",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CreatedByUserId",
                table: "FormTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_IsSystemTemplate",
                table: "FormTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_ModifiedByUserId",
                table: "FormTemplates",
                column: "ModifiedByUserId");

            // NOTE: TemplateImportMappings indexes already exist in database, skipping creation
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormInstanceAttachments");

            // NOTE: TemplateImportMappings was not created by this migration, not dropping

            migrationBuilder.DropTable(
                name: "FormInstanceValues");

            migrationBuilder.DropTable(
                name: "FormInstances");

            migrationBuilder.DropTable(
                name: "FormTemplateFields");

            migrationBuilder.DropTable(
                name: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "AllowCreateForm",
                table: "EstimationWorksheetColumns");

            migrationBuilder.DropColumn(
                name: "FormDisplayField",
                table: "EstimationWorksheetColumns");

            migrationBuilder.DropColumn(
                name: "LinkedFormTemplateId",
                table: "EstimationWorksheetColumns");
        }
    }
}
