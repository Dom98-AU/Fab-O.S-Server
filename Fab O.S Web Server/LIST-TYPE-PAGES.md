# List-Type Pages Registry
## Fab.OS Platform - Standardized List Page Documentation

**Last Updated:** 2025-10-23
**Purpose:** This document tracks all list-type pages in the Fab.OS Platform to ensure UI/UX consistency and maintainability.

---

## What is a List-Type Page?

A **List-Type Page** is any page that displays a collection of items (entities) in a tabular, list, or card format with the following characteristics:

### Required Features:
- ✅ Displays multiple items from a data source
- ✅ Supports view switching (Table/List/Card views)
- ✅ Includes search/filter functionality
- ✅ Supports item selection
- ✅ Uses `StandardToolbar` with `IToolbarActionProvider`
- ✅ Uses `TableViewToolbar` for view management
- ✅ Uses `GenericTableView`, `GenericListView`, or `GenericCardView` components

### Standard Components Stack:
```
StandardToolbar (search + actions)
    ↓
TableViewToolbar (view switcher + column management)
    ↓
GenericTableView / GenericListView / GenericCardView
```

---

## Metadata Marker System

All list-type pages MUST include this metadata comment block at the top of the file:

```razor
@*
═══════════════════════════════════════════════════════════════
LIST-TYPE PAGE
═══════════════════════════════════════════════════════════════
Page Type: List
Entity: [EntityName]
Views: Table, List, Card
Features: ViewPreferences, ColumnManagement, Search, Selection
Pattern: Standard
Last Updated: [Date]
═══════════════════════════════════════════════════════════════
*@
```

---

## Registered List-Type Pages

### ✅ Fully Standardized Pages

#### 1. PackagesList.razor
```
Route: /packages
Entity: Package
Status: ✅ STANDARDIZED (Reference Implementation)
Features:
  - View Preferences: Enabled
  - Column Management: Enabled
  - BreadcrumbService: Yes
  - All 3 views: Table, List, Card
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✓
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ✅ Yes
```

#### 2. RevisionsList.razor
```
Route: /takeoffs/{id}/revisions
Entity: Revision
Status: ✅ STANDARDIZED
Features:
  - View Preferences: Enabled
  - Column Management: Enabled
  - BreadcrumbService: Yes
  - All 3 views: Table, List, Card
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✓
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ✅ Yes
```

#### 3. TakeoffPackages.razor
```
Route: /takeoffs/{id}/packages
Entity: Package (scoped to Takeoff)
Status: ✅ STANDARDIZED
Features:
  - View Preferences: Enabled
  - Column Management: Enabled
  - BreadcrumbService: Yes
  - All 3 views: Table, List, Card
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✓
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ✅ Yes
```

#### 4. PackageDrawingMeasurements.razor
```
Route: /packages/{id}/drawings/{drawingId}/measurements
Entity: TraceTakeoffMeasurement
Status: ✅ STANDARDIZED
Features:
  - View Preferences: Enabled
  - Column Management: Enabled
  - BreadcrumbService: Yes
  - All 3 views: Table, List, Card
  - SignalR realtime updates
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✓
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ✅ Yes
```

#### 5. PackageSharePointFiles.razor
```
Route: /packages/{id}/sharepoint-files
Entity: SharePointFileInfo
Status: ✅ STANDARDIZED
Features:
  - View Preferences: Enabled
  - Column Management: Enabled
  - BreadcrumbService: Yes
  - All 3 views: Table, List, Card
  - Folder navigation
  - File upload/delete
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✓
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ✅ Yes
Special: Renders folders + files separately
```

---

### ⚠️ Partially Standardized Pages

