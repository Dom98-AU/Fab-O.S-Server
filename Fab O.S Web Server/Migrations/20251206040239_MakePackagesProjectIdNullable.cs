using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class MakePackagesProjectIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key constraint first if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Packages_Projects_ProjectId')
                BEGIN
                    ALTER TABLE [Packages] DROP CONSTRAINT [FK_Packages_Projects_ProjectId];
                END
            ");

            // Make ProjectId nullable
            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Packages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Re-add the foreign key allowing NULL values
            migrationBuilder.Sql(@"
                ALTER TABLE [Packages]
                ADD CONSTRAINT [FK_Packages_Projects_ProjectId]
                FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This would require setting a default ProjectId for all NULL values first
            // Not implementing full rollback as this is a one-way migration
        }
    }
}
