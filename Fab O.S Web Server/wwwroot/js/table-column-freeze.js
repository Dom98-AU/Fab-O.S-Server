// Table Column Freeze Management
window.applyFrozenColumns = function(frozenColumns) {
    const table = document.querySelector('.fabos-table');
    if (!table) return;
    
    // Reset all frozen styles first
    const allCells = table.querySelectorAll('.frozen-column');
    allCells.forEach(cell => {
        cell.classList.remove('frozen-column');
        cell.style.left = '';
        cell.style.position = '';
    });
    
    // Calculate cumulative widths for frozen columns
    let cumulativeWidth = 0;
    const columnPositions = [];
    
    frozenColumns.forEach(column => {
        const columnIndex = getColumnIndex(column.PropertyName);
        if (columnIndex >= 0) {
            columnPositions.push({
                index: columnIndex,
                left: cumulativeWidth
            });
            
            // Get actual column width
            const headerCell = table.querySelector(`th:nth-child(${columnIndex + 1})`);
            if (headerCell) {
                const width = headerCell.offsetWidth;
                cumulativeWidth += width;
            }
        }
    });
    
    // Apply frozen styles to cells
    columnPositions.forEach(pos => {
        // Header cells
        const headerCell = table.querySelector(`thead th:nth-child(${pos.index + 1})`);
        if (headerCell) {
            headerCell.classList.add('frozen-column');
            headerCell.style.left = `${pos.left}px`;
            headerCell.style.position = 'sticky';
        }
        
        // Body cells
        const bodyCells = table.querySelectorAll(`tbody td:nth-child(${pos.index + 1})`);
        bodyCells.forEach(cell => {
            cell.classList.add('frozen-column');
            cell.style.left = `${pos.left}px`;
            cell.style.position = 'sticky';
        });
    });
    
    // Add shadow indicator after last frozen column
    if (columnPositions.length > 0) {
        const lastPos = columnPositions[columnPositions.length - 1];
        const shadow = document.createElement('div');
        shadow.className = 'frozen-column-shadow';
        shadow.style.left = `${cumulativeWidth}px`;
        table.appendChild(shadow);
    }
};

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