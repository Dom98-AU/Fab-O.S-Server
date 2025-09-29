-- Catalogue Items Seed Data Script
-- This script populates the CatalogueItems table with the AU/NZ catalogue data
-- Total items: 7,107

-- Ensure we have a default company
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Companies ON;
    INSERT INTO Companies (Id, Name, ShortName, Code, IsActive, CreatedDate)
    VALUES (1, 'Default Company', 'DEFAULT', 'DEF', 1, GETUTCDATE());
    SET IDENTITY_INSERT Companies OFF;
END

-- Clear existing catalogue items if needed (optional - comment out if you want to preserve existing data)
-- DELETE FROM CatalogueItems WHERE CompanyId = 1;

-- Insert Catalogue Items
-- Note: Due to the large number of items (7,107), these should be imported using the Excel import service
-- or through a bulk insert operation. Here are some sample entries to demonstrate the structure:

INSERT INTO CatalogueItems (
    ItemCode, Description, Category, Material, Profile,
    Length_mm, Width_mm, Thickness_mm, Mass_kg_m,
    Standard, Grade, Finish, Unit, IsActive, CreatedDate, CompanyId
) VALUES
-- PLATES - Mild Steel
('PLT-MS-6MM', '6mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 6, 56.52,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-8MM', '8mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 8, 75.36,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-10MM', '10mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 10, 94.2,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-12MM', '12mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 12, 113.04,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-16MM', '16mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 16, 150.72,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-20MM', '20mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 20, 188.4,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

('PLT-MS-25MM', '25mm Mild Steel Plate', 'Plates', 'Mild Steel', NULL,
 2400, 1200, 25, 235.5,
 'AS/NZS 3678', '250', 'Black/Mill', 'SHEET', 1, GETUTCDATE(), 1),

-- HOLLOW SECTIONS - Square (SHS)
('SHS-50X50X3', '50x50x3.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '50x50x3.0SHS',
 6000, 50, 3, 4.25,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('SHS-75X75X3', '75x75x3.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '75x75x3.0SHS',
 6000, 75, 3, 6.60,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('SHS-100X100X4', '100x100x4.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '100x100x4.0SHS',
 6000, 100, 4, 11.90,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('SHS-100X100X5', '100x100x5.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '100x100x5.0SHS',
 6000, 100, 5, 14.70,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('SHS-150X150X5', '150x150x5.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '150x150x5.0SHS',
 8000, 150, 5, 22.70,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('SHS-200X200X6', '200x200x6.0 Square Hollow Section', 'Hollow Sections', 'Mild Steel', '200x200x6.0SHS',
 8000, 200, 6, 36.60,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

-- HOLLOW SECTIONS - Rectangular (RHS)
('RHS-50X25X3', '50x25x3.0 Rectangular Hollow Section', 'Hollow Sections', 'Mild Steel', '50x25x3.0RHS',
 6000, 50, 3, 3.07,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('RHS-75X50X3', '75x50x3.0 Rectangular Hollow Section', 'Hollow Sections', 'Mild Steel', '75x50x3.0RHS',
 6000, 75, 3, 5.42,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('RHS-100X50X4', '100x50x4.0 Rectangular Hollow Section', 'Hollow Sections', 'Mild Steel', '100x50x4.0RHS',
 6000, 100, 4, 8.96,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('RHS-150X50X5', '150x50x5.0 Rectangular Hollow Section', 'Hollow Sections', 'Mild Steel', '150x50x5.0RHS',
 8000, 150, 5, 14.70,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('RHS-200X100X6', '200x100x6.0 Rectangular Hollow Section', 'Hollow Sections', 'Mild Steel', '200x100x6.0RHS',
 8000, 200, 6, 27.30,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

-- CIRCULAR HOLLOW SECTIONS (CHS/Pipe)
('CHS-21.3X2.6', 'NB15 (21.3 OD) x 2.6 CHS', 'Pipes', 'Mild Steel', '21.3CHS',
 6000, NULL, 2.6, 1.27,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-26.9X2.6', 'NB20 (26.9 OD) x 2.6 CHS', 'Pipes', 'Mild Steel', '26.9CHS',
 6000, NULL, 2.6, 1.63,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-33.7X3.2', 'NB25 (33.7 OD) x 3.2 CHS', 'Pipes', 'Mild Steel', '33.7CHS',
 6000, NULL, 3.2, 2.50,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-42.4X3.2', 'NB32 (42.4 OD) x 3.2 CHS', 'Pipes', 'Mild Steel', '42.4CHS',
 6000, NULL, 3.2, 3.20,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-48.3X3.2', 'NB40 (48.3 OD) x 3.2 CHS', 'Pipes', 'Mild Steel', '48.3CHS',
 6000, NULL, 3.2, 3.68,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-60.3X3.6', 'NB50 (60.3 OD) x 3.6 CHS', 'Pipes', 'Mild Steel', '60.3CHS',
 6000, NULL, 3.6, 5.19,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-88.9X4.0', 'NB80 (88.9 OD) x 4.0 CHS', 'Pipes', 'Mild Steel', '88.9CHS',
 6000, NULL, 4.0, 8.63,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

('CHS-114.3X4.5', 'NB100 (114.3 OD) x 4.5 CHS', 'Pipes', 'Mild Steel', '114.3CHS',
 6000, NULL, 4.5, 12.50,
 'AS/NZS 1163', 'C350L0', 'Black', 'LENGTH', 1, GETUTCDATE(), 1),

-- UNIVERSAL BEAMS (UB)
('UB-150X14', '150UB14.0 Universal Beam', 'Beams', 'Mild Steel', '150UB',
 9000, 150, NULL, 14.0,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-150X18', '150UB18.0 Universal Beam', 'Beams', 'Mild Steel', '150UB',
 9000, 150, NULL, 18.0,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-180X16', '180UB16.1 Universal Beam', 'Beams', 'Mild Steel', '180UB',
 9000, 180, NULL, 16.1,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-180X18', '180UB18.1 Universal Beam', 'Beams', 'Mild Steel', '180UB',
 9000, 180, NULL, 18.1,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-200X18', '200UB18.2 Universal Beam', 'Beams', 'Mild Steel', '200UB',
 9000, 200, NULL, 18.2,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-200X22', '200UB22.3 Universal Beam', 'Beams', 'Mild Steel', '200UB',
 9000, 200, NULL, 22.3,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-200X25', '200UB25.4 Universal Beam', 'Beams', 'Mild Steel', '200UB',
 9000, 200, NULL, 25.4,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-250X25', '250UB25.7 Universal Beam', 'Beams', 'Mild Steel', '250UB',
 12000, 250, NULL, 25.7,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-250X31', '250UB31.4 Universal Beam', 'Beams', 'Mild Steel', '250UB',
 12000, 250, NULL, 31.4,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-310X32', '310UB32.0 Universal Beam', 'Beams', 'Mild Steel', '310UB',
 12000, 310, NULL, 32.0,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-310X40', '310UB40.4 Universal Beam', 'Beams', 'Mild Steel', '310UB',
 12000, 310, NULL, 40.4,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UB-360X44', '360UB44.7 Universal Beam', 'Beams', 'Mild Steel', '360UB',
 12000, 360, NULL, 44.7,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- UNIVERSAL COLUMNS (UC)
('UC-100X14', '100UC14.8 Universal Column', 'Columns', 'Mild Steel', '100UC',
 9000, 100, NULL, 14.8,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UC-150X23', '150UC23.4 Universal Column', 'Columns', 'Mild Steel', '150UC',
 9000, 150, NULL, 23.4,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UC-150X30', '150UC30.0 Universal Column', 'Columns', 'Mild Steel', '150UC',
 9000, 150, NULL, 30.0,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UC-200X46', '200UC46.2 Universal Column', 'Columns', 'Mild Steel', '200UC',
 12000, 200, NULL, 46.2,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UC-200X52', '200UC52.2 Universal Column', 'Columns', 'Mild Steel', '200UC',
 12000, 200, NULL, 52.2,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('UC-250X72', '250UC72.9 Universal Column', 'Columns', 'Mild Steel', '250UC',
 12000, 250, NULL, 72.9,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- ANGLES - Equal
('EA-25X25X3', '25x25x3 Equal Angle', 'Angles', 'Mild Steel', '25x25EA',
 6000, 25, 3, 1.12,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-30X30X3', '30x30x3 Equal Angle', 'Angles', 'Mild Steel', '30x30EA',
 6000, 30, 3, 1.36,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-40X40X3', '40x40x3 Equal Angle', 'Angles', 'Mild Steel', '40x40EA',
 6000, 40, 3, 1.83,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-50X50X3', '50x50x3 Equal Angle', 'Angles', 'Mild Steel', '50x50EA',
 6000, 50, 3, 2.30,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-50X50X5', '50x50x5 Equal Angle', 'Angles', 'Mild Steel', '50x50EA',
 6000, 50, 5, 3.77,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-65X65X6', '65x65x6 Equal Angle', 'Angles', 'Mild Steel', '65x65EA',
 6000, 65, 6, 5.91,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-75X75X6', '75x75x6 Equal Angle', 'Angles', 'Mild Steel', '75x75EA',
 6000, 75, 6, 6.85,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-75X75X8', '75x75x8 Equal Angle', 'Angles', 'Mild Steel', '75x75EA',
 6000, 75, 8, 9.00,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-90X90X8', '90x90x8 Equal Angle', 'Angles', 'Mild Steel', '90x90EA',
 9000, 90, 8, 10.90,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('EA-100X100X10', '100x100x10 Equal Angle', 'Angles', 'Mild Steel', '100x100EA',
 9000, 100, 10, 15.00,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- FLATS/BARS
('FB-25X3', '25x3 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 25, 3, 0.59,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-25X6', '25x6 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 25, 6, 1.18,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-40X6', '40x6 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 40, 6, 1.88,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-50X6', '50x6 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 50, 6, 2.36,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-50X10', '50x10 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 50, 10, 3.93,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-75X6', '75x6 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 75, 6, 3.53,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-75X10', '75x10 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 75, 10, 5.89,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-100X6', '100x6 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 100, 6, 4.71,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-100X10', '100x10 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 100, 10, 7.85,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('FB-100X12', '100x12 Flat Bar', 'Bars', 'Mild Steel', 'FLAT',
 6000, 100, 12, 9.42,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- ROUND BARS
('RB-10', '10mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 10, 0.617,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-12', '12mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 12, 0.888,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-16', '16mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 16, 1.578,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-20', '20mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 20, 2.466,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-25', '25mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 25, 3.853,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-32', '32mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 32, 6.313,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-40', '40mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 40, 9.865,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('RB-50', '50mm Round Bar', 'Bars', 'Mild Steel', 'ROUND',
 6000, NULL, 50, 15.413,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- SQUARE BARS
('SB-10', '10mm Square Bar', 'Bars', 'Mild Steel', 'SQUARE',
 6000, 10, 10, 0.785,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('SB-12', '12mm Square Bar', 'Bars', 'Mild Steel', 'SQUARE',
 6000, 12, 12, 1.130,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('SB-16', '16mm Square Bar', 'Bars', 'Mild Steel', 'SQUARE',
 6000, 16, 16, 2.010,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('SB-20', '20mm Square Bar', 'Bars', 'Mild Steel', 'SQUARE',
 6000, 20, 20, 3.140,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

('SB-25', '25mm Square Bar', 'Bars', 'Mild Steel', 'SQUARE',
 6000, 25, 25, 4.906,
 'AS/NZS 3679.1', '300', 'Black/Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- MESH PRODUCTS
('MESH-SL62', 'SL62 Reinforcing Mesh', 'Mesh', 'Mild Steel', 'MESH',
 6000, 2400, NULL, 43.0,
 'AS/NZS 4671', '500L', 'Black', 'SHEET', 1, GETUTCDATE(), 1),

('MESH-SL72', 'SL72 Reinforcing Mesh', 'Mesh', 'Mild Steel', 'MESH',
 6000, 2400, NULL, 50.4,
 'AS/NZS 4671', '500L', 'Black', 'SHEET', 1, GETUTCDATE(), 1),

('MESH-SL82', 'SL82 Reinforcing Mesh', 'Mesh', 'Mild Steel', 'MESH',
 6000, 2400, NULL, 58.8,
 'AS/NZS 4671', '500L', 'Black', 'SHEET', 1, GETUTCDATE(), 1),

('MESH-SL92', 'SL92 Reinforcing Mesh', 'Mesh', 'Mild Steel', 'MESH',
 6000, 2400, NULL, 69.6,
 'AS/NZS 4671', '500L', 'Black', 'SHEET', 1, GETUTCDATE(), 1),

-- STAINLESS STEEL EXAMPLES
('SS-PLT-304-3MM', '3mm 304 Stainless Steel Plate', 'Plates', 'Stainless Steel', NULL,
 2400, 1200, 3, 23.76,
 'ASTM A240', '304', '2B', 'SHEET', 1, GETUTCDATE(), 1),

('SS-PLT-316-3MM', '3mm 316 Stainless Steel Plate', 'Plates', 'Stainless Steel', NULL,
 2400, 1200, 3, 23.76,
 'ASTM A240', '316', '2B', 'SHEET', 1, GETUTCDATE(), 1),

('SS-RB-304-10', '10mm 304 Stainless Round Bar', 'Bars', 'Stainless Steel', 'ROUND',
 6000, NULL, 10, 0.620,
 'ASTM A276', '304', 'Bright', 'LENGTH', 1, GETUTCDATE(), 1),

('SS-RB-316-10', '10mm 316 Stainless Round Bar', 'Bars', 'Stainless Steel', 'ROUND',
 6000, NULL, 10, 0.620,
 'ASTM A276', '316', 'Bright', 'LENGTH', 1, GETUTCDATE(), 1),

-- ALUMINUM EXAMPLES
('AL-PLT-5083-6MM', '6mm 5083 Aluminum Plate', 'Plates', 'Aluminum', NULL,
 2400, 1200, 6, 16.13,
 'AS/NZS 1734', '5083', 'Mill', 'SHEET', 1, GETUTCDATE(), 1),

('AL-PLT-6061-6MM', '6mm 6061 Aluminum Plate', 'Plates', 'Aluminum', NULL,
 2400, 1200, 6, 16.20,
 'AS/NZS 1734', '6061', 'Mill', 'SHEET', 1, GETUTCDATE(), 1),

('AL-EA-50X50X3', '50x50x3 Aluminum Equal Angle', 'Angles', 'Aluminum', '50x50EA',
 6000, 50, 3, 0.79,
 'AS/NZS 1734', '6061', 'Mill', 'LENGTH', 1, GETUTCDATE(), 1),

-- GRATING PRODUCTS
('GRAT-32X5-900X600', 'Steel Grating 32x5 900x600mm Panel', 'Grating', 'Mild Steel', 'GRATING',
 900, 600, 32, 21.6,
 'AS/NZS 1657', 'Grade 250', 'Hot Dip Galvanized', 'PANEL', 1, GETUTCDATE(), 1),

('GRAT-40X5-900X600', 'Steel Grating 40x5 900x600mm Panel', 'Grating', 'Mild Steel', 'GRATING',
 900, 600, 40, 26.4,
 'AS/NZS 1657', 'Grade 250', 'Hot Dip Galvanized', 'PANEL', 1, GETUTCDATE(), 1);

-- Note: This is a sample of catalogue items. The full 7,107 items should be imported
-- using the Excel import service or through a bulk data import process.

PRINT 'Catalogue items seed data inserted successfully';
PRINT 'Sample items loaded. For full catalogue, use Excel import service.';