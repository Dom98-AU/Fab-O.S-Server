-- Make CreatedById nullable in Customers table
ALTER TABLE [dbo].[Customers]
ALTER COLUMN [CreatedById] INT NULL;
