// Table Column Freeze Management
window.applyFrozenColumns = function(frozenColumns) {
    const table = document.querySelector('.fabos-table');
    if (!table) return;

    // Clear any existing frozen states
    clearFrozenColumns();

    if (!frozenColumns || frozenColumns.length === 0) {
        return;
    }

    // Group frozen columns by position
    const leftFrozen = frozenColumns.filter(col => col.FreezePosition === 'Left').sort((a, b) => a.Order - b.Order);
    const rightFrozen = frozenColumns.filter(col => col.FreezePosition === 'Right').sort((a, b) => a.Order - b.Order);

    // Apply left frozen columns
    applyLeftFrozenColumns(table, leftFrozen);

    // Apply right frozen columns
    applyRightFrozenColumns(table, rightFrozen);
};

// Clear all frozen column styles
window.clearFrozenColumns = function() {
    const table = document.querySelector('.fabos-table');
    if (!table) return;

    // Remove all frozen classes and styles
    const allCells = table.querySelectorAll('.frozen-column, .frozen-left, .frozen-right');
    allCells.forEach(cell => {
        cell.classList.remove('frozen-column', 'frozen-left', 'frozen-right');
        cell.style.left = '';
        cell.style.right = '';
        cell.style.position = '';
        cell.style.zIndex = '';
    });

    // Remove shadow indicators
    const shadows = table.querySelectorAll('.frozen-column-shadow');
    shadows.forEach(shadow => shadow.remove());
};

function applyLeftFrozenColumns(table, leftFrozen) {
    let cumulativeWidth = 0;

    leftFrozen.forEach((column, index) => {
        const columnElements = getColumnElementsByProperty(table, column.PropertyName);
        if (columnElements.length === 0) return;

        const headerCell = columnElements.header;
        const bodyCells = columnElements.bodyCells;

        // Calculate width from the actual rendered header
        const width = headerCell ? headerCell.offsetWidth : 0;

        // Apply frozen styles
        if (headerCell) {
            headerCell.classList.add('frozen-column', 'frozen-left');
            headerCell.style.left = `${cumulativeWidth}px`;
            headerCell.style.position = 'sticky';
            headerCell.style.zIndex = `${10 + index}`;
        }

        bodyCells.forEach(cell => {
            cell.classList.add('frozen-column', 'frozen-left');
            cell.style.left = `${cumulativeWidth}px`;
            cell.style.position = 'sticky';
            cell.style.zIndex = `${10 + index}`;
        });

        cumulativeWidth += width;
    });

    // Add shadow indicator after last left frozen column
    if (leftFrozen.length > 0 && cumulativeWidth > 0) {
        addShadowIndicator(table, 'left', cumulativeWidth);
    }
}

function applyRightFrozenColumns(table, rightFrozen) {
    let cumulativeWidth = 0;

    // Process right frozen columns in reverse order
    for (let i = rightFrozen.length - 1; i >= 0; i--) {
        const column = rightFrozen[i];
        const columnElements = getColumnElementsByProperty(table, column.PropertyName);
        if (columnElements.length === 0) continue;

        const headerCell = columnElements.header;
        const bodyCells = columnElements.bodyCells;

        // Calculate width from the actual rendered header
        const width = headerCell ? headerCell.offsetWidth : 0;

        // Apply frozen styles
        if (headerCell) {
            headerCell.classList.add('frozen-column', 'frozen-right');
            headerCell.style.right = `${cumulativeWidth}px`;
            headerCell.style.position = 'sticky';
            headerCell.style.zIndex = `${10 + i}`;
        }

        bodyCells.forEach(cell => {
            cell.classList.add('frozen-column', 'frozen-right');
            cell.style.right = `${cumulativeWidth}px`;
            cell.style.position = 'sticky';
            cell.style.zIndex = `${10 + i}`;
        });

        cumulativeWidth += width;
    }

    // Add shadow indicator before first right frozen column
    if (rightFrozen.length > 0 && cumulativeWidth > 0) {
        addShadowIndicator(table, 'right', cumulativeWidth);
    }
}

function getColumnElementsByProperty(table, propertyName) {
    // Find header cell by data-column attribute
    const headerCell = table.querySelector(`thead th[data-column="${propertyName}"]`);
    if (!headerCell) {
        return { header: null, bodyCells: [] };
    }

    // Get column index from header position
    const headerCells = Array.from(table.querySelectorAll('thead th'));
    const columnIndex = headerCells.indexOf(headerCell);

    if (columnIndex === -1) {
        return { header: headerCell, bodyCells: [] };
    }

    // Get all body cells in this column
    const bodyCells = Array.from(table.querySelectorAll(`tbody td:nth-child(${columnIndex + 1})`));

    return {
        header: headerCell,
        bodyCells: bodyCells
    };
}