#### 6. Takeoffs.razor
```
Route: /takeoffs
Entity: Takeoff
Status: ⚠️ PARTIAL (Missing TableViewToolbar)
Features:
  - View Preferences: Partial (uses GenericViewSwitcher only)
  - Column Management: No
  - BreadcrumbService: No
  - All 3 views: Table, List, Card
Components:
  - StandardToolbar ✓
  - GenericViewSwitcher ✓ (but NOT TableViewToolbar)
  - GenericTableView ✓
  - GenericListView ✓
  - GenericCardView ✓
Modern UI: ⚠️ Partial
Issues:
  - Missing TableViewToolbar (uses simplified GenericViewSwitcher)
  - No column management
  - Manual results summary instead of toolbar integration
  - Hardcoded columns (not using ColumnDefinition)
Recommendation: Add TableViewToolbar
```

---

### ❌ Non-Standardized Pages (Need Refactoring)

#### 7. Customers.razor
```
Route: /customers
Entity: Customer
Status: ❌ NOT STANDARDIZED
Current Implementation:
  - Hand-coded HTML table
  - 310 lines of embedded CSS
  - No view switching (table only)
  - Manual filtering
  - No column management
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✗
  - GenericTableView ✗
  - GenericListView ✗
  - GenericCardView ✗
Modern UI: ❌ No
Priority: HIGH
Estimated Effort: 2-3 hours
```

#### 8. Contacts.razor
```
Route: /contacts
Entity: Contact
Status: ❌ NOT STANDARDIZED
Current Implementation:
  - Custom view toggle (grid/list)
  - 341 lines of embedded CSS
  - Manual CSS Grid for cards
  - Hand-coded table for list view
  - No column management
Components:
  - StandardToolbar ✓
  - TableViewToolbar ✗
  - GenericTableView ✗
  - GenericListView ✗
  - GenericCardView ✗
Modern UI: ❌ No
Priority: HIGH
Estimated Effort: 3-4 hours
```

---

## Page Categories

### Primary List Pages (Standalone)
- **PackagesList.razor** - All packages
- **Takeoffs.razor** - All takeoffs
- **Customers.razor** - All customers
- **Contacts.razor** - All contacts

### Scoped List Pages (Child Resources)
- **RevisionsList.razor** - Revisions for a takeoff
- **TakeoffPackages.razor** - Packages for a takeoff
- **PackageDrawingMeasurements.razor** - Measurements for a drawing
- **PackageSharePointFiles.razor** - Files for a package

---

## Standard Implementation Checklist

When creating or refactoring a list-type page, ensure:

### Required Components
- [ ] `@page` directive with route
- [ ] `@rendermode InteractiveServer`
- [ ] `IToolbarActionProvider` interface implementation
- [ ] `StandardToolbar` component
- [ ] `TableViewToolbar` component
- [ ] All three Generic view components (Table/List/Card)

### Required Features
- [ ] View switching (3 standard views)
- [ ] View preferences (EnableViewPreferences="true")
- [ ] Column management (EnableColumnManagement="true")
- [ ] Search functionality
- [ ] Multi-select with checkboxes
- [ ] Row click/double-click handlers
- [ ] BreadcrumbService integration
- [ ] Empty state handling
- [ ] Loading state handling

### Required State Variables
```csharp
private GenericViewSwitcher<T>.ViewType currentView = ViewType.Table;
private List<T> allItems = new();
private List<T> filteredItems = new();
private List<T> selectedTableItems = new();
private List<T> selectedListItems = new();
private List<T> selectedCardItems = new();
private List<ColumnDefinition> columnDefinitions = new();
private List<ColumnDefinition> managedColumns = new();
private ViewState currentViewState = new();
private bool hasUnsavedChanges = false;
private bool hasCustomColumnConfig = false;
private bool isLoading = true;
private string searchTerm = "";
```

### Required Methods
```csharp
- OnInitializedAsync()
- UpdateBreadcrumb()
- InitializeColumnDefinitions()
- InitializeTableColumns()
- UpdateTableColumns()
- CreateTableColumn()
- LoadData()
- FilterData()
- OnSearchChanged()
- OnViewChanged()
- HandleRowClick()
- HandleRowDoubleClick()
- HandleTableSelectionChanged()
- HandleListSelectionChanged()
- HandleCardSelectionChanged()
- GetSelectedItems()
- GetActions() // IToolbarActionProvider
- HandleColumnsChanged()
- HandleViewLoaded()
```

