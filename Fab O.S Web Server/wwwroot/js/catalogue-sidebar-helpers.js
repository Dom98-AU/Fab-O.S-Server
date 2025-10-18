/**
 * Catalogue Sidebar Helpers
 * Manages body classes for catalogue sidebar state to coordinate with modals
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

    console.log('[Catalogue Sidebar] Body classes after update:', document.body.className);
    console.log('[Catalogue Sidebar] Has modal-open:', document.body.classList.contains('modal-open'));
    console.log('[Catalogue Sidebar] Has catalogue-sidebar-open:', document.body.classList.contains('catalogue-sidebar-open'));

    // Check modal element position
    const modal = document.querySelector('.modal-fullscreen');
    const catalogueSidebar = document.querySelector('.takeoff-catalogue-sidebar');

    if (modal) {
        const modalStyle = window.getComputedStyle(modal);
        console.log('[Catalogue Sidebar] 🎯 MODAL computed left:', modalStyle.left);
        console.log('[Catalogue Sidebar] 🎯 MODAL computed z-index:', modalStyle.zIndex);

        // Check which CSS rule is being applied
        const matchedRules = [];
        if (document.body.classList.contains('modal-open')) {
            matchedRules.push('body.modal-open');
        }
        if (document.body.classList.contains('catalogue-sidebar-open')) {
            matchedRules.push('catalogue-sidebar-open');
        }
        console.log('[Catalogue Sidebar] 🎯 Active body classes for CSS:', matchedRules.join(' + '));
    }

    if (catalogueSidebar) {
        const sidebarStyle = window.getComputedStyle(catalogueSidebar);
        console.log('[Catalogue Sidebar] 📍 SIDEBAR computed left:', sidebarStyle.left);
        console.log('[Catalogue Sidebar] 📍 SIDEBAR computed z-index:', sidebarStyle.zIndex);
        console.log('[Catalogue Sidebar] 📍 SIDEBAR has .visible class:', catalogueSidebar.classList.contains('visible'));
    }

    console.log(`[Catalogue Sidebar] ====================================`);
}
