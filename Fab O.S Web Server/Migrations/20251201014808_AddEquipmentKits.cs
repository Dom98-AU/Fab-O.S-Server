using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentKits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultCheckoutDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    RequiresSignature = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequiresConditionCheck = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentKits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    KitTemplateId = table.Column<int>(type: "int", nullable: true),
                    KitCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    AssignedToUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QRCodeData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QRCodeIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasMaintenanceFlag = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaintenanceFlagNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentKits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentKits_KitTemplates_KitTemplateId",
                        column: x => x.KitTemplateId,
                        principalTable: "KitTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitTemplateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitTemplateId = table.Column<int>(type: "int", nullable: false),
                    EquipmentTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitTemplateItems_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitTemplateItems_KitTemplates_KitTemplateId",
                        column: x => x.KitTemplateId,
                        principalTable: "KitTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitCheckouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    KitId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CheckedOutToUserId = table.Column<int>(type: "int", nullable: false),
                    CheckedOutToUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CheckoutDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ExpectedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckoutPurpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProjectReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckoutOverallCondition = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CheckoutNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CheckoutSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckoutSignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckoutProcessedByUserId = table.Column<int>(type: "int", nullable: true),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReturnedByUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReturnOverallCondition = table.Column<int>(type: "int", nullable: true),
                    ReturnNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReturnSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnSignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnProcessedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitCheckouts_EquipmentKits_KitId",
                        column: x => x.KitId,
                        principalTable: "EquipmentKits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentKitItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    TemplateItemId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    NeedsMaintenance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentKitItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentKitItems_EquipmentKits_KitId",
                        column: x => x.KitId,
                        principalTable: "EquipmentKits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentKitItems_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentKitItems_KitTemplateItems_TemplateItemId",
                        column: x => x.TemplateItemId,
                        principalTable: "KitTemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitCheckoutItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitCheckoutId = table.Column<int>(type: "int", nullable: false),
                    KitItemId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    WasPresentAtCheckout = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CheckoutCondition = table.Column<int>(type: "int", nullable: true),
                    CheckoutNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WasPresentAtReturn = table.Column<bool>(type: "bit", nullable: true),
                    ReturnCondition = table.Column<int>(type: "int", nullable: true),
                    ReturnNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DamageReported = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DamageDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitCheckoutItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitCheckoutItems_EquipmentKitItems_KitItemId",
                        column: x => x.KitItemId,
                        principalTable: "EquipmentKitItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitCheckoutItems_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitCheckoutItems_KitCheckouts_KitCheckoutId",
                        column: x => x.KitCheckoutId,
                        principalTable: "KitCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKitItems_EquipmentId",
                table: "EquipmentKitItems",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKitItems_KitId",
                table: "EquipmentKitItems",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKitItems_KitId_EquipmentId",
                table: "EquipmentKitItems",
                columns: new[] { "KitId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKitItems_TemplateItemId",
                table: "EquipmentKitItems",
                column: "TemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_AssignedToUserId",
                table: "EquipmentKits",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_CompanyId_IsDeleted",
                table: "EquipmentKits",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_CompanyId_KitCode",
                table: "EquipmentKits",
                columns: new[] { "CompanyId", "KitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_KitTemplateId",
                table: "EquipmentKits",
                column: "KitTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_QRCodeIdentifier",
                table: "EquipmentKits",
                column: "QRCodeIdentifier",
                unique: true,
                filter: "[QRCodeIdentifier] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentKits_Status",
                table: "EquipmentKits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckoutItems_EquipmentId",
                table: "KitCheckoutItems",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckoutItems_KitCheckoutId",
                table: "KitCheckoutItems",
                column: "KitCheckoutId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckoutItems_KitItemId",
                table: "KitCheckoutItems",
                column: "KitItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_CheckedOutToUserId",
                table: "KitCheckouts",
                column: "CheckedOutToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_CompanyId_Status",
                table: "KitCheckouts",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_ExpectedReturnDate",
                table: "KitCheckouts",
                column: "ExpectedReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_KitId",
                table: "KitCheckouts",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_KitCheckouts_Status",
                table: "KitCheckouts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KitTemplateItems_EquipmentTypeId",
                table: "KitTemplateItems",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_KitTemplateItems_KitTemplateId",
                table: "KitTemplateItems",
                column: "KitTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_KitTemplates_Category",
                table: "KitTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KitTemplates_CompanyId_IsDeleted",
                table: "KitTemplates",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_KitTemplates_CompanyId_TemplateCode",
                table: "KitTemplates",
                columns: new[] { "CompanyId", "TemplateCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitCheckoutItems");

            migrationBuilder.DropTable(
                name: "EquipmentKitItems");

            migrationBuilder.DropTable(
                name: "KitCheckouts");

            migrationBuilder.DropTable(
                name: "KitTemplateItems");

            migrationBuilder.DropTable(
                name: "EquipmentKits");

            migrationBuilder.DropTable(
                name: "KitTemplates");
        }
    }
}