---

## Code Template

Use **PackagesList.razor** and **PackagesList.razor.cs** as the reference template for all list-type pages.

### File Structure
```
/Components/Pages/
  ├─ EntityList.razor          (UI markup)
  ├─ EntityList.razor.cs       (Code-behind)
  └─ EntityList.razor.css      (Scoped styles, if needed)
```

---

## Modern Table UI Specifications

All list-type pages use the **Modern Table UI** design system:

### Colors
- Background gradient: `#f8fafc` to `#e2e8f0` (slate-50 to slate-100)
- Table card: `#ffffff` white with shadow
- Header: `#f8fafc` (slate-50)
- Row hover: `#f8fafc` (slate-50)
- Selected row: `#eff6ff` (blue-50)
- Text: Slate colors (#1e293b, #64748b, #475569)
- Checkbox accent: `#3b82f6` (blue-500)

### Typography
- System font stack (Segoe UI, Roboto, etc.)
- Header: 0.875rem, weight 600
- Body: 1rem, weight 400

### Components
- **GenericTableView**: Modern table with sorting, selection, and column management
- **GenericListView**: Vertical list with custom templates
- **GenericCardView**: Responsive card grid

---

## Maintenance Guidelines

### Adding a New List-Type Page
1. Copy `PackagesList.razor` and `PackagesList.razor.cs` as templates
2. Add the LIST-TYPE PAGE metadata comment block
3. Update this registry document
4. Implement all required features
5. Test all three views (Table/List/Card)
6. Verify view preferences work
7. Test column management
8. Submit for code review

### Updating an Existing List-Type Page
1. Check this registry for current status
2. Ensure metadata comment is present
3. Follow the standard pattern
4. Update "Last Updated" in metadata
5. Run standardization checklist
6. Update this registry if structure changes

### Refactoring Non-Standard Pages
1. Identify page in "Non-Standardized" section
2. Use PackagesList.razor as reference
3. Replace custom implementations with standard components
4. Add metadata marker
5. Move to "Fully Standardized" section in this registry
6. Remove embedded CSS (use standard component styles)

---

## Quality Assurance

### Code Review Checklist
- [ ] Metadata comment present and accurate
- [ ] Uses StandardToolbar + TableViewToolbar
- [ ] All three view types implemented
- [ ] Column management enabled
- [ ] View preferences enabled
- [ ] BreadcrumbService integrated
- [ ] No embedded CSS (uses modern-table-* classes)
- [ ] Follows naming conventions
- [ ] Registry document updated

### Testing Checklist
- [ ] Table view renders correctly
- [ ] List view renders correctly
- [ ] Card view renders correctly
- [ ] View switching works
- [ ] Search/filter works
- [ ] Selection works (individual + select all)
- [ ] Sort works (if applicable)
- [ ] Column management works
- [ ] View preferences save/load
- [ ] Empty state displays
- [ ] Loading state displays
- [ ] Responsive on mobile
- [ ] Keyboard navigation works
- [ ] Screen reader compatible

---

## Statistics

**Total List-Type Pages:** 8
**Fully Standardized:** 5 (63%)
**Partially Standardized:** 1 (12%)
**Non-Standardized:** 2 (25%)

**Modern UI Applied:** 5 pages
**Need Refactoring:** 3 pages (Takeoffs, Customers, Contacts)

---

## References

- **Reference Implementation:** [PackagesList.razor](Components/Pages/PackagesList.razor)
- **Modern Table Spec:** [GenericTableView.razor](Components/Shared/GenericTableView.razor)
- **Component Documentation:** See `Components/Shared/README.md`

---

## Change Log

### 2025-10-23
- Created LIST-TYPE-PAGES.md registry
- Added metadata marker system
- Documented all 8 list-type pages
- Applied Modern Table UI to GenericTableView
- PackageSharePointFiles.razor standardized
- Updated statistics

---

**Maintained by:** Development Team
**Questions?** See [CLAUDE.md](../../CLAUDE.md) for project guidelines
