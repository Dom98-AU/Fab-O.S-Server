// Modal Helper Module for Blazor

// Set body class for modal open state
export function setBodyModalOpen(isOpen) {
    if (isOpen) {
        document.body.classList.add('modal-open');
    } else {
        document.body.classList.remove('modal-open');
    }
}

export function initializeModalPositioning() {
    const modal = document.querySelector('.modal-fullscreen');

    if (!modal) {
        console.log('[Modal Positioning] Modal not found');
        return null;
    }

    console.log('[Modal Positioning] Initialized - using CSS-only positioning');

    // Clear any inline left style to allow CSS rules to take effect
    // The modal positioning is now handled entirely by CSS in modal-infrastructure.css
    // based on body classes: modal-open, sidebar-collapsed, sidebar-expanded,
    // catalogue-sidebar-open, catalogue-sidebar-expanded, catalogue-sidebar-full
    modal.style.left = '';

    // Return null as we don't need the observer anymore
    // CSS handles all positioning dynamically
    return null;
}

export function disposeModalPositioning(observer) {
    if (observer && observer.disconnect) {
        observer.disconnect();
    }
}

export function addEscapeKeyListener(dotNetRef) {
    const handler = (event) => {
        if (event.key === 'Escape' || event.keyCode === 27) {
            dotNetRef.invokeMethodAsync('CloseModals');
        }
    };

    document.addEventListener('keydown', handler);

    // Return handler for cleanup
    return handler;
}

export function removeEscapeKeyListener(handler) {
    if (handler) {
        document.removeEventListener('keydown', handler);
    }
}

export function focusElement(selector) {
    const element = document.querySelector(selector);
    if (element) {
        element.focus();
    }
}

export function trapFocus(modalSelector) {
    const modal = document.querySelector(modalSelector);
    if (!modal) return;

    const focusableElements = modal.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );

    if (focusableElements.length === 0) return;

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    const handleTab = (event) => {
        if (event.key !== 'Tab') return;

        if (event.shiftKey) {
            // Shift + Tab
            if (document.activeElement === firstElement) {
                event.preventDefault();
                lastElement.focus();
            }
        } else {
            // Tab
            if (document.activeElement === lastElement) {
                event.preventDefault();
                firstElement.focus();
            }
        }
    };

    modal.addEventListener('keydown', handleTab);

    // Focus first element
    firstElement.focus();

    // Return handler for cleanup
    return handleTab;
}

export function releaseFocusTrap(modalSelector, handler) {
    const modal = document.querySelector(modalSelector);
    if (modal && handler) {
        modal.removeEventListener('keydown', handler);
    }
}