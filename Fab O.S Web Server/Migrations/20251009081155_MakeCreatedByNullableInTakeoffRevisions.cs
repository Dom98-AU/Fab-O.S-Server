using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class MakeCreatedByNullableInTakeoffRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TakeoffRevisions_Users_CreatedBy",
                table: "TakeoffRevisions");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "TakeoffRevisions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_TakeoffRevisions_Users_CreatedBy",
                table: "TakeoffRevisions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TakeoffRevisions_Users_CreatedBy",
                table: "TakeoffRevisions");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "TakeoffRevisions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TakeoffRevisions_Users_CreatedBy",
                table: "TakeoffRevisions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
