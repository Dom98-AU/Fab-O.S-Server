#!/usr/bin/env python3
"""
Import ALL catalogue items from Excel - generates SQL statements to be executed via SQL MCP
"""

import pandas as pd
import sys

EXCEL_FILE = '/mnt/c/Fab.OS Platform/AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx'

def clean_sql_value(val):
    """Clean value for SQL insertion"""
    if pd.isna(val) or val == '':
        return 'NULL'
    elif isinstance(val, str):
        # Escape single quotes
        return "'" + str(val).replace("'", "''").strip()[:500] + "'"
    elif isinstance(val, (int, float)):
        return str(round(val, 4))
    return 'NULL'

def get_category_from_sheet(sheet_name):
    """Map sheet name to category"""
    mappings = {
        'Cold_Rolled_Sheet': 'Cold Rolled Sheet',
        'Equal_Angles': 'Equal Angles',
        'Floor_Plates': 'Floor Plates',
        'Tread_Plates': 'Floor Plates',
        'Galvanized_Sheet': 'Galvanized Sheet',
        'Grating': 'Grating',
        'Merchant_Flats': 'Flat Bars',
        'Flat_Bars': 'Flat Bars',
        'PFC_Channels': 'PFC Channels',
        'Channels': 'Channels',
        'Pipes': 'Pipes',
        'CHS': 'CHS',
        'Plates': 'Plates',
        'RHS_Rectangular': 'RHS',
        'Round_Bars': 'Round Bars',
        'SHS_Square': 'SHS',
        'Square_Bars': 'Square Bars',
        'Hex_Bars': 'Hex Bars',
        'Unequal_Angles': 'Unequal Angles',
        'Universal_Beams': 'Universal Beams',
        'Universal_Columns': 'Universal Columns',
        'Round_Tubes': 'Round Tubes',
        'Rectangular': 'RHS',
        'Square_Tubes': 'SHS',
        'Seamless_Tubes': 'Seamless Tubes',
        'Food_Grade_Tubes': 'Food Grade Tubes',
        'Hollow_Bars': 'Hollow Bars',
        'Welded_Handrail_Tubes': 'Handrail Tubes',
        'Half_Rounds': 'Half Rounds',
        'Tees': 'Tees',
        'Zeds': 'Zeds',
        'Fasteners': 'Fasteners',
        'C_Purlins': 'C Purlins',
        'Z_Purlins': 'Z Purlins',
        'Bridging': 'Purlin Bridging'
    }

    # Extract material prefix
    if sheet_name.startswith('F_MS_'):
        material = 'Mild Steel'
        category_part = sheet_name[5:]
    elif sheet_name.startswith('F_SS_'):
        material = 'Stainless Steel'
        category_part = sheet_name[5:]
    elif sheet_name.startswith('NF_AL_'):
        material = 'Aluminum'
        category_part = sheet_name[6:]
    else:
        material = 'Other'
        category_part = sheet_name

    # Get category
    for key, cat in mappings.items():
        if key in category_part:
            return material, cat

    return material, category_part.replace('_', ' ')

