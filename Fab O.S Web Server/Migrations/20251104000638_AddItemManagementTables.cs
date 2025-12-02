using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddItemManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ProductLicenses table already exists in database - skip creation
            // It was created by a previous migration that was deleted

            // NOTE: This migration is currently empty because EF Core didn't detect
            // the missing tables (Assemblies, InventoryItems, PurchaseOrders, etc.)
            // They need to be added manually or through a new migration after
            // ProductLicenses is properly tracked in migration history.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to drop - ProductLicenses was not created by this migration
        }
    }
}
