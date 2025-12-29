/**
 * Template Sidebar Helpers
 *
 * Manages body classes for template column sidebar state.
 * Follows same pattern as catalogue-sidebar-helpers.js
 *
 * CSS Variables handle responsive positioning:
 * - var(--main-sidebar-width) updates when main sidebar collapses/expands
 * - Template sidebar position: left: var(--main-sidebar-width)
 * - Main content shift: margin-left: calc(var(--main-sidebar-width) + var(--template-sidebar-width))
 */

/**
 * Update body classes based on template sidebar state
 * @param {boolean} isVisible - Whether sidebar is visible
 */
export function updateTemplateSidebarState(isVisible) {
    console.log(`[Template Sidebar] ====================================`);
    console.log(`[Template Sidebar] Updating state: visible=${isVisible}`);

    // Remove template sidebar class
    document.body.classList.remove('template-sidebar-open');

    // Add class if visible
    if (isVisible) {
        document.body.classList.add('template-sidebar-open');
    }

    // Get computed CSS variable values for debugging
    const rootStyles = getComputedStyle(document.documentElement);
    const mainSidebarWidth = rootStyles.getPropertyValue('--main-sidebar-width').trim();
    const templateSidebarWidth = rootStyles.getPropertyValue('--template-sidebar-width').trim();

    console.log('[Template Sidebar] CSS Variable --main-sidebar-width:', mainSidebarWidth);
    console.log('[Template Sidebar] CSS Variable --template-sidebar-width:', templateSidebarWidth);
    console.log('[Template Sidebar] Body classes after update:', document.body.className);

    // Check element positions
    const templateSidebar = document.querySelector('.template-column-sidebar');
    const mainContent = document.querySelector('.fabos-main-content');

    if (templateSidebar) {
        const sidebarStyle = window.getComputedStyle(templateSidebar);
        console.log('[Template Sidebar] SIDEBAR computed left:', sidebarStyle.left);
        console.log('[Template Sidebar] SIDEBAR width:', sidebarStyle.width);
    }

    if (mainContent) {
        const contentStyle = window.getComputedStyle(mainContent);
        console.log('[Template Sidebar] MAIN CONTENT computed margin-left:', contentStyle.marginLeft);
    }

    console.log(`[Template Sidebar] ====================================`);
}

/**
 * Cleanup template sidebar state (call on component dispose)
 */
export function cleanupTemplateSidebar() {
    console.log('[Template Sidebar] Cleanup - removing body class');
    document.body.classList.remove('template-sidebar-open');
}

/**
 * Update body classes based on properties panel state
 * @param {boolean} isVisible - Whether properties panel is visible
 */
export function updatePropertiesPanelState(isVisible) {
    console.log(`[Properties Panel] ====================================`);
    console.log(`[Properties Panel] Updating state: visible=${isVisible}`);

    // Remove properties panel class
    document.body.classList.remove('properties-panel-open');

    // Add class if visible
    if (isVisible) {
        document.body.classList.add('properties-panel-open');
    }

    // Get computed CSS variable values for debugging
    const rootStyles = getComputedStyle(document.documentElement);
    const propertiesPanelWidth = rootStyles.getPropertyValue('--properties-panel-width').trim();

    console.log('[Properties Panel] CSS Variable --properties-panel-width:', propertiesPanelWidth);
    console.log('[Properties Panel] Body classes after update:', document.body.className);

    // Check element positions
    const propertiesPanel = document.querySelector('.column-properties-panel');
    const mainContent = document.querySelector('.fabos-main-content');

    if (propertiesPanel) {
        const panelStyle = window.getComputedStyle(propertiesPanel);
        console.log('[Properties Panel] PANEL computed right:', panelStyle.right);
        console.log('[Properties Panel] PANEL width:', panelStyle.width);
    }

    if (mainContent) {
        const contentStyle = window.getComputedStyle(mainContent);
        console.log('[Properties Panel] MAIN CONTENT computed margin-right:', contentStyle.marginRight);
    }

    console.log(`[Properties Panel] ====================================`);
}

/**
 * Cleanup properties panel state (call on component dispose)
 */
export function cleanupPropertiesPanel() {
    console.log('[Properties Panel] Cleanup - removing body class');
    document.body.classList.remove('properties-panel-open');
}
