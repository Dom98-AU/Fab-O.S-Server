# Fab.OS Platform - Visual Identity Guidelines

## Overview

This document establishes the visual identity system for the Fab.OS multi-tenant platform, based on NWI Group's "house of brands" approach. The Fab.OS platform requires consistent branding across all tenant applications while maintaining professional credibility and modern design standards.

## Brand Approach

Fab.OS follows a unified visual identity system that ensures consistent brand recognition across all modules (Estimate, Trace, Fabmate, QDocs) and tenant implementations. The approach emphasizes:

- **Consistency**: Unified visual language across all platform touchpoints
- **Professionalism**: Clean, modern aesthetic that builds trust
- **Scalability**: Design system that works across web, mobile, and print applications
- **Differentiation**: Subtle visual variations for different modules while maintaining cohesion

## Logo System

### Primary Logo
- **Design**: Isometric "F" lettermark in blue gradient
- **Colors**: Light blue (#3144CD) to deep blue (#0D1A80) gradient
- **Style**: 3D isometric design with geometric construction
- **Usage**: Main platform branding, favicons, app icons, marketing materials
- **Clear Space**: Minimum clear space equal to half the logo height on all sides

### Logo Construction
- **Primary Element**: Geometric "F" built from isometric shapes
- **Gradient Direction**: Light blue at top/front faces, darker blue for depth/shadows
- **Proportions**: Maintains consistent angles for isometric perspective
- **Minimum Size**: 24px for digital use, 15mm for print

### Logo Variations
- **Full Color**: Primary gradient version for light backgrounds
- **Monochrome**: Single color versions in deep blue (#0D1A80) or white
- **Mark Only**: "F" symbol without wordmark for compact spaces
- **Horizontal**: Logo with "Fab.OS" wordmark positioned to the right

### Module Integration
Each platform module integrates with the primary logo:
- **Format**: Fab.OS "F" mark + module name
- **Typography**: Module names in Avenir Next Medium
- **Layout**: Module name positioned consistently relative to logo mark
- **Color**: Module names in deep blue (#0D1A80) or white on dark backgrounds

## Color Palette

### Primary Colors
- **Deep Blue**: `#0D1A80` (rgb(13,26,128), cmyk(90,80,0,50))
  - Primary brand color for headers, CTAs, and key elements
- **Medium Blue**: `#3144CD` (rgb(49,68,205), cmyk(76,67,0,20))
  - Secondary actions, links, accent elements
- **Light Blue**: `#5261D5` (rgb(82,97,213), cmyk(62,54,0,16))
  - Hover states, highlights, tertiary elements

### Secondary Colors
- **Charcoal**: `#161616` (rgb(22,22,22), cmyk(0,0,0,91))
  - Primary text, icons
- **Medium Gray**: `#777777` (rgb(119,119,119), cmyk(0,0,0,53))
  - Secondary text, form labels
- **Light Gray**: `#B1B1B1` (rgb(177,177,177), cmyk(0,0,0,31))
  - Borders, disabled states, subtle backgrounds
- **White**: `#FFFFFF` (rgb(255,255,255), cmyk(0,0,0,0))
  - Backgrounds, negative space

### Color Usage Guidelines
- Use Deep Blue (#0D1A80) for primary actions and branding elements
- Medium Blue (#3144CD) for secondary actions and interactive elements
- Maintain sufficient contrast ratios for accessibility (minimum 4.5:1 for normal text)
- Reserve light colors for backgrounds and supporting elements

## Typography

### System Font Stack
Use the existing system font stack already implemented in the Fab.OS platform for consistency across all modules and components.

### Typography Weights
**Regular/Normal Weight**
- Body text, descriptions, form fields
- Standard readable weight for content

**Medium/Semibold Weight**
- Subheadings, button text, navigation items
- Provides emphasis without being too heavy

**Bold Weight**
- Headlines, page titles, call-to-action buttons
- Strong presence for important information hierarchy

### Typography Guidelines
- **Headings**: Use Bold weight for H1-H2, Medium weight for H3-H4
- **Body Text**: Regular weight, 16px minimum for web interfaces
- **UI Elements**: Medium weight for buttons and navigation
- **Line Height**: 1.4-1.6 for optimal readability
- **Letter Spacing**: Default spacing, avoid excessive tracking
- **Consistency**: Maintain the same font stack across all platform modules

## Graphic Elements & Patterns

### Module-Specific Visual Elements

**Estimate Module**
- **Pattern**: Concentric squares/rectangles
- **Symbolism**: Precision, measurement, structured analysis
- **Usage**: Backgrounds, decorative elements, icons

**Trace Module**
- **Pattern**: Diagonal lines/hatching
- **Symbolism**: Movement, tracking, process flow
- **Usage**: Progress indicators, section dividers

**Fabmate Module**
- **Pattern**: Chevron/arrow sequences
- **Symbolism**: Forward progress, manufacturing flow
- **Usage**: Process steps, navigation elements

**QDocs Module**
- **Pattern**: Plus signs/crosses in grid
- **Symbolism**: Addition, building, documentation
- **Usage**: File organization, grid layouts

**Systems/General**
- **Pattern**: Dots and dashes in organized grid
- **Symbolism**: Data points, connectivity, systems integration
- **Usage**: Dashboard backgrounds, technical interfaces

### Pattern Usage Guidelines
- Use patterns subtly as background elements or accents
- Maintain sufficient contrast with foreground content
- Scale patterns appropriately for different screen sizes
- Combine with brand colors for cohesive appearance

## Application Guidelines

### Web Interface
- **Header**: Deep Blue background with white Fab.OS logo
- **Navigation**: Medium Blue for active states, Gray for inactive
- **Cards/Panels**: White backgrounds with subtle gray borders
- **Buttons**: Blue gradient backgrounds with white text
- **Forms**: Clean layout with adequate white space

### Mobile Interface
- Larger touch targets (minimum 44px)
- Simplified navigation patterns
- Maintain color hierarchy from web version
- Ensure text remains legible at small sizes

### Marketing Materials
- Use full color palette with Deep Blue as dominant color
- Incorporate appropriate module patterns
- Maintain generous white space for professional appearance
- Ensure logo has sufficient clear space around it

## Accessibility Standards

### Color Contrast
- **Normal Text**: Minimum 4.5:1 contrast ratio
- **Large Text**: Minimum 3:1 contrast ratio
- **Interactive Elements**: Clear visual states for hover, focus, active

### Typography
- Minimum 16px for body text on web
- Maximum line length of 80 characters
- Clear hierarchy with appropriate heading levels
- Avoid all-caps text for readability

## Brand Voice Alignment

The visual identity supports Fab.OS positioning as:
- **Intelligent**: Modern, clean design with subtle sophistication
- **Reliable**: Consistent application of design principles
- **Efficient**: Streamlined interface elements and clear information hierarchy
- **Professional**: Polished appearance suitable for enterprise environments

## Asset Locations

### Logo and Favicon Files
All Fab.OS logo assets, favicons, and brand graphics are located in:
```
C:\Fab.OS Platform\Fab O.S\SteelEstimation.Web\wwwroot
```

**Available Assets:**
- Favicon files (various sizes)
- Logo variations (full color, monochrome, different formats)
- App icons for different devices
- Brand graphics and patterns

**Usage in Code:**
- Reference assets using relative paths from wwwroot
- Ensure proper alt text for accessibility
- Use appropriate sizes for different contexts (favicon, header, marketing)

## Toolbar Specification

### Overview
The toolbar system provides a consistent interface for actions across all Fab.OS modules. It uses a modern gradient-based design with the Fab.OS color palette and maintains visual hierarchy through careful use of color and spacing.

### Toolbar Structure
- **Container**: Light gradient background (#ffffff to #f8f9fa) with subtle border
- **Sections**: Primary actions (left), Secondary actions (center), Utilities (right)
- **Responsive**: Collapses intelligently on mobile devices

### Button Styles

#### Primary Actions (New/Create)
- **Background**: Deep Blue to Dark Blue gradient (#3144CD to #0D1A80)
- **Hover**: Light Blue to Medium Blue gradient (#5261D5 to #3144CD)
- **Purpose**: Main creation actions, most important user tasks

#### Secondary Actions (Export/Import)
- **Export**: Light Blue to Medium Blue gradient (#5261D5 to #3144CD)
- **Import**: Green gradient (#10b981 to #059669) for data ingestion
- **Hover**: Lighter shade gradients with enhanced shadows

#### Utility Actions (Reports/Actions/Related)
- **Reports/Actions**: Charcoal gradient (#777777 to #161616)
- **Related**: Light Blue to Medium Blue gradient (#5261D5 to #3144CD)
- **Refresh**: Primary blue gradient matching New button style

#### Destructive Actions
- **Delete**: Red gradient (#dc2626 to #b91c1c) 
- **Hover**: Brighter red gradient (#ef4444 to #dc2626)
- **Warning**: Clear visual distinction for dangerous operations

### Visual Effects
- **Shadows**: Subtle box-shadows using primary color (rgba(13, 26, 128, 0.08))
- **Inset Borders**: White inset border (rgba(255, 255, 255, 0.25)) for depth
- **Transitions**: Smooth 0.2s cubic-bezier transitions
- **Hover States**: Slight Y-axis translation (-1px) and enhanced shadows
- **Active States**: Compressed appearance with inset shadows

### Dropdown Menus
- **Border**: Light Gray (#B1B1B1) for subtle definition
- **Shadow**: Deep Blue shadow (rgba(13, 26, 128, 0.15))
- **Item Hover**: Light blue background tint with primary blue text (#3144CD)
- **Icons**: Medium Gray (#777777) transitioning to primary blue on hover
- **Badges**: Primary blue gradient matching button style

### Accessibility Features
- **Focus States**: 3px primary blue outline (rgba(49, 68, 205, 0.3))
- **Focus Visible**: 2px solid outline in Medium Blue (#3144CD)
- **Disabled States**: 60% opacity with neutral gray background
- **Color Contrast**: Maintains WCAG AA standards for all text

### Implementation CSS Variables
```css
/* Toolbar-specific color variables */
:root {
  --toolbar-primary-gradient: linear-gradient(180deg, #3144CD 0%, #0D1A80 100%);
  --toolbar-primary-hover: linear-gradient(180deg, #5261D5 0%, #3144CD 100%);
  --toolbar-secondary-gradient: linear-gradient(180deg, #5261D5 0%, #3144CD 100%);
  --toolbar-utility-gradient: linear-gradient(180deg, #777777 0%, #161616 100%);
  --toolbar-danger-gradient: linear-gradient(180deg, #dc2626 0%, #b91c1c 100%);
  --toolbar-success-gradient: linear-gradient(180deg, #10b981 0%, #059669 100%);
  --toolbar-shadow-color: rgba(13, 26, 128, 0.08);
  --toolbar-focus-color: rgba(49, 68, 205, 0.3);
}
```

### Responsive Behavior
- **Desktop (1024px+)**: Full button text and icons visible
- **Tablet (768px-1024px)**: Reduced font sizes, maintained spacing
- **Mobile (320px-768px)**: Icon-only buttons, collapsed sections

### Animation Guidelines
- **Hover Transitions**: 0.2s cubic-bezier(0.3, 0, 0.5, 1)
- **Dropdown Animations**: 0.15s slide-down with fade
- **Ripple Effects**: 0.3s radial expansion on click
- **Button Press**: Subtle Y-axis movement for tactile feedback

## DataViewSwitcher Specification

### Overview
The DataViewSwitcher component provides a unified interface for displaying data in multiple views (Table, Grid, List) across all Fab.OS modules. It implements the Fab.OS visual identity consistently while offering flexibility for custom templates.

### View Switcher Controls
- **Button Group**: Clean toggle between Table, Grid, and List views
- **Active State**: Deep Blue gradient (#3144CD to #0D1A80) for selected view
- **Hover Effects**: Smooth transitions with Fab.OS color palette
- **Item Count**: Displays total items with subtle gray background

### Card View (Grid) Design

#### Card Container
- **Background**: White to light gray gradient
- **Border**: Light Gray (#B1B1B1) 1px solid
- **Height**: 320px for optimal content display
- **Shadow**: Subtle Deep Blue shadow (rgba(13, 26, 128, 0.08))
- **Hover**: Elevated effect with enhanced shadows

#### Card Elements
- **Avatar/Icon**: 48x48px with Fab.OS gradient (#3144CD to #0D1A80)
- **Title**: 1.1rem bold in Charcoal (#161616)
- **Subtitle**: 0.875rem in Medium Gray (#777777)
- **Status Ribbon**: Diagonal ribbon for status indicators (green for active, red for inactive)

#### Statistics Grid
- **Layout**: 2-column grid for metrics
- **Background**: Light gray (#f8fafc) with Light Gray border
- **Icons**: Color-coded with Fab.OS blues
- **Values**: Large bold numbers in Charcoal
- **Labels**: Small uppercase text in Light Gray

#### Action Buttons
- **Default**: White background with Light Gray border
- **View Hover**: Deep Blue gradient
- **Edit Hover**: Light Blue gradient (#5261D5 to #3144CD)
- **Delete Hover**: Red gradient for destructive actions

### List View Design

#### List Item Container
- **Background**: White with subtle hover gradient
- **Border**: Light Gray (#B1B1B1) 1px solid
- **Padding**: 1rem 1.25rem for comfortable spacing
- **Hover**: Slide right animation with enhanced shadow

#### List Elements
- **Avatar**: 40x40px with Fab.OS gradient
- **Primary Text**: 0.95rem semibold in Charcoal
- **Secondary Text**: 0.8rem in Medium Gray
- **Meta Information**: Small text with icon indicators
- **Status Pills**: Rounded badges with color-coded backgrounds

### Table View Design
- **Headers**: Fab.OS gradient background with white text
- **Rows**: White background with hover effects
- **Selected**: Light blue tinted background
- **Borders**: Light Gray (#B1B1B1) for subtle separation

### Color Implementation

```css
/* DataViewSwitcher Color Variables */
:root {
  --dv-primary-gradient: linear-gradient(180deg, #3144CD 0%, #0D1A80 100%);
  --dv-hover-gradient: linear-gradient(180deg, #5261D5 0%, #3144CD 100%);
  --dv-border-color: #B1B1B1;
  --dv-text-primary: #161616;
  --dv-text-secondary: #777777;
  --dv-shadow-color: rgba(13, 26, 128, 0.08);
  --dv-success-color: #10b981;
  --dv-danger-color: #ef4444;
}
```

### Responsive Behavior
- **Desktop**: Full grid with 3-4 columns
- **Tablet**: 2-column grid, adjusted card heights
- **Mobile**: Single column, simplified card layout

### Animation Guidelines
- **Card Hover**: 0.3s cubic-bezier(0.4, 0, 0.2, 1) for smooth lift
- **List Hover**: 0.2s ease for quick response
- **View Transitions**: Instant switching between views
- **Status Animations**: Pulse effect for active status indicators

### Accessibility Features
- **Selection**: Clear checkbox indicators with Fab.OS blue when checked
- **Focus States**: Visible outline in Medium Blue (#3144CD)
- **Contrast**: All text meets WCAG AA standards
- **Interactive Areas**: Minimum 32px touch targets for mobile

### Usage Guidelines
- **Consistency**: Use DataViewSwitcher for all data listings
- **Custom Templates**: Override CardTemplate or ListItemTemplate when needed
- **Empty States**: Provide helpful messages when no data is available
- **Loading States**: Show spinner with primary color during data fetch

## Column Management UI Specification

### Overview
The Column Management system provides sophisticated controls for table and list column manipulation, including drag-and-drop reordering, column freezing, and visibility management. The UI maintains the Fab.OS visual identity while ensuring optimal usability for complex data management tasks.

### Column Reorder Panel

#### Panel Container
- **Background**: White with subtle shadow
- **Border**: Light Gray (#B1B1B1) 1px solid
- **Border Radius**: 8px for modern appearance
- **Shadow**: Deep Blue shadow (rgba(13, 26, 128, 0.15))
- **Max Dimensions**: 400px width, 600px height with scroll

#### Panel Header
- **Background**: Deep Blue to Dark Blue gradient (#3144CD to #0D1A80)
- **Color**: White text for contrast
- **Padding**: 0.75rem 1rem
- **Border Radius**: 8px 8px 0 0 (rounded top corners)
- **Typography**: Medium weight, 1.1rem size

#### Column Items
- **Background**: White with hover state
- **Border**: Light Gray (#B1B1B1) 1px solid
- **Padding**: 0.75rem
- **Hover**: Light blue background tint (rgba(49, 68, 205, 0.05))
- **Drag Handle**: Medium Gray (#777777) grip icon
- **Type Icons**: Color-coded based on data type

#### Drag States
- **Dragging Item**: 50% opacity with scale(0.98) transform
- **Drop Target**: 2px dashed border in Primary Blue (#3144CD)
- **Drop Target Background**: Light blue tint (rgba(49, 68, 205, 0.05))
- **Cursor**: grab/grabbing for drag operations

### Column Freeze Indicators

#### Freeze Position Badges
- **Background**: Primary gradient (#5261D5 to #3144CD)
- **Color**: White text
- **Border Radius**: 12px for pill appearance
- **Padding**: 0.125rem 0.5rem
- **Font Size**: 0.75rem
- **Display**: Inline-flex with icon

#### Frozen Column Styling
- **Left Frozen**: Sticky positioning with right shadow
- **Right Frozen**: Sticky positioning with left shadow
- **Shadow**: 2px 0 4px rgba(0,0,0,0.1) for depth
- **Background**: White to prevent content overlap
- **Z-Index**: 10-15 based on freeze order

### Z-Index Hierarchy

```css
/* Column Management Z-Index Layers */
:root {
  --z-table-body: 1;
  --z-frozen-columns-left: 10;
  --z-frozen-columns-right: 15;
  --z-column-panel: 100;
  --z-modal-backdrop: 1040;
  --z-modal: 1050;
  --z-dropdown-menu: 1060;  /* Above modals for proper layering */
  --z-tooltip: 1070;
}
```

### Control Elements

#### Visibility Checkboxes
- **Unchecked**: Light Gray border (#B1B1B1)
- **Checked**: Primary Blue background (#3144CD)
- **Hover**: Light Blue border (#5261D5)
- **Disabled**: 60% opacity for required columns

#### Freeze Dropdown
- **Button**: White background with gray border
- **Hover**: Light blue background tint
- **Active**: Primary Blue border
- **Menu Position**: Fixed positioning when in modals
- **Menu Z-Index**: 1060 to appear above modal content

#### Action Buttons
- **Reset**: Medium Gray gradient (#777777 to #161616)
- **Show All**: Primary Blue gradient
- **Hide All**: Light Gray background
- **Unfreeze All**: Secondary Blue gradient

### Visual Feedback

#### Drag Feedback
```css
.column-item.dragging {
  opacity: 0.5;
  transform: scale(0.98);
  transition: transform 0.15s ease;
}

.column-item.drop-target {
  border: 2px dashed #3144CD;
  background: rgba(49, 68, 205, 0.05);
}

.drag-handle {
  cursor: grab;
  color: #777777;
}

.drag-handle:active {
  cursor: grabbing;
}
```

#### Freeze Transition
```css
.frozen-left,
.frozen-right {
  transition: box-shadow 0.2s ease;
}

.table-container.scrolling .frozen-left {
  box-shadow: 2px 0 8px rgba(13, 26, 128, 0.12);
}

.table-container.scrolling .frozen-right {
  box-shadow: -2px 0 8px rgba(13, 26, 128, 0.12);
}
```

### Responsive Behavior

#### Desktop (1024px+)
- Full panel with all controls visible
- Drag-and-drop fully enabled
- Multiple frozen columns supported

#### Tablet (768px-1024px)
- Panel width adjusted to viewport
- Touch-friendly drag targets (44px minimum)
- Limited frozen columns (2 max per side)

#### Mobile (320px-768px)
- Simplified controls (visibility only)
- No drag-and-drop (use up/down buttons)
- Single frozen column support

### Accessibility Features

#### ARIA Labels
```html
<div role="list" aria-label="Column configuration">
  <div role="listitem" 
       draggable="true"
       aria-grabbed="false"
       aria-dropeffect="move"
       aria-label="Column: Product Name">
    <!-- Column content -->
  </div>
</div>
```

#### Keyboard Support
- **Tab**: Navigate between controls
- **Space**: Toggle checkboxes and buttons
- **Arrow Keys**: Reorder columns when focused
- **Escape**: Cancel drag operation
- **Enter**: Apply freeze position from dropdown

#### Screen Reader Announcements
- Column order changes announced
- Visibility state changes announced
- Freeze position changes announced
- Count of visible/hidden columns provided

### Animation Specifications

#### Panel Opening
- **Duration**: 0.3s
- **Easing**: cubic-bezier(0.4, 0, 0.2, 1)
- **Effect**: Slide down with fade in

#### Drag Animation
- **Pickup**: Scale to 0.98 over 0.15s
- **Drop**: Scale to 1.0 over 0.2s
- **Reorder**: Smooth position transition 0.3s

#### State Changes
- **Checkbox**: 0.15s transition
- **Button Hover**: 0.2s ease
- **Freeze Application**: 0.3s slide animation

### Integration Guidelines

#### Modal Integration
When the column manager appears in a modal:
1. Set dropdown `data-bs-container="body"`
2. Use fixed positioning for dropdown menus
3. Apply z-index 1060 for dropdowns
4. Ensure panel scrolls independently

#### Table Integration
For tables with frozen columns:
1. Apply sticky positioning dynamically
2. Calculate left/right offsets via JavaScript
3. Update on scroll for shadow effects
4. Maintain header alignment with frozen columns

### Performance Considerations

#### Optimization Strategies
- Virtual scrolling for 50+ columns
- Debounced drag events (16ms)
- Cached DOM queries for positioning
- RequestAnimationFrame for animations

#### JavaScript Requirements
```javascript
// Column freezing position calculation
function updateFrozenColumns(container) {
  const leftColumns = container.querySelectorAll('.frozen-left');
  const rightColumns = container.querySelectorAll('.frozen-right');
  
  let leftOffset = 0;
  leftColumns.forEach(col => {
    col.style.left = `${leftOffset}px`;
    leftOffset += col.offsetWidth;
  });
  
  let rightOffset = 0;
  rightColumns.forEach(col => {
    col.style.right = `${rightOffset}px`;
    rightOffset += col.offsetWidth;
  });
}
```

## Implementation Notes for Development

### CSS Custom Properties
```css
:root {
  --color-primary: #0D1A80;
  --color-secondary: #3144CD;
  --color-tertiary: #5261D5;
  --color-text-primary: #161616;
  --color-text-secondary: #777777;
  --color-border: #B1B1B1;
  --color-background: #FFFFFF;
  
  /* Logo and branding */
  --logo-gradient-start: #3144CD;
  --logo-gradient-end: #0D1A80;
}
```

### Typography Stack
```css
/* Use existing system font stack from current Fab.OS implementation */
font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
```

### Logo Implementation
```css
.fab-logo {
  background: linear-gradient(135deg, var(--logo-gradient-start), var(--logo-gradient-end));
  /* Apply to logo elements when using CSS-based implementation */
}
```

### Responsive Breakpoints
- Mobile: 320px - 768px
- Tablet: 768px - 1024px  
- Desktop: 1024px+

This visual identity system ensures the Fab.OS platform maintains professional credibility while providing clear differentiation between modules and consistent user experience across all touchpoints.