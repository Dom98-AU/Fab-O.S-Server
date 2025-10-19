#!/bin/bash
# Run the TakeoffRevisions migration SQL script

# Get connection string from appsettings
CONNECTION_STRING=$(grep -A 1 "DefaultConnection" "appsettings.Development.json" 2>/dev/null || grep -A 1 "DefaultConnection" "appsettings.json")

# Extract the SQL migration and run it via sqlcmd
# Note: This assumes you have the connection details in your environment or need to provide them

echo "Running TakeoffRevisions migration..."
echo "Note: You may need to provide database credentials"

# The migration SQL is in apply_revision_only.sql
# We need to execute it using sqlcmd or through the application
cat apply_revision_only.sql