function addShadowIndicator(table, side, offset) {
    const shadow = document.createElement('div');
    shadow.className = `frozen-column-shadow frozen-shadow-${side}`;

    if (side === 'left') {
        shadow.style.left = `${offset}px`;
    } else {
        shadow.style.right = `${offset}px`;
    }

    shadow.style.position = 'absolute';
    shadow.style.top = '0';
    shadow.style.bottom = '0';
    shadow.style.width = '6px';
    shadow.style.pointerEvents = 'none';
    shadow.style.background = 'linear-gradient(90deg, rgba(0,0,0,0.1), transparent)';
    shadow.style.zIndex = '5';

    table.appendChild(shadow);
}

// Legacy function - replaced by getColumnElementsByProperty
function getColumnIndex(propertyName) {
    const table = document.querySelector('.fabos-table');
    if (!table) return -1;

    const headers = table.querySelectorAll('thead th');
    for (let i = 0; i < headers.length; i++) {
        if (headers[i].getAttribute('data-column') === propertyName) {
            return i;
        }
    }
    return -1;
}

// Recalculate frozen columns when table structure changes
window.recalculateFrozenColumns = function() {
    const table = document.querySelector('.fabos-table');
    if (!table) return;

    // Get currently frozen columns
    const frozenHeaders = table.querySelectorAll('thead th.frozen-column');
    const frozenColumns = [];

    frozenHeaders.forEach(header => {
        const propertyName = header.getAttribute('data-column');
        const isLeft = header.classList.contains('frozen-left');
        const isRight = header.classList.contains('frozen-right');

        if (propertyName) {
            frozenColumns.push({
                PropertyName: propertyName,
                FreezePosition: isLeft ? 'Left' : (isRight ? 'Right' : 'None'),
                Order: Array.from(header.parentElement.children).indexOf(header)
            });
        }
    });

    // Reapply frozen columns
    if (frozenColumns.length > 0) {
        applyFrozenColumns(frozenColumns);
    }
};

// Column resize functionality
window.initColumnResize = function() {
    const table = document.querySelector('.fabos-table');
    if (!table) return;
    
    const headers = table.querySelectorAll('thead th');
    headers.forEach((header, index) => {
        const resizeHandle = document.createElement('div');
        resizeHandle.className = 'column-resize-handle';
        header.appendChild(resizeHandle);
        
        let startX = 0;
        let startWidth = 0;
        
        resizeHandle.addEventListener('mousedown', (e) => {
            startX = e.clientX;
            startWidth = header.offsetWidth;
            resizeHandle.classList.add('resizing');
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
        });
        
        function handleMouseMove(e) {
            const diff = e.clientX - startX;
            const newWidth = Math.max(50, startWidth + diff);
            header.style.width = `${newWidth}px`;
            
            // Update corresponding body cells
            const bodyCells = table.querySelectorAll(`tbody td:nth-child(${index + 1})`);
            bodyCells.forEach(cell => {
                cell.style.width = `${newWidth}px`;
            });
        }
        
        function handleMouseUp() {
            resizeHandle.classList.remove('resizing');
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
            
            // Trigger column width changed event
            const event = new CustomEvent('columnResized', {
                detail: {
                    columnIndex: index,
                    newWidth: header.offsetWidth
                }
            });
            table.dispatchEvent(event);
        }
    });
};

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initColumnResize);
} else {
    initColumnResize();
}

// Observe table changes to recalculate frozen columns
if (typeof MutationObserver !== 'undefined') {
    const observer = new MutationObserver(function(mutations) {
        let shouldRecalculate = false;

        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList' ||
                (mutation.type === 'attributes' &&
                 (mutation.attributeName === 'class' || mutation.attributeName === 'style'))) {
                shouldRecalculate = true;
            }
        });

        if (shouldRecalculate) {
            // Debounce the recalculation
            clearTimeout(window.frozenRecalcTimeout);
            window.frozenRecalcTimeout = setTimeout(recalculateFrozenColumns, 100);
        }
    });

    // Start observing table changes
    document.addEventListener('DOMContentLoaded', function() {
        const table = document.querySelector('.fabos-table');
        if (table) {
            observer.observe(table, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['class', 'style']
            });
        }
    });
}