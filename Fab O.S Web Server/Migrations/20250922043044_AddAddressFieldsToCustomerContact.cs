using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressFieldsToCustomerContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "CustomerContacts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "CustomerContacts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CustomerContacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "CustomerContacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedAddress",
                table: "CustomerContacts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GooglePlaceId",
                table: "CustomerContacts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InheritCustomerAddress",
                table: "CustomerContacts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "CustomerContacts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "CustomerContacts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "FormattedAddress",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "GooglePlaceId",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "InheritCustomerAddress",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "CustomerContacts");
        }
    }
}
