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
    const sidebar = document.querySelector('.fabos-sidebar');
    const modal = document.querySelector('.fullscreen-modal-content');

    if (!sidebar || !modal) {
        return null;
    }

    // Function to update modal position
    const updatePosition = () => {
        const isCollapsed = sidebar.classList.contains('collapsed');
        const isExpanded = sidebar.classList.contains('expanded');

        let leftOffset = 300; // default standard width + gap
        if (isCollapsed) leftOffset = 80;  // collapsed width + gap
        else if (isExpanded) leftOffset = 440; // expanded width + gap

        modal.style.left = leftOffset + 'px';
    };

    // Create MutationObserver to watch for sidebar changes
    const observer = new MutationObserver(updatePosition);

    observer.observe(sidebar, {
        attributes: true,
        attributeFilter: ['class']
    });

    // Set initial position
    updatePosition();

    // Return observer for later cleanup
    return observer;
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