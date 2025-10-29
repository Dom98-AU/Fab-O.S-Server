/**
 * Catalogue Sidebar Helpers (Modernized with CSS Variables)
 *
 * Manages body classes for catalogue sidebar state.
 * Positioning is now handled by CSS custom properties - no MutationObserver needed!
 *
 * CSS Variables automatically handle responsive positioning:
 * - var(--main-sidebar-width) updates when main sidebar collapses/expands
 * - Catalogue sidebar position: left: var(--main-sidebar-width)
 * - Modal position: left: calc(var(--main-sidebar-width) + var(--catalogue-sidebar-width))
 */

/**
 * Update body classes based on catalogue sidebar state
 * @param {boolean} isVisible - Whether sidebar is visible
 * @param {string} widthClass - Width class: '', 'width-expanded', or 'width-full'
 */
export function updateCatalogueSidebarState(isVisible, widthClass) {
    console.log(`[Catalogue Sidebar] ====================================`);
    console.log(`[Catalogue Sidebar] Updating state: visible=${isVisible}, width=${widthClass}`);

    // Remove all catalogue sidebar classes
    document.body.classList.remove('catalogue-sidebar-open');
    document.body.classList.remove('catalogue-sidebar-expanded');
    document.body.classList.remove('catalogue-sidebar-full');

    // Add appropriate classes if visible
    if (isVisible) {
        document.body.classList.add('catalogue-sidebar-open');

        if (widthClass === 'width-expanded') {
            document.body.classList.add('catalogue-sidebar-expanded');
        } else if (widthClass === 'width-full') {
            document.body.classList.add('catalogue-sidebar-full');
        }
    }

    // Get computed CSS variable values for debugging
    const rootStyles = getComputedStyle(document.documentElement);
    const mainSidebarWidth = rootStyles.getPropertyValue('--main-sidebar-width').trim();
    const catalogueSidebarWidth = rootStyles.getPropertyValue('--catalogue-sidebar-width').trim();

    console.log('[Catalogue Sidebar] 📏 CSS Variable --main-sidebar-width:', mainSidebarWidth);
    console.log('[Catalogue Sidebar] 📏 CSS Variable --catalogue-sidebar-width:', catalogueSidebarWidth);
    console.log('[Catalogue Sidebar] Body classes after update:', document.body.className);

    // Check element positions
    const modal = document.querySelector('.modal-fullscreen');
    const catalogueSidebar = document.querySelector('.takeoff-catalogue-sidebar');

    if (modal) {
        const modalStyle = window.getComputedStyle(modal);
        console.log('[Catalogue Sidebar] 🎯 MODAL computed left:', modalStyle.left);
    }

    if (catalogueSidebar) {
        const sidebarStyle = window.getComputedStyle(catalogueSidebar);
        console.log('[Catalogue Sidebar] 📍 SIDEBAR computed left:', sidebarStyle.left);
        console.log('[Catalogue Sidebar] 📍 SIDEBAR width:', sidebarStyle.width);
    }

    console.log(`[Catalogue Sidebar] ====================================`);
}
