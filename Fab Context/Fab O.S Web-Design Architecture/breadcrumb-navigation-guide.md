# Breadcrumb Navigation Guide - Fab.OS Platform

## Overview
This document defines the breadcrumb navigation system for the Fab.OS Steel Estimation Platform, ensuring consistent navigation patterns that align with the Fab.OS visual identity guidelines.

## Design Principles

### Visual Identity Alignment
Breadcrumbs follow the Fab.OS visual identity system:
- **Primary Color**: Deep Blue (#0D1A80) for active/current items
- **Secondary Color**: Medium Gray (#777777) for navigable links
- **Typography**: System font stack with Medium weight for links, Bold for active items
- **Gradients**: Subtle linear gradient backgrounds (135deg, #f8f9fa to #ffffff)

### User Experience Goals
1. **Clear Navigation Context**: Users always know their location in the application hierarchy
2. **Quick Navigation**: Enable rapid movement between hierarchical levels
3. **Visual Consistency**: Maintain uniform appearance across all page types
4. **Accessibility**: Ensure proper ARIA labels and keyboard navigation support

## Component Structure

### Breadcrumb Container
```html
<nav aria-label="breadcrumb" class="breadcrumb-container">
    <ol class="breadcrumb">
        <!-- Breadcrumb items -->
    </ol>
</nav>
```

### Breadcrumb Item Model
```csharp
public class BreadcrumbItem
{
    public string Label { get; set; }      // Display text
    public string Url { get; set; }        // Navigation URL (null for active item)
    public string Icon { get; set; }       // Optional Font Awesome icon class
    public bool IsActive { get; set; }     // Current page indicator
}
```

## Visual Specifications

### Container Styling
- **Background**: Linear gradient (135deg, #f8f9fa 0%, #ffffff 100%)
- **Padding**: 0.75rem 1.25rem
- **Border Radius**: 10px
- **Box Shadow**: 0 2px 8px rgba(0, 0, 0, 0.05)
- **Border**: 1px solid rgba(0, 0, 0, 0.05)
- **Margin Bottom**: 1rem

### Typography
- **Font Size**: 0.875rem (14px)
- **Line Height**: 1.4
- **Link Color**: #777777 (Medium Gray)
- **Active Color**: #0D1A80 (Deep Blue)
- **Active Weight**: 600 (Semibold)

### Separators
- **Character**: "›" (single right angle quote)
- **Size**: 1.2rem
- **Color**: #6c757d
- **Spacing**: 0 0.75rem padding

### Interactive States

#### Hover State
- **Text Color**: #0D1A80 (Deep Blue)
- **Background**: rgba(13, 26, 128, 0.05)
- **Transform**: translateY(-1px)
- **Underline**: 2px solid #0D1A80 (animated width)

#### Active State
- **Text Color**: #0D1A80 (Deep Blue)
- **Font Weight**: 600
- **No hover effects**: Current page is not clickable

### Icon Integration
- **Position**: Left of text with 0.5rem spacing
- **Color**: Inherits from text color
- **Opacity**: 0.7 default, 1.0 on hover/active
- **Size**: Same as font size

## Implementation Patterns

### Page Type Requirements

#### List Pages
```razor
Home > [Module] > [Entity List]
Example: Home > Customers > All Customers
```

#### Card Pages
```razor
Home > [Module] > [Entity List] > [Entity Name/ID]
Example: Home > Customers > All Customers > John Doe
```

#### Document Pages
```razor
Home > [Module] > [Document Type] > [Document ID/Status]
Example: Home > Orders > Sales Orders > SO-2024-001
```

#### Worksheet Pages
```razor
Home > [Module] > Worksheets > [Worksheet Type]
Example: Home > Pricing > Worksheets > Bulk Update
```

### Position in Page Layout
Breadcrumbs must be positioned:
1. **After** the StandardToolbar component
2. **Before** the main content area
3. **Within** the container-fluid wrapper
4. **Outside** any card or panel components

### Component Usage

```razor
@page "/customers/{Id:int}"
@using SteelEstimation.Web.Shared.Components

<StandardToolbar ActionProvider="@this" />

<Breadcrumb Items="@breadcrumbs" />

<div class="main-content">
    <!-- Page content -->
</div>

@code {
    private List<BreadcrumbItem> breadcrumbs = new();
    
    protected override void OnInitialized()
    {
        breadcrumbs = new List<BreadcrumbItem>
        {
            new() { Label = "Home", Url = "/", Icon = "fas fa-home" },
            new() { Label = "Customers", Url = "/customers", Icon = "fas fa-users" },
            new() { Label = customerName, IsActive = true }
        };
    }
}
```

## Responsive Design

### Desktop (≥768px)
- Full breadcrumb trail visible
- Icons displayed
- Standard spacing and sizing

### Mobile (<768px)
- Show only last 2 levels
- Reduce font size to 0.8rem
- Compress padding to 0.5rem 1rem
- Hide icons to save space
- Add "..." for truncated items

## Accessibility Requirements

### ARIA Labels
- Container: `aria-label="breadcrumb"`
- Current page: `aria-current="page"`
- Navigation structure: Semantic `<nav>` and `<ol>` elements

### Keyboard Navigation
- All links focusable via Tab key
- Focus states match hover states
- Escape key returns focus to main content

### Screen Reader Support
- Announce navigation context
- Indicate current page location
- Provide clear link descriptions

## Dark Theme Support

### Container
- **Background**: Linear gradient (135deg, #1a1a1a 0%, #2d2d2d 100%)
- **Border**: 1px solid rgba(255, 255, 255, 0.1)

### Text Colors
- **Links**: #B1B1B1 (Light Gray)
- **Active**: #5261D5 (Light Blue)
- **Separators**: #777777

### Hover States
- **Background**: rgba(82, 97, 213, 0.1)
- **Text**: #5261D5 (Light Blue)

## Best Practices

### Content Guidelines
1. Keep labels concise (2-3 words maximum)
2. Use title case for consistency
3. Avoid abbreviations unless well-known
4. Include relevant context (IDs, names) when helpful

### Navigation Hierarchy
1. Always start with "Home" as root
2. Follow the actual URL structure
3. Maximum 4 levels deep (5 including Home)
4. Use descriptive labels that match page titles

### Performance Considerations
1. Generate breadcrumbs server-side when possible
2. Cache breadcrumb data for static hierarchies
3. Lazy load icons to reduce initial bundle size
4. Use CSS transitions sparingly for smooth performance

## Integration with Page Types

### Mandatory Implementation
All four page types MUST include breadcrumbs:
- ✅ List Pages
- ✅ Card Pages
- ✅ Document Pages
- ✅ Worksheet Pages

### Consistent Positioning
Breadcrumbs appear in the same location across all page types:
1. StandardToolbar (with actions)
2. **Breadcrumb Navigation** ← Consistent position
3. Page-specific content (filters, tabs, grids, etc.)

## Module-Specific Patterns

### Estimation Module
```
Home > Estimations > [Project Name] > [Section]
Example: Home > Estimations > Steel Tower Project > Dashboard
```

### Customers Module
```
Home > Customers > [View/Action]
Example: Home > Customers > Edit Profile
```

### Settings Module
```
Home > Settings > [Category] > [Setting]
Example: Home > Settings > Display > Theme Options
```

### Admin Module
```
Home > Admin > [Section] > [Action]
Example: Home > Admin > Users > Create New
```

## Testing Checklist

### Visual Testing
- [ ] Breadcrumbs display correctly on all page types
- [ ] Colors match Fab.OS brand guidelines
- [ ] Hover states work smoothly
- [ ] Icons align properly with text
- [ ] Separators positioned consistently

### Functional Testing
- [ ] All links navigate correctly
- [ ] Active item is not clickable
- [ ] Back navigation maintains state
- [ ] Dynamic breadcrumbs update properly

### Accessibility Testing
- [ ] Keyboard navigation works
- [ ] Screen readers announce correctly
- [ ] Focus states visible
- [ ] ARIA labels present

### Responsive Testing
- [ ] Mobile view truncates appropriately
- [ ] Tablet view maintains readability
- [ ] Desktop view shows full trail
- [ ] Touch targets adequate on mobile

## Implementation Timeline

1. **Phase 1**: Create reusable Breadcrumb component
2. **Phase 2**: Implement in sample pages for validation
3. **Phase 3**: Roll out to production pages
4. **Phase 4**: Add to new page creation template

## Related Documentation
- [Fab.OS Visual Identity Guidelines](./fabos_visual_identity.md)
- [Page Types Architecture](./page-types-architecture.md)
- [Component Library Documentation](../components/README.md)