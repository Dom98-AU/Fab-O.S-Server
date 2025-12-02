using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuoteAddEstimations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: Commenting out operations for things that don't exist in database
            // These were in the model but never created in the database

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Catalogues_Users_CreatorId",
            //     table: "Catalogues");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Catalogues_Users_ModifierId",
            //     table: "Catalogues");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Orders_Quotes_QuoteId",
            //     table: "Orders");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Orders_Users_LastModifiedBy",
            //     table: "Orders");

            // migrationBuilder.DropTable(
            //     name: "QuoteLineItems");

            // migrationBuilder.DropTable(
            //     name: "Quotes");

            // migrationBuilder.DropIndex(
            //     name: "IX_Orders_QuoteId",
            //     table: "Orders");

            // migrationBuilder.DropIndex(
            //     name: "IX_Catalogues_CreatorId",
            //     table: "Catalogues");

            // migrationBuilder.DropIndex(
            //     name: "IX_Catalogues_ModifierId",
            //     table: "Catalogues");

            // migrationBuilder.DropColumn(
            //     name: "Description",
            //     table: "Orders");

            // migrationBuilder.DropColumn(
            //     name: "ProjectName",
            //     table: "Orders");

            // migrationBuilder.DropColumn(
            //     name: "QuoteId",
            //     table: "Orders");

            // migrationBuilder.DropColumn(
            //     name: "CreatorId",
            //     table: "Catalogues");

            // migrationBuilder.DropColumn(
            //     name: "ModifierId",
            //     table: "Catalogues");

            // migrationBuilder.RenameColumn(
            //     name: "LastModifiedBy",
            //     table: "Orders",
            //     newName: "ModifiedBy");

            // migrationBuilder.RenameIndex(
            //     name: "IX_Orders_LastModifiedBy",
            //     table: "Orders",
            //     newName: "IX_Orders_ModifiedBy");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Catalogues_CreatedBy",
            //     table: "Catalogues",
            //     column: "CreatedBy");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Catalogues_ModifiedBy",
            //     table: "Catalogues",
            //     column: "ModifiedBy");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Catalogues_Users_CreatedBy",
            //     table: "Catalogues",
            //     column: "CreatedBy",
            //     principalTable: "Users",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Catalogues_Users_ModifiedBy",
            //     table: "Catalogues",
            //     column: "ModifiedBy",
            //     principalTable: "Users",
            //     principalColumn: "Id");

            // Foreign key already exists in database
            // migrationBuilder.AddForeignKey(
            //     name: "FK_Orders_Users_ModifiedBy",
            //     table: "Orders",
            //     column: "ModifiedBy",
            //     principalTable: "Users",
            //     principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Catalogues_Users_CreatedBy",
                table: "Catalogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Catalogues_Users_ModifiedBy",
                table: "Catalogues");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_ModifiedBy",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Catalogues_CreatedBy",
                table: "Catalogues");

            migrationBuilder.DropIndex(
                name: "IX_Catalogues_ModifiedBy",
                table: "Catalogues");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "Orders",
                newName: "LastModifiedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_ModifiedBy",
                table: "Orders",
                newName: "IX_Orders_LastModifiedBy");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuoteId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Catalogues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifierId",
                table: "Catalogues",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LaborHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LaborRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QuoteDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    QuoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "dateadd(day, 30, getutcdate())")
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
                name: "QuoteLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    CatalogueItemId = table.Column<int>(type: "int", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Orders_QuoteId",
                table: "Orders",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalogues_CreatorId",
                table: "Catalogues",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalogues_ModifierId",
                table: "Catalogues",
                column: "ModifierId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Catalogues_Users_CreatorId",
                table: "Catalogues",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Catalogues_Users_ModifierId",
                table: "Catalogues",
                column: "ModifierId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Quotes_QuoteId",
                table: "Orders",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_LastModifiedBy",
                table: "Orders",
                column: "LastModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
