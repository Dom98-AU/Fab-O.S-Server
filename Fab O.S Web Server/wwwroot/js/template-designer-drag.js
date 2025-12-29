/**
 * FabOS Template Designer Drag-Drop JavaScript Interop
 * Handles drag-drop for adding columns from sidebar and reordering columns
 */

window.templateDesigner = {
    dotNetRef: null,
    draggedColumn: null,
    draggedFieldData: null,

    /**
     * Initialize the template designer with a DotNet reference
     */
    initialize: function (dotNetReference) {
        console.log('[TemplateDesigner] Initializing drag-drop handlers...');
        this.dotNetRef = dotNetReference;
        this.setupDropZones();
        return true;
    },

    /**
     * Set up drop zones for the spreadsheet
     */
    setupDropZones: function () {
        const container = document.querySelector('.spreadsheet-container');
        if (!container) {
            console.log('[TemplateDesigner] Spreadsheet container not found, retrying...');
            setTimeout(() => this.setupDropZones(), 100);
            return;
        }

        container.addEventListener('dragover', (e) => this.handleDragOver(e));
        container.addEventListener('drop', (e) => this.handleDrop(e));
        container.addEventListener('dragleave', (e) => this.handleDragLeave(e));

        console.log('[TemplateDesigner] Drop zones initialized');
    },

    /**
     * Handle drag start from sidebar field item
     */
    handleFieldDragStart: function (e, fieldData) {
        console.log('[TemplateDesigner] Field drag start:', fieldData);
        this.draggedFieldData = fieldData;
        this.draggedColumn = null;

        e.dataTransfer.setData('application/json', JSON.stringify(fieldData));
        e.dataTransfer.setData('text/plain', fieldData.fieldName);
        e.dataTransfer.effectAllowed = 'copy';

        // Add dragging class for visual feedback
        e.target.classList.add('dragging');
    },

    /**
     * Handle drag start from column header for reordering
     */
    handleColumnDragStart: function (e, columnId, displayOrder) {
        console.log('[TemplateDesigner] Column drag start:', { columnId, displayOrder });
        this.draggedColumn = { id: columnId, displayOrder: displayOrder };
        this.draggedFieldData = null;

        e.dataTransfer.setData('text/plain', columnId.toString());
        e.dataTransfer.setData('column-id', columnId.toString());
        e.dataTransfer.effectAllowed = 'move';

        e.target.classList.add('dragging');
    },

    /**
     * Handle drag end
     */
    handleDragEnd: function (e) {
        console.log('[TemplateDesigner] Drag end');
        e.target.classList.remove('dragging');
        this.draggedColumn = null;
        this.draggedFieldData = null;

        // Remove all drag-over indicators
        document.querySelectorAll('.drag-over, .drag-over-left, .drag-over-right').forEach(el => {
            el.classList.remove('drag-over', 'drag-over-left', 'drag-over-right');
        });
    },

    /**
     * Handle drag over the spreadsheet
     */
    handleDragOver: function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = this.draggedColumn ? 'move' : 'copy';

        const header = e.target.closest('.column-header');
        const addZone = e.target.closest('.column-add-zone');

        // Clear previous indicators
        document.querySelectorAll('.drag-over, .drag-over-left, .drag-over-right').forEach(el => {
            el.classList.remove('drag-over', 'drag-over-left', 'drag-over-right');
        });

        if (addZone) {
            addZone.classList.add('drag-over');
        } else if (header) {
            // Determine if dropping on left or right side of header
            const rect = header.getBoundingClientRect();
            const midpoint = rect.left + rect.width / 2;
            if (e.clientX < midpoint) {
                header.classList.add('drag-over-left');
            } else {
                header.classList.add('drag-over-right');
            }
        }
    },

    /**
     * Handle drag leave
     */
    handleDragLeave: function (e) {
        const header = e.target.closest('.column-header');
        const addZone = e.target.closest('.column-add-zone');

        if (header) {
            header.classList.remove('drag-over-left', 'drag-over-right');
        }
        if (addZone) {
            addZone.classList.remove('drag-over');
        }
    },

    /**
     * Handle drop on spreadsheet
     */
    handleDrop: function (e) {
        e.preventDefault();
        console.log('[TemplateDesigner] Drop event');

        // Remove all drag indicators
        document.querySelectorAll('.drag-over, .drag-over-left, .drag-over-right').forEach(el => {
            el.classList.remove('drag-over', 'drag-over-left', 'drag-over-right');
        });

        const header = e.target.closest('.column-header');
        const addZone = e.target.closest('.column-add-zone');
        const dropIndex = this.getDropIndex(e);

        if (this.draggedFieldData) {
            // Dropping a new field from sidebar
            console.log('[TemplateDesigner] Dropping new field at index:', dropIndex);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('HandleFieldDropped', this.draggedFieldData, dropIndex);
            }
        } else if (this.draggedColumn) {
            // Reordering existing column
            console.log('[TemplateDesigner] Reordering column to index:', dropIndex);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('HandleColumnReorder', this.draggedColumn.id, dropIndex);
            }
        }

        this.draggedColumn = null;
        this.draggedFieldData = null;
    },

    /**
     * Get the drop index based on mouse position
     */
    getDropIndex: function (e) {
        const headers = document.querySelectorAll('.column-header');
        const addZone = e.target.closest('.column-add-zone');

        // If dropping on the add zone, return the last index + 1
        if (addZone) {
            return headers.length;
        }

        // Find which header we're dropping on or near
        for (let i = 0; i < headers.length; i++) {
            const header = headers[i];
            const rect = header.getBoundingClientRect();

            if (e.clientX >= rect.left && e.clientX <= rect.right) {
                // Dropping on this header - determine left or right side
                const midpoint = rect.left + rect.width / 2;
                return e.clientX < midpoint ? i : i + 1;
            }
        }

        // Default to end
        return headers.length;
    },

    /**
     * Scroll spreadsheet to show a specific column
     */
    scrollToColumn: function (columnIndex) {
        const headers = document.querySelectorAll('.column-header');
        if (columnIndex >= 0 && columnIndex < headers.length) {
            headers[columnIndex].scrollIntoView({ behavior: 'smooth', inline: 'center' });
        }
    },

    /**
     * Highlight a column temporarily
     */
    highlightColumn: function (columnIndex) {
        const headers = document.querySelectorAll('.column-header');
        if (columnIndex >= 0 && columnIndex < headers.length) {
            const header = headers[columnIndex];
            header.classList.add('highlight');
            setTimeout(() => header.classList.remove('highlight'), 1000);
        }
    },

    /**
     * Clean up when leaving the page
     */
    dispose: function () {
        console.log('[TemplateDesigner] Disposing drag-drop handlers');
        this.dotNetRef = null;
        this.draggedColumn = null;
        this.draggedFieldData = null;
    }
};

console.log('[TemplateDesigner] template-designer-drag.js loaded');
