using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserCompanyIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // STEP 1: Ensure default company exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Companies WHERE Id = 1)
                BEGIN
                    SET IDENTITY_INSERT Companies ON;
                    INSERT INTO Companies (Id, Name, ShortName, Code, IsActive, SubscriptionLevel, MaxUsers, CreatedDate, LastModified)
                    VALUES (1, 'Default Company', 'DEFAULT', 'DEF', 1, 'Standard', 50, GETUTCDATE(), GETUTCDATE());
                    SET IDENTITY_INSERT Companies OFF;
                END
            ");

            // STEP 2: Update all existing users with NULL CompanyId to default company (ID = 1)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET CompanyId = 1
                WHERE CompanyId IS NULL
            ");

            // STEP 3: Make CompanyId column NOT NULL with default value of 1
            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert CompanyId to nullable
            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Users",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: false,
                oldDefaultValue: 1);
        }
    }
}
