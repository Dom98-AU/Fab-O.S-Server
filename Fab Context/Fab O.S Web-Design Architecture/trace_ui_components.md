# Trace Module UI Components

## Overview

The Trace module provides specialized UI components for visual PDF takeoff functionality, following the Fab.OS visual identity guidelines. These components emphasize the Trace pattern theme (diagonal lines/hatching) representing movement, tracking, and process flow.

## Component Library

### 1. MeasurementCard Component

**Purpose**: Displays individual measurement information in a card format with visual type indicators.

**Location**: `/Components/Trace/MeasurementCard.razor`

**Features**:
- Type-specific color coding (line=blue, area=green, count=orange)
- Gradient backgrounds following Fab.OS identity
- Hover effects with elevation
- Status badges
- Catalogue item integration
- Page number indicators
- Color strip for visual measurement color

**Usage**:
```razor
<MeasurementCard 
    Measurement="@measurement"
    IsSelected="@isSelected"
    ShowPageNumber="true"
    ShowColorIndicator="true"
    OnEdit="@HandleEdit"
    OnDelete="@HandleDelete" />
```

**Parameters**:
- `Measurement` (TraceMeasurement): The measurement data to display
- `IsSelected` (bool): Highlights the card when selected
- `ShowPageNumber` (bool): Shows the PDF page number
- `ShowColorIndicator` (bool): Shows the measurement color strip
- `OnEdit` (EventCallback): Edit button handler
- `OnDelete` (EventCallback): Delete button handler

