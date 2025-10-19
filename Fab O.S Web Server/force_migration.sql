-- Manually mark our migration as applied in the migration history
-- This tells EF that the migration has already been run
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20250930231103_AddMissingSchemaChanges', '8.0.8');