using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class DrawingPartNullableDrawingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DrawingId",
                table: "DrawingParts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsManualEntry",
                table: "DrawingParts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "DrawingParts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParts_OrderId",
                table: "DrawingParts",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrawingParts_Orders_OrderId",
                table: "DrawingParts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawingParts_Orders_OrderId",
                table: "DrawingParts");

            migrationBuilder.DropIndex(
                name: "IX_DrawingParts_OrderId",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "IsManualEntry",
                table: "DrawingParts");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "DrawingParts");

            migrationBuilder.AlterColumn<int>(
                name: "DrawingId",
                table: "DrawingParts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
