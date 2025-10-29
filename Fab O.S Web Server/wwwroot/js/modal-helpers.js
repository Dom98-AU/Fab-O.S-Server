/**
 * Modal Helper Functions for EmbeddableListPart
 * Provides immediate focus management and accessibility for modal components
 */

window.focusElement = (element) => {
    try {
        if (element && element.focus) {
            // Set focus immediately for instant visibility
            element.focus();

            // Scroll element into view if needed
            element.scrollIntoView({
                behavior: 'instant',
                block: 'center',
                inline: 'center'
            });
        }
    } catch (error) {
        console.warn('Failed to focus modal element:', error);
    }
};

// Blur element to exit hover state
window.blurElement = (element) => {
    try {
        if (element && element.blur) {
            element.blur();

            // Also remove focus and trigger reflow
            element.style.pointerEvents = 'none';
            element.offsetHeight; // Force reflow
            element.style.pointerEvents = '';
        }
    } catch (error) {
        console.warn('Failed to blur element:', error);
    }
};

// Handle ESC key to close modal
window.setupModalKeyHandlers = (element, dotNetObjectRef) => {
    const handleKeyDown = (event) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            if (dotNetObjectRef) {
                dotNetObjectRef.invokeMethodAsync('CloseModal');
            }
        }
    };

    if (element) {
        element.addEventListener('keydown', handleKeyDown);

        // Return cleanup function
        return () => {
            element.removeEventListener('keydown', handleKeyDown);
        };
    }

    return null;
};

// Ensure modal is visible immediately when created
window.ensureModalVisibility = (element) => {
    try {
        if (element) {
            // Force immediate visibility with DOM manipulation
            element.style.display = 'flex';
            element.style.opacity = '1';
            element.style.visibility = 'visible';
            element.style.pointerEvents = 'auto';
            element.style.transform = 'scale(1)';
            element.style.zIndex = '2147483647';

            // Multiple reflow triggers for cross-browser compatibility
            element.offsetHeight;
            element.getBoundingClientRect();

            // Force repaint with a micro task
            requestAnimationFrame(() => {
                element.style.willChange = 'auto';
                element.offsetHeight;

                // Additional browser-specific fixes
                if (element.style.webkitTransform !== undefined) {
                    element.style.webkitTransform = 'translateZ(0)';
                }

                // Trigger one more repaint
                requestAnimationFrame(() => {
                    element.offsetHeight;
                });
            });
        }
    } catch (error) {
        console.warn('Failed to ensure modal visibility:', error);
    }
};

// Enhanced modal visibility with hover state management
window.ensureModalVisibilityWithHoverFix = (modalElement, buttonElement) => {
    try {
        if (modalElement) {
            // First ensure button is completely out of hover state
            if (buttonElement) {
                buttonElement.blur();

                // Temporarily disable all pointer events on button
                buttonElement.style.pointerEvents = 'none';

                // Create synthetic mouse move event away from button
                const rect = buttonElement.getBoundingClientRect();
                const mouseEvent = new MouseEvent('mousemove', {
                    clientX: rect.right + 100,
                    clientY: rect.bottom + 100,
                    bubbles: true
                });
                document.dispatchEvent(mouseEvent);
            }

            // Force immediate visibility with DOM manipulation
            modalElement.style.display = 'flex';
            modalElement.style.opacity = '1';
            modalElement.style.visibility = 'visible';
            modalElement.style.pointerEvents = 'auto';
            modalElement.style.transform = 'scale(1)';
            modalElement.style.zIndex = '2147483647';

            // Enhanced reflow triggers with multiple methods
            modalElement.offsetHeight;
            modalElement.getBoundingClientRect();
            modalElement.scrollTop; // Additional reflow trigger

            // Force immediate paint with multiple requestAnimationFrame calls
            requestAnimationFrame(() => {
                modalElement.style.willChange = 'auto';
                modalElement.offsetHeight;

                // Browser-specific hardware acceleration
                if (modalElement.style.webkitTransform !== undefined) {
                    modalElement.style.webkitTransform = 'translateZ(0)';
                }

                // Additional repaint cycles for stubborn browsers
                requestAnimationFrame(() => {
                    modalElement.offsetHeight;
                    modalElement.style.transform = 'translateZ(0) scale(1)';

                    requestAnimationFrame(() => {
                        modalElement.offsetHeight;

                        // Re-enable button pointer events after modal is visible
                        if (buttonElement) {
                            setTimeout(() => {
                                buttonElement.style.pointerEvents = '';
                            }, 50);
                        }
                    });
                });
            });
        }
    } catch (error) {
        console.warn('Failed to ensure modal visibility with hover fix:', error);
    }
};

/**
 * Download a file from base64 data
 * Used for Excel export and other file downloads
 */
window.downloadFileFromBase64 = (fileName, base64Data) => {
    try {
        // Convert base64 to blob
        const binaryString = window.atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        const blob = new Blob([bytes], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;

        // Trigger download
        document.body.appendChild(link);
        link.click();

        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);

        console.log(`File downloaded successfully: ${fileName}`);
    } catch (error) {
        console.error('Failed to download file:', error);
        throw error;
    }
};