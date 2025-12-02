-- Find all 102x76 RHS items
PRINT '=== 102x76 RHS Items ==='
SELECT
    Id,
    ItemCode,
    Description,
    CONCAT(Width_mm, 'x', Height_mm, 'x', Thickness_mm, 'mm') as Dimensions,
    StandardLength_m,
    StandardLengths
FROM CatalogueItems
WHERE
    IsActive = 1
    AND Profile = 'RHS'
    AND Width_mm = 102
    AND Height_mm = 76
ORDER BY Thickness_mm, StandardLength_m;

PRINT ''
PRINT '=== RHS Items grouped by dimensions (showing duplicates) ==='
SELECT
    CONCAT(Width_mm, 'x', Height_mm, 'x', Thickness_mm, 'mm ', Material) as Item,
    COUNT(*) as Count,
    STRING_AGG(CAST(StandardLength_m AS VARCHAR), ', ') as Lengths,
    MIN(ItemCode) as SampleCode
FROM CatalogueItems
WHERE
    IsActive = 1
    AND Profile = 'RHS'
GROUP BY Width_mm, Height_mm, Thickness_mm, Material
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;
