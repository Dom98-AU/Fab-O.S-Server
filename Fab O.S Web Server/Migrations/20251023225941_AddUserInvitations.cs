using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FabOS.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if the foreign key exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Companies_CompanyId' AND parent_object_id = OBJECT_ID('Users'))
                BEGIN
                    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Companies_CompanyId];
                END
            ");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PdfAnnotations') AND name = 'CoordinatesData')
                    ALTER TABLE [PdfAnnotations] ADD [CoordinatesData] nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PdfAnnotations') AND name = 'MeasurementPrecision')
                    ALTER TABLE [PdfAnnotations] ADD [MeasurementPrecision] nvarchar(20) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PdfAnnotations') AND name = 'MeasurementScale')
                    ALTER TABLE [PdfAnnotations] ADD [MeasurementScale] nvarchar(500) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PdfAnnotations') AND name = 'MeasurementValue')
                    ALTER TABLE [PdfAnnotations] ADD [MeasurementValue] nvarchar(100) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PackageDrawings') AND name = 'ProviderFileId')
                    ALTER TABLE [PackageDrawings] ADD [ProviderFileId] nvarchar(500) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PackageDrawings') AND name = 'ProviderMetadata')
                    ALTER TABLE [PackageDrawings] ADD [ProviderMetadata] nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PackageDrawings') AND name = 'StorageProvider')
                    ALTER TABLE [PackageDrawings] ADD [StorageProvider] nvarchar(50) NULL;
            ");

            // Create PdfEditLocks table only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PdfEditLocks')
                BEGIN
                    CREATE TABLE [PdfEditLocks] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PackageDrawingId] int NOT NULL,
                        [SessionId] nvarchar(255) NOT NULL,
                        [UserId] int NOT NULL,
                        [UserName] nvarchar(255) NOT NULL,
                        [LockedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
                        [LastHeartbeat] datetime2 NOT NULL DEFAULT (getutcdate()),
                        [LastActivityAt] datetime2 NOT NULL DEFAULT (getutcdate()),
                        [IsActive] bit NOT NULL DEFAULT 1,
                        CONSTRAINT [PK_PdfEditLocks] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PdfEditLocks_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_PdfEditLocks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
                    );
                END
            ");

            // Create UserInvitations table only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserInvitations')
                BEGIN
                    CREATE TABLE [UserInvitations] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Email] nvarchar(255) NOT NULL,
                        [InvitedByUserId] int NOT NULL,
                        [CompanyId] int NOT NULL,
                        [Token] nvarchar(50) NOT NULL,
                        [Status] int NOT NULL,
                        [AuthMethod] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ExpiresAt] datetime2 NOT NULL,
                        [AcceptedAt] datetime2 NULL,
                        [InvitedById] int NULL,
                        CONSTRAINT [PK_UserInvitations] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserInvitations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserInvitations_Users_InvitedById] FOREIGN KEY ([InvitedById]) REFERENCES [Users] ([Id])
                    );
                END
            ");

            // Create indexes for PdfEditLocks if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PdfEditLocks_LastHeartbeat' AND object_id = OBJECT_ID('PdfEditLocks'))
                    CREATE INDEX [IX_PdfEditLocks_LastHeartbeat] ON [PdfEditLocks] ([LastHeartbeat]) WHERE [IsActive] = 1;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PdfEditLocks_PackageDrawingId_IsActive' AND object_id = OBJECT_ID('PdfEditLocks'))
                    CREATE INDEX [IX_PdfEditLocks_PackageDrawingId_IsActive] ON [PdfEditLocks] ([PackageDrawingId], [IsActive]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PdfEditLocks_SessionId' AND object_id = OBJECT_ID('PdfEditLocks'))
                    CREATE INDEX [IX_PdfEditLocks_SessionId] ON [PdfEditLocks] ([SessionId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PdfEditLocks_UserId' AND object_id = OBJECT_ID('PdfEditLocks'))
                    CREATE INDEX [IX_PdfEditLocks_UserId] ON [PdfEditLocks] ([UserId]);
            ");

            // Create indexes for UserInvitations if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserInvitations_CompanyId' AND object_id = OBJECT_ID('UserInvitations'))
                    CREATE INDEX [IX_UserInvitations_CompanyId] ON [UserInvitations] ([CompanyId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserInvitations_InvitedById' AND object_id = OBJECT_ID('UserInvitations'))
                    CREATE INDEX [IX_UserInvitations_InvitedById] ON [UserInvitations] ([InvitedById]);
            ");

            // Add foreign key with NO ACTION to avoid cascade cycles
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Companies_CompanyId' AND parent_object_id = OBJECT_ID('Users'))
                BEGIN
                    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Companies_CompanyId]
                    FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if the foreign key exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Companies_CompanyId' AND parent_object_id = OBJECT_ID('Users'))
                BEGIN
                    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Companies_CompanyId];
                END
            ");

            migrationBuilder.DropTable(
                name: "PdfEditLocks");

            migrationBuilder.DropTable(
                name: "UserInvitations");

            migrationBuilder.DropColumn(
                name: "CoordinatesData",
                table: "PdfAnnotations");

            migrationBuilder.DropColumn(
                name: "MeasurementPrecision",
                table: "PdfAnnotations");

            migrationBuilder.DropColumn(
                name: "MeasurementScale",
                table: "PdfAnnotations");

            migrationBuilder.DropColumn(
                name: "MeasurementValue",
                table: "PdfAnnotations");

            migrationBuilder.DropColumn(
                name: "ProviderFileId",
                table: "PackageDrawings");

            migrationBuilder.DropColumn(
                name: "ProviderMetadata",
                table: "PackageDrawings");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "PackageDrawings");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Users",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