def process_sheet(sheet_name, df):
    """Process a single sheet and return SQL insert values"""

    if sheet_name == 'Import_Mappings':
        return []

    material, category = get_category_from_sheet(sheet_name)

    # For stainless steel plates, append grade to category
    if 'Plates_' in sheet_name and material == 'Stainless Steel':
        grade = sheet_name.split('Plates_')[1]
        category = f'SS {grade} Plates'

    values = []

    for _, row in df.iterrows():
        try:
            # Get item code - skip if empty
            item_code = row.get('ItemCode', row.get('Item_Code', row.get('Code', '')))
            if pd.isna(item_code) or str(item_code).strip() == '':
                continue

            item_code = clean_sql_value(item_code)

            # Get material from row or use sheet default
            row_material = row.get('Material', material)
            material_val = clean_sql_value(row_material)

            # Description
            desc = row.get('Description', row.get('Name', ''))
            description = clean_sql_value(desc)

            # Dimensions
            width = clean_sql_value(row.get('Width_mm', row.get('Width', None)))
            length = clean_sql_value(row.get('Length_mm', row.get('Length', None)))
            thickness = clean_sql_value(row.get('Thickness_mm', row.get('Thickness', row.get('Wall_mm', None))))
            depth = clean_sql_value(row.get('Depth_mm', row.get('Height_mm', row.get('Depth', None))))
            diameter = clean_sql_value(row.get('Diameter_mm', row.get('OD_mm', row.get('Diameter', row.get('OD mm', None)))))

            # Web and flange for structural sections
            web_thickness = clean_sql_value(row.get('Web_Thickness_mm', row.get('tw_mm', row.get('Web mm', row.get('tw', None)))))
            flange_thickness = clean_sql_value(row.get('Flange_Thickness_mm', row.get('tf_mm', row.get('Flange mm', row.get('tf', None)))))

            # Weights
            weight = clean_sql_value(row.get('Weight_kg', row.get('Weight', row.get('Mass', None))))
            mass_kg_m = clean_sql_value(row.get('Mass_kg_m', row.get('kg_m', row.get('kg/m', row.get('Mass kg/m', None)))))
            mass_kg_m2 = clean_sql_value(row.get('Mass_kg_m2', row.get('kg_m2', row.get('kg/m2', row.get('Mass kg/m2', None)))))

            # Calculate mass_kg_m2 for plates if not provided
            if mass_kg_m2 == 'NULL' and category == 'Plates':
                if width != 'NULL' and length != 'NULL' and weight != 'NULL':
                    try:
                        w = float(width)
                        l = float(length)
                        wt = float(weight)
                        area_m2 = (w * l) / 1000000
                        if area_m2 > 0:
                            mass_kg_m2 = str(round(wt / area_m2, 2))
                    except:
                        pass

            # Additional properties
            grade = clean_sql_value(row.get('Grade', row.get('Steel_Grade', None)))
            standard = clean_sql_value(row.get('Standard', row.get('Specification', None)))
            finish = clean_sql_value(row.get('Finish', row.get('Surface_Finish', None)))

            # Build the value tuple
            value = f"({item_code}, {material_val}, '{category}', {description}, {width}, {length}, {thickness}, {depth}, {diameter}, {web_thickness}, {flange_thickness}, {weight}, {mass_kg_m}, {mass_kg_m2}, {grade}, {standard}, {finish}, 1)"

            values.append(value)

        except Exception as e:
            print(f"Error in {sheet_name} row: {e}", file=sys.stderr)
            continue

    return values

def main():
    """Main processing function"""

    try:
        excel_file = pd.ExcelFile(EXCEL_FILE)
        sheets = excel_file.sheet_names
        print(f"Found {len(sheets)} sheets to process", file=sys.stderr)
    except Exception as e:
        print(f"Error reading Excel: {e}", file=sys.stderr)
        return

    total_items = 0
    batch_size = 50  # Insert 50 items at a time

    for sheet_name in sheets:
        try:
            print(f"\n-- Processing: {sheet_name}", file=sys.stderr)
            df = pd.read_excel(EXCEL_FILE, sheet_name=sheet_name)

            values = process_sheet(sheet_name, df)

            if values:
                # Output SQL in batches
                for i in range(0, len(values), batch_size):
                    batch = values[i:i+batch_size]

                    print(f"\n-- {sheet_name} batch {i//batch_size + 1}")
                    print("INSERT INTO dbo.CatalogueItems")
                    print("(ItemCode, Material, Category, Description, Width_mm, Length_mm, Thickness_mm, Depth_mm, Diameter_mm, Web_Thickness_mm, Flange_Thickness_mm, Weight_kg, Mass_kg_m, Mass_kg_m2, Grade, Standard, Finish, CompanyId)")
                    print("VALUES")
                    print(",\n".join(batch))
                    print(";")

                    total_items += len(batch)

            print(f"  Processed {len(values)} items from {sheet_name}", file=sys.stderr)

        except Exception as e:
            print(f"Error processing {sheet_name}: {e}", file=sys.stderr)

    print(f"\n-- Total items to insert: {total_items}", file=sys.stderr)

if __name__ == "__main__":
    main()