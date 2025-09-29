#!/usr/bin/env python3
"""
Generate SQL INSERT statements for catalogue items from Excel file
"""

import pandas as pd
import sys
import os

EXCEL_FILE = '/mnt/c/Fab.OS Platform/AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx'

def clean_value(val):
    """Clean and prepare values for SQL insertion"""
    if pd.isna(val) or val == '' or val == 'nan':
        return 'NULL'
    elif isinstance(val, str):
        # Escape single quotes and clean string
        return "N'" + str(val).replace("'", "''").strip()[:500] + "'"
    elif isinstance(val, (int, float)):
        if pd.isna(val):
            return 'NULL'
        return str(val)
    else:
        return "N'" + str(val) + "'"

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
    elif 'SHS' in sheet_name or ('Square' in sheet_name and 'Tubes' in sheet_name):
        return 'SHS'
    elif 'RHS' in sheet_name or 'Rectangular' in sheet_name:
        return 'RHS'
    elif 'CHS' in sheet_name or 'Round_Tubes' in sheet_name or 'Pipes' in sheet_name:
        return 'CHS/Pipes'
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
    elif 'Bridging' in sheet_name:
        return 'Bridging'
    elif 'Fasteners' in sheet_name:
        return 'Fasteners'
    elif 'Seamless' in sheet_name:
        return 'Seamless Tubes'
    elif 'Handrail' in sheet_name:
        return 'Handrail'
    elif 'Food_Grade' in sheet_name:
        return 'Food Grade'
    elif 'Hollow' in sheet_name:
        return 'Hollow Bars'
    elif 'Half_Rounds' in sheet_name:
        return 'Half Rounds'
    elif 'Tees' in sheet_name:
        return 'Tees'
    elif 'Zeds' in sheet_name:
        return 'Zeds'
    else:
        return 'Other'

def process_sheet(sheet_name, df):
    """Process a single sheet and generate SQL statements"""

    if sheet_name == 'Import_Mappings':
        return []

    category = get_category_from_sheet_name(sheet_name)
    sql_statements = []

    # Clean column names
    df.columns = df.columns.str.strip()

    for _, row in df.iterrows():
        try:
            # Map columns
            item_code = row.get('ItemCode', row.get('Item_Code', row.get('Code', row.get('Item Code', ''))))

            if pd.isna(item_code) or str(item_code).strip() == '':
                continue

            item_code = clean_value(item_code)
            material = clean_value(row.get('Material', row.get('Grade', 'Steel')))
            description = clean_value(row.get('Description', row.get('Name', '')))

            # Dimensions
            width_mm = clean_value(row.get('Width_mm', row.get('Width', row.get('Width mm', None))))
            length_mm = clean_value(row.get('Length_mm', row.get('Length', row.get('Length mm', None))))
            thickness_mm = clean_value(row.get('Thickness_mm', row.get('Thickness', row.get('Wall Thickness', None))))
            depth_mm = clean_value(row.get('Depth_mm', row.get('Height_mm', row.get('Depth', None))))
            diameter_mm = clean_value(row.get('Diameter_mm', row.get('OD_mm', row.get('Diameter', None))))

            # Handle special columns for sections
            if pd.notna(row.get('OD mm', None)):
                diameter_mm = clean_value(row.get('OD mm'))
            if pd.notna(row.get('Wall mm', None)):
                thickness_mm = clean_value(row.get('Wall mm'))

            # Weights
            weight_kg = clean_value(row.get('Weight_kg', row.get('Weight', row.get('Weight kg', None))))
            mass_kg_m = clean_value(row.get('Mass_kg_m', row.get('kg_m', row.get('kg/m', row.get('Mass kg/m', None)))))
            mass_kg_m2 = clean_value(row.get('Mass_kg_m2', row.get('kg_m2', row.get('kg/m2', row.get('Mass kg/m2', None)))))

            # Additional properties
            grade = clean_value(row.get('Grade', row.get('Steel_Grade', None)))
            standard = clean_value(row.get('Standard', row.get('Specification', None)))
            finish = clean_value(row.get('Finish', row.get('Surface_Finish', row.get('Surface Finish', None))))

            # Special handling for structural sections
            web_thickness = clean_value(row.get('Web_Thickness_mm', row.get('tw mm', row.get('Web mm', None))))
            flange_thickness = clean_value(row.get('Flange_Thickness_mm', row.get('tf mm', row.get('Flange mm', None))))

            sql = f"""INSERT INTO dbo.CatalogueItems (
ItemCode, Material, Category, Description,
Width_mm, Length_mm, Thickness_mm, Depth_mm, Diameter_mm,
Weight_kg, Mass_kg_m, Mass_kg_m2,
Web_Thickness_mm, Flange_Thickness_mm,
Grade, Standard, Finish,
CompanyId, CreatedDate
) VALUES (
{item_code}, {material}, N'{category}', {description},
{width_mm}, {length_mm}, {thickness_mm}, {depth_mm}, {diameter_mm},
{weight_kg}, {mass_kg_m}, {mass_kg_m2},
{web_thickness}, {flange_thickness},
{grade}, {standard}, {finish},
1, GETUTCDATE()
);"""

            sql_statements.append(sql)

        except Exception as e:
            print(f"Error processing row in {sheet_name}: {e}", file=sys.stderr)
            continue

    return sql_statements

def main():
    """Main function to generate SQL statements"""

    # Read Excel file
    try:
        excel_file = pd.ExcelFile(EXCEL_FILE)
        print(f"-- Found {len(excel_file.sheet_names)} sheets in Excel file", file=sys.stderr)
    except Exception as e:
        print(f"-- Failed to read Excel file: {e}", file=sys.stderr)
        return

    total_statements = 0

    # Process each sheet
    for sheet_name in excel_file.sheet_names:
        try:
            df = pd.read_excel(EXCEL_FILE, sheet_name=sheet_name)
            statements = process_sheet(sheet_name, df)

            if statements:
                print(f"\n-- Sheet: {sheet_name} ({len(statements)} items)")
                for stmt in statements:
                    print(stmt)

                total_statements += len(statements)

        except Exception as e:
            print(f"-- Error processing sheet {sheet_name}: {e}", file=sys.stderr)
            continue

    print(f"\n-- Total SQL statements generated: {total_statements}", file=sys.stderr)

if __name__ == "__main__":
    main()