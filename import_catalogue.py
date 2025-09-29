#!/usr/bin/env python3
"""
Import all catalogue items from Excel file to SQL Server database
"""

import pandas as pd
import pyodbc
import os
from datetime import datetime

# Database connection string - update with your credentials
CONNECTION_STRING = """
Driver={ODBC Driver 17 for SQL Server};
Server=tcp:sql-server-steel-estimation-sandbox.database.windows.net,1433;
Database=sqldb-steel-estimation-sandbox;
Uid=sqladmin;
Pwd=MYHYPERSTR0NGKEYK3Y?!;
Encrypt=yes;
TrustServerCertificate=no;
Connection Timeout=30;
"""

# Excel file path
EXCEL_FILE = '/mnt/c/Fab.OS Platform/AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx'

def clean_value(val):
    """Clean and prepare values for SQL insertion"""
    if pd.isna(val) or val == '' or val == 'nan':
        return 'NULL'
    elif isinstance(val, str):
        # Escape single quotes and clean string
        return "'" + str(val).replace("'", "''").strip() + "'"
    elif isinstance(val, (int, float)):
        if pd.isna(val):
            return 'NULL'
        return str(val)
    else:
        return "'" + str(val) + "'"

def get_category_from_sheet_name(sheet_name):
    """Extract category from sheet name"""
    if 'Plates' in sheet_name:
        return 'Plates'
    elif 'Equal_Angles' in sheet_name:
        return 'Equal Angles'
    elif 'Unequal_Angles' in sheet_name:
        return 'Unequal Angles'
    elif 'Universal_Beams' in sheet_name:
        return 'Universal Beams'
    elif 'Universal_Columns' in sheet_name:
        return 'Universal Columns'
    elif 'Round_Bars' in sheet_name:
        return 'Round Bars'
    elif 'Square_Bars' in sheet_name:
        return 'Square Bars'
    elif 'Hex_Bars' in sheet_name:
        return 'Hex Bars'
    elif 'Flat_Bars' in sheet_name or 'Merchant_Flats' in sheet_name:
        return 'Flat Bars'
    elif 'SHS' in sheet_name or 'Square' in sheet_name and 'Tubes' in sheet_name:
        return 'SHS'
    elif 'RHS' in sheet_name or 'Rectangular' in sheet_name:
        return 'RHS'
    elif 'CHS' in sheet_name or 'Round_Tubes' in sheet_name or 'Pipes' in sheet_name:
        return 'CHS'
    elif 'Channels' in sheet_name or 'PFC' in sheet_name:
        return 'Channels'
    elif 'Grating' in sheet_name:
        return 'Grating'
    elif 'Floor_Plates' in sheet_name or 'Tread_Plates' in sheet_name:
        return 'Floor Plates'
    elif 'Sheet' in sheet_name:
        return 'Sheet'
    elif 'Purlins' in sheet_name:
        return 'Purlins'
    elif 'Fasteners' in sheet_name:
        return 'Fasteners'
    else:
        return 'Other'

