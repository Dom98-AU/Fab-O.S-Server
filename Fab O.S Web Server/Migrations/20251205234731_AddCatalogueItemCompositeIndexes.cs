using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogueItemCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CatalogueItems_CompanyId",
                table: "CatalogueItems");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_CompanyId_Category",
                table: "CatalogueItems",
                columns: new[] { "CompanyId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_CompanyId_Material",
                table: "CatalogueItems",
                columns: new[] { "CompanyId", "Material" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_CompanyId_Material_Category",
                table: "CatalogueItems",
                columns: new[] { "CompanyId", "Material", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CatalogueItems_CompanyId_Category",
                table: "CatalogueItems");

            migrationBuilder.DropIndex(
                name: "IX_CatalogueItems_CompanyId_Material",
                table: "CatalogueItems");

            migrationBuilder.DropIndex(
                name: "IX_CatalogueItems_CompanyId_Material_Category",
                table: "CatalogueItems");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogueItems_CompanyId",
                table: "CatalogueItems",
                column: "CompanyId");
        }
    }
}