**Visual Design**:
- Uses Fab.OS gradient system (#3144CD to #0D1A80)
- Type indicators with 4px colored left border
- Subtle shadow with Deep Blue tint (rgba(13, 26, 128, 0.12))
- Hover transform with -2px Y translation
- Icon buttons with gradient hover states

### 2. TraceToolbar Component

**Purpose**: Provides a comprehensive toolbar for trace operations including measurement tools, export/import, and calibration.

**Location**: `/Components/Trace/TraceToolbar.razor`

**Features**:
- Measurement tool selection (line, area, count)
- Drawing upload button
- Calibration controls
- Multi-format export dropdown (PDF, Excel, JSON, Processing)
- Import functionality
- Generate takeoff action
- Reports access
- Refresh with loading state
- Status bar with item count

**Usage**:
```razor
<TraceToolbar
    ShowMeasurementTools="true"
    ActiveTool="@currentTool"
    ItemCount="@measurements.Count"
    StatusMessage="@statusMsg"
    OnToolSelect="@SelectTool"
    OnExport="@HandleExport"
    OnGenerateTakeoff="@GenerateTakeoff" />
```

**Parameters**:
- `ShowDrawingUpload` (bool): Display upload button
- `ShowMeasurementTools` (bool): Display measurement tool group
- `ShowCalibration` (bool): Display calibrate button
- `ShowExport` (bool): Display export dropdown
- `ShowImport` (bool): Display import button
- `ShowGenerateTakeoff` (bool): Display generate takeoff button
- `ShowReports` (bool): Display reports button
- `ShowRefresh` (bool): Display refresh button
- `ShowStatusBar` (bool): Display status information
- `ActiveTool` (string): Currently selected measurement tool
- `StatusMessage` (string): Status text to display
- `ItemCount` (int?): Number of items to show in status
- `IsRefreshing` (bool): Shows loading state on refresh

**Visual Design**:
- White to light gray gradient background
- Button gradients following Fab.OS toolbar specification
- Tool group with toggle selection
- Export dropdown with icon indicators
- Diagonal line pattern accent (45° repeating gradient)
- Responsive collapse to icon-only on mobile

### 3. CalibrationDialog Component

**Purpose**: Modal dialog for calibrating drawing scale through line measurement or reference objects.

**Location**: `/Components/Trace/CalibrationDialog.razor`

**Features**:
- Line measurement calibration
- Reference object presets (I-beams, doors, steel sections)
- Unit conversion (mm, cm, m, in, ft)
- Real-time scale calculation
- Calibration data JSON storage

**Usage**:
```razor
<CalibrationDialog 
    IsVisible="@showCalibration"
    DrawingId="@currentDrawingId"
    OnCalibrationComplete="@HandleCalibrationComplete"
    OnCancel="@() => showCalibration = false" />
```

**Visual Design**:
- Modal with Deep Blue gradient header
- Reference items as list buttons
- Input fields with Fab.OS styling
- Progress feedback during calibration

## Design Patterns

### Color System

The Trace module uses the following color assignments:

```css
/* Measurement Types */
--trace-line-color: linear-gradient(180deg, #3144CD 0%, #0D1A80 100%);  /* Blue */
--trace-area-color: linear-gradient(180deg, #10b981 0%, #059669 100%);  /* Green */
--trace-count-color: linear-gradient(180deg, #f59e0b 0%, #d97706 100%); /* Orange */
--trace-polygon-color: linear-gradient(180deg, #8b5cf6 0%, #7c3aed 100%); /* Purple */
```

### Pattern Integration

The Trace module pattern (diagonal lines) is integrated through:

1. **Toolbar Accent**: 45° repeating gradient line at top
2. **Card Borders**: Diagonal hatching on hover states
3. **Loading States**: Diagonal stripe animations
4. **Background Patterns**: Subtle diagonal grid for empty states

### Iconography

Consistent icon usage across Trace components:

- **Line Tool**: `fas fa-ruler`
- **Area Tool**: `fas fa-vector-square`
- **Count Tool**: `fas fa-hashtag`
- **Calibrate**: `fas fa-ruler-combined`
- **Drawing**: `fas fa-file-pdf`
- **Takeoff**: `fas fa-drafting-compass`
- **Export**: `fas fa-file-export`
- **Import**: `fas fa-file-import`

## Responsive Behavior

### Desktop (1024px+)
- Full toolbar with text labels
- Multi-column measurement cards
- Side-by-side panels in takeoff view

### Tablet (768px-1024px)
- Condensed toolbar with smaller text
- 2-column card grid
- Stacked panels with tabs

### Mobile (320px-768px)
- Icon-only toolbar buttons
- Single column cards
- Full-screen panels with navigation

## Accessibility

All Trace components follow WCAG AA standards:

- **Color Contrast**: Minimum 4.5:1 for normal text
- **Focus States**: 3px outline in Medium Blue (#3144CD)
- **Touch Targets**: Minimum 44px on mobile
- **ARIA Labels**: Descriptive labels for all interactive elements
- **Keyboard Navigation**: Full keyboard support for all actions

## Animation Guidelines

### Hover Effects
```css
transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
transform: translateY(-2px);
```

### Tool Selection
```css
transition: background 0.2s ease, color 0.2s ease;
```

### Modal Animations
```css
animation: slideDown 0.3s cubic-bezier(0.4, 0, 0.2, 1);
```

## Usage Examples

### Complete Trace Page Setup
```razor
@page "/trace/measurements"

<TraceToolbar
    ShowMeasurementTools="false"
    ShowExport="true"
    ItemCount="@measurements.Count"
    OnExport="@ExportMeasurements"
    OnRefresh="@LoadMeasurements" />

<div class="measurements-grid">
    @foreach (var measurement in measurements)
    {
        <MeasurementCard 
            Measurement="@measurement"
            OnEdit="@EditMeasurement"
            OnDelete="@DeleteMeasurement" />
    }
</div>

<CalibrationDialog 
    @ref="calibrationDialog"
    IsVisible="@showCalibration"
    OnCalibrationComplete="@ApplyCalibration" />
```

## Integration with Trace Module

These components are designed to work seamlessly with:

- `ITraceDrawingService`: Drawing management
- `ITraceMeasurementService`: Measurement CRUD
- `ITraceTakeoffService`: Takeoff generation
- JavaScript `TraceViewer` class: PDF rendering and interaction

## Performance Considerations

- Use virtualization for large measurement lists (50+ items)
- Lazy load PDF pages in viewer
- Debounce measurement updates (300ms)
- Cache drawing SAS URLs for 1 hour
- Optimize card rendering with `@key` directives

## Future Enhancements

Planned improvements for Trace UI components:

1. **Measurement Templates**: Save and reuse common measurements
2. **Batch Operations**: Multi-select for bulk actions
3. **Measurement Groups**: Organize measurements by category
4. **Quick Filters**: Filter by type, status, or page
5. **Keyboard Shortcuts**: Power user productivity features
6. **Mobile Drawing**: Touch-optimized measurement creation
7. **Collaborative Features**: Real-time multi-user measurements
8. **AI Assistance**: Automatic measurement suggestions

## Component Maintenance

When updating Trace components:

1. Maintain Fab.OS visual identity
2. Test across all breakpoints
3. Verify accessibility standards
4. Update this documentation
5. Test with sample PDF files
6. Validate measurement accuracy