def process_sheet(sheet_name, df, cursor):
    """Process a single sheet and insert into database"""

    print(f"\nProcessing sheet: {sheet_name} ({len(df)} rows)")

    if sheet_name == 'Import_Mappings':
        print("  Skipping Import_Mappings sheet")
        return 0

    category = get_category_from_sheet_name(sheet_name)
    inserted_count = 0

    # Process in batches of 100
    batch_size = 100

    for i in range(0, len(df), batch_size):
        batch = df.iloc[i:i+batch_size]
        values_list = []

        for _, row in batch.iterrows():
            # Map columns - handle various column names
            item_code = clean_value(row.get('ItemCode', row.get('Item_Code', row.get('Code', ''))))

            if item_code == 'NULL' or item_code == "''":
                continue  # Skip rows without item code

            material = clean_value(row.get('Material', row.get('Grade', 'Steel')))
            description = clean_value(row.get('Description', row.get('Name', '')))

            # Dimensions
            width_mm = clean_value(row.get('Width_mm', row.get('Width', None)))
            length_mm = clean_value(row.get('Length_mm', row.get('Length', None)))
            thickness_mm = clean_value(row.get('Thickness_mm', row.get('Thickness', None)))
            depth_mm = clean_value(row.get('Depth_mm', row.get('Height_mm', None)))
            diameter_mm = clean_value(row.get('Diameter_mm', row.get('OD_mm', None)))

            # Weights
            weight_kg = clean_value(row.get('Weight_kg', row.get('Weight', None)))
            mass_kg_m = clean_value(row.get('Mass_kg_m', row.get('kg_m', None)))
            mass_kg_m2 = clean_value(row.get('Mass_kg_m2', row.get('kg_m2', None)))

            # Additional properties
            grade = clean_value(row.get('Grade', row.get('Steel_Grade', None)))
            standard = clean_value(row.get('Standard', row.get('Specification', None)))
            finish = clean_value(row.get('Finish', row.get('Surface_Finish', None)))

            # Build values string
            values = f"""(
                {item_code}, {material}, '{category}', {description},
                {width_mm}, {length_mm}, {thickness_mm}, {depth_mm}, {diameter_mm},
                {weight_kg}, {mass_kg_m}, {mass_kg_m2},
                {grade}, {standard}, {finish},
                1, GETUTCDATE()
            )"""

            values_list.append(values)

        if values_list:
            # Insert batch
            sql = f"""
            INSERT INTO dbo.CatalogueItems (
                ItemCode, Material, Category, Description,
                Width_mm, Length_mm, Thickness_mm, Depth_mm, Diameter_mm,
                Weight_kg, Mass_kg_m, Mass_kg_m2,
                Grade, Standard, Finish,
                CompanyId, CreatedDate
            ) VALUES {','.join(values_list)}
            """

            try:
                cursor.execute(sql)
                cursor.commit()
                inserted_count += len(values_list)
                print(f"  Inserted batch: {i+1} to {min(i+batch_size, len(df))} ({len(values_list)} items)")
            except Exception as e:
                print(f"  Error in batch {i//batch_size + 1}: {str(e)[:100]}")
                # Try individual inserts for this batch
                for value in values_list:
                    try:
                        sql_single = f"""
                        INSERT INTO dbo.CatalogueItems (
                            ItemCode, Material, Category, Description,
                            Width_mm, Length_mm, Thickness_mm, Depth_mm, Diameter_mm,
                            Weight_kg, Mass_kg_m, Mass_kg_m2,
                            Grade, Standard, Finish,
                            CompanyId, CreatedDate
                        ) VALUES {value}
                        """
                        cursor.execute(sql_single)
                        cursor.commit()
                        inserted_count += 1
                    except Exception as e2:
                        pass  # Skip failed individual inserts

    print(f"  Total inserted from {sheet_name}: {inserted_count}")
    return inserted_count

def main():
    """Main import function"""
    print("Starting catalogue import...")
    print(f"Reading from: {EXCEL_FILE}")

    # Connect to database
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        cursor = conn.cursor()
        print("Connected to database successfully")
    except Exception as e:
        print(f"Failed to connect to database: {e}")
        return

    # Read Excel file
    try:
        excel_file = pd.ExcelFile(EXCEL_FILE)
        print(f"Found {len(excel_file.sheet_names)} sheets in Excel file")
    except Exception as e:
        print(f"Failed to read Excel file: {e}")
        return

    total_inserted = 0

    # Process each sheet
    for sheet_name in excel_file.sheet_names:
        try:
            df = pd.read_excel(EXCEL_FILE, sheet_name=sheet_name)
            inserted = process_sheet(sheet_name, df, cursor)
            total_inserted += inserted
        except Exception as e:
            print(f"Error processing sheet {sheet_name}: {e}")
            continue

    # Verify final count
    cursor.execute("SELECT COUNT(*) FROM dbo.CatalogueItems")
    final_count = cursor.fetchone()[0]

    print(f"\n" + "="*60)
    print(f"Import completed!")
    print(f"Total items inserted: {total_inserted}")
    print(f"Final count in database: {final_count}")
    print("="*60)

    # Close connection
    cursor.close()
    conn.close()

if __name__ == "__main__":
    main()