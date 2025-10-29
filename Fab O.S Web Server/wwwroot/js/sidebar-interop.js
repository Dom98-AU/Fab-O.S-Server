/**
 * FabOS Sidebar JavaScript Interop
 * Enterprise-grade sidebar state management with CSS variables
 *
 * Architecture:
 * 1. Blazor C# calls updateSidebarBodyClass() via JSInterop
 * 2. JavaScript updates body classes (sidebar-collapsed, sidebar-expanded)
 * 3. CSS rules update CSS variables (--main-sidebar-width: 60px or 280px or 420px)
 * 4. All sidebars and modals use var(--main-sidebar-width) and automatically reposition
 */

// Track sidebar state in JavaScript
window.sidebarState = {
    isCollapsed: false,
    isExpanded: false
};

/**
 * Update sidebar body classes and CSS variables
 * Called by Blazor via JSInterop
 */
window.updateSidebarBodyClass = (isCollapsed, isExpanded) => {
    console.log('[Sidebar] updateSidebarBodyClass called:', { isCollapsed, isExpanded });

    // Update JavaScript state
    window.sidebarState.isCollapsed = isCollapsed;
    window.sidebarState.isExpanded = isExpanded;

    // Update body classes
    document.body.classList.remove('sidebar-collapsed', 'sidebar-expanded');
    if (isCollapsed) {
        document.body.classList.add('sidebar-collapsed');
        console.log('[Sidebar] Added sidebar-collapsed class');
    } else if (isExpanded) {
        document.body.classList.add('sidebar-expanded');
        console.log('[Sidebar] Added sidebar-expanded class');
    } else {
        console.log('[Sidebar] Removed all sidebar state classes (normal state)');
    }

    // Verify CSS variable was updated by the CSS rule
    const bodyStyles = getComputedStyle(document.body);
    const mainSidebarWidth = bodyStyles.getPropertyValue('--main-sidebar-width').trim();
    console.log('[Sidebar] CSS variable --main-sidebar-width (from body):', mainSidebarWidth);

    // Update the sidebar element's visual state
    const sidebar = document.querySelector('.fabos-sidebar');
    if (sidebar) {
        sidebar.classList.remove('collapsed', 'expanded');
        if (isCollapsed) {
            sidebar.classList.add('collapsed');
        } else if (isExpanded) {
            sidebar.classList.add('expanded');
        }
    }
};

/**
 * Toggle sidebar state (DEPRECATED - now handled by Blazor @onclick)
 * This function is kept for backward compatibility but is no longer used.
 */
window.handleSidebarToggle = (event) => {
    console.warn('[Sidebar] ⚠️ DEPRECATED: handleSidebarToggle called, but should be handled by Blazor now!');
};

/**
 * Initialize sidebar interop when DOM is ready
 * NOTE: Hamburger click is now handled by Blazor @onclick, so we don't attach a JS click handler anymore.
 * This function is kept for potential future enhancements.
 */
function initializeSidebarInterop() {
    console.log('[Sidebar] Initializing sidebar interop...');

    // Verify the hamburger button exists (for debugging)
    const hamburger = document.querySelector('.fabos-hamburger-button');
    if (hamburger) {
        console.log('[Sidebar] ✅ Hamburger button found in DOM');
        console.log('[Sidebar] 🎯 Button element:', hamburger);
        console.log('[Sidebar] 📍 Button bounding rect:', hamburger.getBoundingClientRect());

        // Add debugging listener (does NOT interfere with Blazor - no preventDefault!)
        hamburger.addEventListener('click', (e) => {
            console.log('[Sidebar DEBUG] 🔍 Click event detected:', {
                target: e.target,
                currentTarget: e.currentTarget,
                tagName: e.target.tagName,
                className: e.target.className,
                defaultPrevented: e.defaultPrevented,
                propagationStopped: e.cancelBubble
            });
            console.log('[Sidebar DEBUG] ⏱️ Waiting for Blazor to handle this click...');
        }, false); // Changed to bubble phase so Blazor handles it first
    } else {
        console.log('[Sidebar] Hamburger button not found yet, will retry in 100ms...');
        setTimeout(initializeSidebarInterop, 100);
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeSidebarInterop);
} else {
    // DOM already loaded
    initializeSidebarInterop();
}

// Re-initialize after Blazor Enhanced Navigation (page transitions)
if (window.Blazor) {
    Blazor.addEventListener('enhancedload', () => {
        console.log('[Sidebar] Blazor enhanced navigation detected, re-initializing...');
        initializeSidebarInterop();
    });
}

console.log('[Sidebar] sidebar-interop.js loaded');
