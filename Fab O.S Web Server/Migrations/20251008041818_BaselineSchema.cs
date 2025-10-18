using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class BaselineSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SubscriptionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Standard"),
                    MaxUsers = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ABN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EfficiencyRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfficiencyRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViewPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ViewType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    ViewStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViewPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assemblies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssemblyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DrawingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParentAssemblyId = table.Column<int>(type: "int", nullable: true),
                    EstimatedWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assemblies_Assemblies_ParentAssemblyId",
                        column: x => x.ParentAssemblyId,
                        principalTable: "Assemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assemblies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Material = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Profile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Length_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Width_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Height_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Depth_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Thickness_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Diameter_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    OD_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ID_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    WallThickness_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Wall_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    NominalBore = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ImperialEquiv = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Web_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Flange_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    A_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    B_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Size_mm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Size = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Size_inch = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BMT_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    BaseThickness_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RaisedThickness_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Mass_kg_m = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    Mass_kg_m2 = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    Mass_kg_length = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    Weight_kg = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    Weight_kg_m2 = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    SurfaceArea_m2 = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    SurfaceArea_m2_per_m = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    SurfaceArea_m2_per_m2 = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Surface = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Standard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Alloy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Temper = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Finish = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Finish_Standard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Coating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StandardLengths = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StandardLength_m = table.Column<int>(type: "int", nullable: true),
                    Cut_To_Size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Pattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Features = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tolerance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Pressure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupplierCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PackQty = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Compliance = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Duty_Rating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogueItems_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedOutUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    PasswordSalt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCenterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkCenterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenters_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GooglePlaceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GooglePlaceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InheritCustomerAddress = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutingOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutingTemplateId = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SetupTime = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RunTime = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_RoutingTemplates_RoutingTemplateId",
                        column: x => x.RoutingTemplateId,
                        principalTable: "RoutingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssemblyComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssemblyId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    ComponentAssemblyId = table.Column<int>(type: "int", nullable: true),
                    ComponentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AlternateItems = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssemblyComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssemblyComponents_Assemblies_AssemblyId",
                        column: x => x.AssemblyId,
                        principalTable: "Assemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssemblyComponents_Assemblies_ComponentAssemblyId",
                        column: x => x.ComponentAssemblyId,
                        principalTable: "Assemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssemblyComponents_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GratingSpecifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: false),
                    LoadBar_Height_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    LoadBar_Spacing_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    LoadBar_Thickness_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CrossBar_Spacing_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Standard_Panel_Length_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Standard_Panel_Width_mm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Dimensions = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GratingSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GratingSpecifications_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: false),
                    InventoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BinLocation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QuantityAvailable = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HeatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MillCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuthMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemSetting = table.Column<bool>(type: "bit", nullable: false),
                    RequiresRestart = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    ValidationRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalSettings_Users_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ModuleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsUserSpecific = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleSettings_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModuleSettings_Users_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModuleSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NumberSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Suffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    StartingNumber = table.Column<int>(type: "int", nullable: false),
                    IncrementBy = table.Column<int>(type: "int", nullable: false),
                    MinDigits = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IncludeYear = table.Column<bool>(type: "bit", nullable: false),
                    IncludeMonth = table.Column<bool>(type: "bit", nullable: false),
                    IncludeCompanyCode = table.Column<bool>(type: "bit", nullable: false),
                    ResetYearly = table.Column<bool>(type: "bit", nullable: false),
                    ResetMonthly = table.Column<bool>(type: "bit", nullable: false),
                    LastResetYear = table.Column<int>(type: "int", nullable: true),
                    LastResetMonth = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AllowManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreviewExample = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumberSeries_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NumberSeries_Users_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MachineCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkCenterId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    MachineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MachineSubType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaxCapacity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CapacityUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SetupTimeMinutes = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    WarmupTimeMinutes = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CooldownTimeMinutes = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PowerConsumptionKwh = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PowerCostPerKwh = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EfficiencyPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QualityRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AvailabilityRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CurrentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaintenanceIntervalHours = table.Column<int>(type: "int", nullable: false),
                    CurrentOperatingHours = table.Column<int>(type: "int", nullable: false),
                    RequiresTooling = table.Column<bool>(type: "bit", nullable: false),
                    ToolingRequirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineCenters_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MachineCenters_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineCenters_Users_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineCenters_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrimarySkill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SkillLevel = table.Column<int>(type: "int", nullable: false),
                    CertificationLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StandardHoursPerDay = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_WorkCenters_PrimaryWorkCenterId",
                        column: x => x.PrimaryWorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentTraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaptureDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkCenterId = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TraceRecordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceRecords_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceRecords_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceRecords_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCenterId = table.Column<int>(type: "int", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenterShifts_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryItemId = table.Column<int>(type: "int", nullable: false),
                    TransactionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineCapabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineCenterId = table.Column<int>(type: "int", nullable: false),
                    CapabilityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Units = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineCapabilities_MachineCenters_MachineCenterId",
                        column: x => x.MachineCenterId,
                        principalTable: "MachineCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MachineOperators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineCenterId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SkillLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EfficiencyRating = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CertificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CertificationExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineOperators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineOperators_MachineCenters_MachineCenterId",
                        column: x => x.MachineCenterId,
                        principalTable: "MachineCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MachineOperators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceAssemblies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(type: "int", nullable: false),
                    AssemblyId = table.Column<int>(type: "int", nullable: true),
                    AssemblyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssemblyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuildOperatorId = table.Column<int>(type: "int", nullable: true),
                    BuildOperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BuildWorkCenterId = table.Column<int>(type: "int", nullable: true),
                    BuildLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BuildNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceAssemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceAssemblies_Assemblies_AssemblyId",
                        column: x => x.AssemblyId,
                        principalTable: "Assemblies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceAssemblies_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceAssemblies_Users_BuildOperatorId",
                        column: x => x.BuildOperatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceAssemblies_WorkCenters_BuildWorkCenterId",
                        column: x => x.BuildWorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceDocuments_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceDocuments_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    MaterialCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeatNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplierBatch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MillCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CertType = table.Column<int>(type: "int", nullable: true),
                    CertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ChemicalComposition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MechanicalProperties = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TestResults = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceMaterials_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceMaterials_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceAssemblyId = table.Column<int>(type: "int", nullable: false),
                    ComponentTraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuantityUsed = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsageNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TraceMaterialCatalogueLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceMaterialId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CalculatedWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceMaterialCatalogueLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceMaterialCatalogueLinks_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceMaterialCatalogueLinks_TraceMaterials_TraceMaterialId",
                        column: x => x.TraceMaterialId,
                        principalTable: "TraceMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calibrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageDrawingId = table.Column<int>(type: "int", nullable: false),
                    PixelsPerUnit = table.Column<double>(type: "float", nullable: false),
                    ScaleRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KnownDistance = table.Column<double>(type: "float", nullable: false),
                    MeasuredPixels = table.Column<double>(type: "float", nullable: false),
                    Point1Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Point2Json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Units = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calibrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstimationPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstimationId = table.Column<int>(type: "int", nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PackageTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDuration = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estimations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstimationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstimationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    TotalMaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverheadAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MarginAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estimations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Estimations_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Estimations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estimations_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: true),
                    EstimationId = table.Column<int>(type: "int", nullable: true),
                    CustomerPONumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    RequiredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PromisedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Estimations_EstimationId",
                        column: x => x.EstimationId,
                        principalTable: "Estimations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EstimationStage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LaborRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ContingencyPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: true),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EstimatedCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProjectType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    QuoteDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "dateadd(day, 30, getutcdate())"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LaborRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Quotes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotes_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceDrawings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TakeoffNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DrawingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    Scale = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ScaleUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "mm"),
                    CalibrationData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrawingTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DrawingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Discipline = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Revision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OCRStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "NotProcessed"),
                    OCRProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OCRResultId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ContactId = table.Column<int>(type: "int", nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TraceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OcrConfidence = table.Column<int>(type: "int", nullable: true),
                    ProjectArchitect = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectEngineer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceDrawings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceDrawings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceDrawings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceDrawings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceDrawings_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteLineItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TakeoffRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakeoffId = table.Column<int>(type: "int", nullable: false),
                    RevisionCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CopiedFromRevisionId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TakeoffRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TakeoffRevisions_TakeoffRevisions_CopiedFromRevisionId",
                        column: x => x.CopiedFromRevisionId,
                        principalTable: "TakeoffRevisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TakeoffRevisions_TraceDrawings_TakeoffId",
                        column: x => x.TakeoffId,
                        principalTable: "TraceDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TakeoffRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceBeamDetections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceDrawingId = table.Column<int>(type: "int", nullable: false),
                    BeamType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BeamSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Length = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceBeamDetections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceBeamDetections_TraceDrawings_TraceDrawingId",
                        column: x => x.TraceDrawingId,
                        principalTable: "TraceDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceDrawingId = table.Column<int>(type: "int", nullable: false),
                    ElementType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Length = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Units = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceMeasurements_TraceDrawings_TraceDrawingId",
                        column: x => x.TraceDrawingId,
                        principalTable: "TraceDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceTakeoffItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceDrawingId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Units = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffItems_TraceDrawings_TraceDrawingId",
                        column: x => x.TraceDrawingId,
                        principalTable: "TraceDrawings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceTakeoffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(type: "int", nullable: false),
                    DrawingId = table.Column<int>(type: "int", nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ScaleUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CalibrationData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffs_TraceDrawings_DrawingId",
                        column: x => x.DrawingId,
                        principalTable: "TraceDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceTakeoffs_TraceRecords_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "TraceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    RevisionId = table.Column<int>(type: "int", nullable: true),
                    PackageSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Project"),
                    PackageNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LaborRatePerHour = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ProcessingEfficiency = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EfficiencyRateId = table.Column<int>(type: "int", nullable: true),
                    RoutingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_EfficiencyRates_EfficiencyRateId",
                        column: x => x.EfficiencyRateId,
                        principalTable: "EfficiencyRates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Packages_RoutingTemplates_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "RoutingTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_TakeoffRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "TakeoffRevisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Packages_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraceTakeoffAnnotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceTakeoffId = table.Column<int>(type: "int", nullable: false),
                    AnnotationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnnotationData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Coordinates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffAnnotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffAnnotations_TraceTakeoffs_TraceTakeoffId",
                        column: x => x.TraceTakeoffId,
                        principalTable: "TraceTakeoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffAnnotations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackageDrawings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    DrawingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DrawingTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SharePointItemId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SharePointUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDrawings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDrawings_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageDrawings_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeldingConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultAssembleFitTack = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultWeld = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultWeldCheck = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefaultWeldTest = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeldingConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeldingConnections_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    WorkOrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkCenterId = table.Column<int>(type: "int", nullable: true),
                    PrimaryResourceId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ScheduledStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Planning"),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasHoldPoints = table.Column<bool>(type: "bit", nullable: false),
                    RequiresInspection = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Resources_PrimaryResourceId",
                        column: x => x.PrimaryResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Users_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrders_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceTakeoffMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceTakeoffId = table.Column<int>(type: "int", nullable: false),
                    PackageDrawingId = table.Column<int>(type: "int", nullable: true),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    MeasurementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Coordinates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    CalculatedWeight = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceTakeoffMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceTakeoffMeasurements_CatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CatalogueItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceTakeoffMeasurements_PackageDrawings_PackageDrawingId",
                        column: x => x.PackageDrawingId,
                        principalTable: "PackageDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceTakeoffMeasurements_TraceTakeoffs_TraceTakeoffId",
                        column: x => x.TraceTakeoffId,
                        principalTable: "TraceTakeoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderAssembly",
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
                    table.PrimaryKey("PK_WorkOrderInventoryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderInventoryItem_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiredSkill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequiredSkillLevel = table.Column<int>(type: "int", nullable: true),
                    RequiredMachine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequiredTooling = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SetupTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CycleTime = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RequiresInspection = table.Column<bool>(type: "bit", nullable: false),
                    InspectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LinkedITPPointId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderOperations_Users_CompletedBy",
                        column: x => x.CompletedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderOperations_WorkOrders_WorkOrderId",
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

            migrationBuilder.CreateTable(
                name: "TraceProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceRecordId = table.Column<int>(type: "int", nullable: false),
                    WorkOrderOperationId = table.Column<int>(type: "int", nullable: true),
                    OperationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperationDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OperatorId = table.Column<int>(type: "int", nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PassedInspection = table.Column<bool>(type: "bit", nullable: true),
                    InspectionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_TraceProcesses_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TraceProcesses_WorkOrderOperations_WorkOrderOperationId",
                        column: x => x.WorkOrderOperationId,
                        principalTable: "WorkOrderOperations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TraceParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceProcessId = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParameterValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_AssemblyNumber",
                table: "Assemblies",
                column: "AssemblyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_CompanyId",
                table: "Assemblies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Assemblies_ParentAssemblyId",
                table: "Assemblies",
                column: "ParentAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyComponents_AssemblyId",
                table: "AssemblyComponents",
                column: "AssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyComponents_CatalogueItemId",
                table: "AssemblyComponents",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyComponents_ComponentAssemblyId",
                table: "AssemblyComponents",
                column: "ComponentAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_Email_Timestamp",
                table: "AuthAuditLogs",
                columns: new[] { "Email", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_Timestamp",
                table: "AuthAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_UserId",
                table: "AuthAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Calibrations_PackageDrawingId",
                table: "Calibrations",
                column: "PackageDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_Category",
                table: "CatalogueItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_CompanyId",
                table: "CatalogueItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_Finish",
                table: "CatalogueItems",
                column: "Finish");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_Grade",
                table: "CatalogueItems",
                column: "Grade");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_ItemCode",
                table: "CatalogueItems",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_Material",
                table: "CatalogueItems",
                column: "Material");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_Profile",
                table: "CatalogueItems",
                column: "Profile");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationPackages_EstimationId",
                table: "EstimationPackages",
                column: "EstimationId");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_ApprovedBy",
                table: "Estimations",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_CreatedBy",
                table: "Estimations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_CustomerId",
                table: "Estimations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_LastModifiedBy",
                table: "Estimations",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Estimations_OrderId",
                table: "Estimations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalSettings_LastModifiedByUserId",
                table: "GlobalSettings",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GratingSpecifications_CatalogueItemId",
                table: "GratingSpecifications",
                column: "CatalogueItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CatalogueItemId",
                table: "InventoryItems",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CompanyId",
                table: "InventoryItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_HeatNumber",
                table: "InventoryItems",
                column: "HeatNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryCode",
                table: "InventoryItems",
                column: "InventoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_LotNumber",
                table: "InventoryItems",
                column: "LotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CompanyId",
                table: "InventoryTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryItemId",
                table: "InventoryTransactions",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TransactionNumber",
                table: "InventoryTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_UserId",
                table: "InventoryTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCapabilities_MachineCenterId",
                table: "MachineCapabilities",
                column: "MachineCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCenters_CompanyId",
                table: "MachineCenters",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCenters_CreatedByUserId",
                table: "MachineCenters",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCenters_LastModifiedByUserId",
                table: "MachineCenters",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineCenters_MachineCode",
                table: "MachineCenters",
                column: "MachineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineCenters_WorkCenterId",
                table: "MachineCenters",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperators_MachineCenterId",
                table: "MachineOperators",
                column: "MachineCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperators_UserId",
                table: "MachineOperators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSettings_CompanyId",
                table: "ModuleSettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSettings_CreatedByUserId",
                table: "ModuleSettings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSettings_LastModifiedByUserId",
                table: "ModuleSettings",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSettings_UserId",
                table: "ModuleSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_CreatedByUserId",
                table: "NumberSeries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_LastModifiedByUserId",
                table: "NumberSeries",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedBy",
                table: "Orders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EstimationId",
                table: "Orders",
                column: "EstimationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LastModifiedBy",
                table: "Orders",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_QuoteId",
                table: "Orders",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDrawings_PackageId",
                table: "PackageDrawings",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDrawings_UploadedBy",
                table: "PackageDrawings",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CreatedBy",
                table: "Packages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_EfficiencyRateId",
                table: "Packages",
                column: "EfficiencyRateId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_LastModifiedBy",
                table: "Packages",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_OrderId",
                table: "Packages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ProjectId",
                table: "Packages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_RevisionId",
                table: "Packages",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_RoutingId",
                table: "Packages",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId",
                table: "Projects",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_JobNumber",
                table: "Projects",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LastModifiedBy",
                table: "Projects",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrderId",
                table: "Projects",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLineItems_QuoteId",
                table: "QuoteLineItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedBy",
                table: "Quotes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_LastModifiedBy",
                table: "Quotes",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_OrderId",
                table: "Quotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_PrimaryWorkCenterId",
                table: "Resources",
                column: "PrimaryWorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_UserId",
                table: "Resources",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_RoutingTemplateId",
                table: "RoutingOperations",
                column: "RoutingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TakeoffRevisions_CopiedFromRevisionId",
                table: "TakeoffRevisions",
                column: "CopiedFromRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TakeoffRevisions_CreatedBy",
                table: "TakeoffRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TakeoffRevisions_TakeoffId",
                table: "TakeoffRevisions",
                column: "TakeoffId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAssemblies_AssemblyId",
                table: "TraceAssemblies",
                column: "AssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAssemblies_BuildOperatorId",
                table: "TraceAssemblies",
                column: "BuildOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAssemblies_BuildWorkCenterId",
                table: "TraceAssemblies",
                column: "BuildWorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceAssemblies_TraceRecordId",
                table: "TraceAssemblies",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceBeamDetections_TraceDrawingId",
                table: "TraceBeamDetections",
                column: "TraceDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceComponents_TraceAssemblyId",
                table: "TraceComponents",
                column: "TraceAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDocuments_CompanyId",
                table: "TraceDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDocuments_TraceRecordId",
                table: "TraceDocuments",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDocuments_UploadedByUserId",
                table: "TraceDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDocuments_VerifiedByUserId",
                table: "TraceDocuments",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDrawings_CompanyId",
                table: "TraceDrawings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDrawings_CustomerId",
                table: "TraceDrawings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDrawings_ProjectId",
                table: "TraceDrawings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceDrawings_UploadedBy",
                table: "TraceDrawings",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterialCatalogueLinks_CatalogueItemId",
                table: "TraceMaterialCatalogueLinks",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterialCatalogueLinks_TraceMaterialId",
                table: "TraceMaterialCatalogueLinks",
                column: "TraceMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterials_CatalogueItemId",
                table: "TraceMaterials",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMaterials_TraceRecordId",
                table: "TraceMaterials",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceMeasurements_TraceDrawingId",
                table: "TraceMeasurements",
                column: "TraceDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceParameters_TraceProcessId",
                table: "TraceParameters",
                column: "TraceProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_OperatorId",
                table: "TraceProcesses",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_TraceRecordId",
                table: "TraceProcesses",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceProcesses_WorkOrderOperationId",
                table: "TraceProcesses",
                column: "WorkOrderOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_CompanyId",
                table: "TraceRecords",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_TraceRecordId",
                table: "TraceRecords",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_UserId",
                table: "TraceRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_WorkCenterId",
                table: "TraceRecords",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffAnnotations_CreatedByUserId",
                table: "TraceTakeoffAnnotations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffAnnotations_TraceTakeoffId",
                table: "TraceTakeoffAnnotations",
                column: "TraceTakeoffId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffItems_TraceDrawingId",
                table: "TraceTakeoffItems",
                column: "TraceDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_CatalogueItemId",
                table: "TraceTakeoffMeasurements",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_PackageDrawingId",
                table: "TraceTakeoffMeasurements",
                column: "PackageDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffMeasurements_TraceTakeoffId",
                table: "TraceTakeoffMeasurements",
                column: "TraceTakeoffId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffs_CompanyId",
                table: "TraceTakeoffs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffs_DrawingId",
                table: "TraceTakeoffs",
                column: "DrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceTakeoffs_TraceRecordId",
                table: "TraceTakeoffs",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthMethods_UserId_Provider",
                table: "UserAuthMethods",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeldingConnections_PackageId",
                table: "WeldingConnections",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_CompanyId",
                table: "WorkCenters",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_WorkCenterCode",
                table: "WorkCenters",
                column: "WorkCenterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterShifts_WorkCenterId",
                table: "WorkCenterShifts",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssembly_WorkOrderId",
                table: "WorkOrderAssembly",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderInventoryItem_WorkOrderId",
                table: "WorkOrderInventoryItem",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_CompletedBy",
                table: "WorkOrderOperations",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderOperations_WorkOrderId",
                table: "WorkOrderOperations",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResources_ResourceId",
                table: "WorkOrderResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderResources_WorkOrderId",
                table: "WorkOrderResources",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedBy",
                table: "WorkOrders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_LastModifiedBy",
                table: "WorkOrders",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PackageId",
                table: "WorkOrders",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PrimaryResourceId",
                table: "WorkOrders",
                column: "PrimaryResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkCenterId",
                table: "WorkOrders",
                column: "WorkCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Calibrations_PackageDrawings_PackageDrawingId",
                table: "Calibrations",
                column: "PackageDrawingId",
                principalTable: "PackageDrawings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EstimationPackages_Estimations_EstimationId",
                table: "EstimationPackages",
                column: "EstimationId",
                principalTable: "Estimations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Estimations_Orders_OrderId",
                table: "Estimations",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Quotes_QuoteId",
                table: "Orders",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_Users_ApprovedBy",
                table: "Estimations");

            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_Users_CreatedBy",
                table: "Estimations");

            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_Users_LastModifiedBy",
                table: "Estimations");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CreatedBy",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_LastModifiedBy",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Users_CreatedBy",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Users_LastModifiedBy",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Estimations_Customers_CustomerId",
                table: "Estimations");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Customers_CustomerId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Estimations_EstimationId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Orders_OrderId",
                table: "Quotes");

            migrationBuilder.DropTable(
                name: "AssemblyComponents");

            migrationBuilder.DropTable(
                name: "AuthAuditLogs");

            migrationBuilder.DropTable(
                name: "Calibrations");

            migrationBuilder.DropTable(
                name: "CustomerAddresses");

            migrationBuilder.DropTable(
                name: "CustomerContacts");

            migrationBuilder.DropTable(
                name: "EstimationPackages");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "GratingSpecifications");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "MachineCapabilities");

            migrationBuilder.DropTable(
                name: "MachineOperators");

            migrationBuilder.DropTable(
                name: "ModuleSettings");

            migrationBuilder.DropTable(
                name: "NumberSeries");

            migrationBuilder.DropTable(
                name: "QuoteLineItems");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoutingOperations");

            migrationBuilder.DropTable(
                name: "SavedViewPreferences");

            migrationBuilder.DropTable(
                name: "TraceBeamDetections");

            migrationBuilder.DropTable(
                name: "TraceComponents");

            migrationBuilder.DropTable(
                name: "TraceDocuments");

            migrationBuilder.DropTable(
                name: "TraceMaterialCatalogueLinks");

            migrationBuilder.DropTable(
                name: "TraceMeasurements");

            migrationBuilder.DropTable(
                name: "TraceParameters");

            migrationBuilder.DropTable(
                name: "TraceTakeoffAnnotations");

            migrationBuilder.DropTable(
                name: "TraceTakeoffItems");

            migrationBuilder.DropTable(
                name: "TraceTakeoffMeasurements");

            migrationBuilder.DropTable(
                name: "UserAuthMethods");

            migrationBuilder.DropTable(
                name: "WeldingConnections");

            migrationBuilder.DropTable(
                name: "WorkCenterShifts");

            migrationBuilder.DropTable(
                name: "WorkOrderAssembly");

            migrationBuilder.DropTable(
                name: "WorkOrderInventoryItem");

            migrationBuilder.DropTable(
                name: "WorkOrderResources");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "MachineCenters");

            migrationBuilder.DropTable(
                name: "TraceAssemblies");

            migrationBuilder.DropTable(
                name: "TraceMaterials");

            migrationBuilder.DropTable(
                name: "TraceProcesses");

            migrationBuilder.DropTable(
                name: "PackageDrawings");

            migrationBuilder.DropTable(
                name: "TraceTakeoffs");

            migrationBuilder.DropTable(
                name: "Assemblies");

            migrationBuilder.DropTable(
                name: "CatalogueItems");

            migrationBuilder.DropTable(
                name: "WorkOrderOperations");

            migrationBuilder.DropTable(
                name: "TraceRecords");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "EfficiencyRates");

            migrationBuilder.DropTable(
                name: "RoutingTemplates");

            migrationBuilder.DropTable(
                name: "TakeoffRevisions");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropTable(
                name: "TraceDrawings");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Estimations");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
