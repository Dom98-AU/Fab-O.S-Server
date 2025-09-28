/**
 * PDF Takeoff Viewer - Advanced PDF viewing with measurement and markup capabilities
 * Requires PDF.js library
 */

window.PdfTakeoffViewer = {
    // Core properties
    pdfDoc: null,
    page: null,
    canvas: null,
    context: null,
    overlayCanvas: null,
    overlayContext: null,
    viewport: null,
    scale: 1.0,
    
    // Calibration properties
    isCalibrated: false,
    calibrationPixelsPerUnit: 1.0,
    calibrationUnits: 'ft',
    isCalibrating: false,
    calibrationStartPoint: null,
    calibrationEndPoint: null,
    
    // Tool state
    currentTool: 'measure',
    isDrawing: false,
    startPoint: null,
    currentPoints: [],
    measurements: [],
    markups: [],
    
    // Event handlers
    mouseDownHandler: null,
    mouseMoveHandler: null,
    mouseUpHandler: null,
    
    /**
     * Initialize PDF.js and load PDF
     */
    async loadPdf(pdfPath) {
        try {
            // Load PDF.js library if not already loaded
            if (typeof pdfjsLib === 'undefined') {
                await this.loadPdfJsLibrary();
            }
            
            // Set worker source
            pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
            
            // Get canvas elements
            this.canvas = document.getElementById('pdf-canvas');
            this.context = this.canvas.getContext('2d');
            this.overlayCanvas = document.getElementById('overlay-canvas');
            this.overlayContext = this.overlayCanvas.getContext('2d');
            
            // Load PDF document
            const loadingTask = pdfjsLib.getDocument(pdfPath);
            this.pdfDoc = await loadingTask.promise;
            
            // Load first page
            await this.renderPage(1);
            
            // Setup event listeners
            this.setupEventListeners();
            
            console.log('PDF loaded successfully');
            
        } catch (error) {
            console.error('Error loading PDF:', error);
            throw error;
        }
    },
    
    /**
     * Load PDF.js library dynamically
     */
    async loadPdfJsLibrary() {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js';
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    },
    
    /**
     * Render PDF page
     */
    async renderPage(pageNumber) {
        try {
            this.page = await this.pdfDoc.getPage(pageNumber);
            this.viewport = this.page.getViewport({ scale: this.scale });
            
            // Set canvas dimensions
            this.canvas.height = this.viewport.height;
            this.canvas.width = this.viewport.width;
            this.overlayCanvas.height = this.viewport.height;
            this.overlayCanvas.width = this.viewport.width;
            
            // Render PDF page
            const renderContext = {
                canvasContext: this.context,
                viewport: this.viewport
            };
            
            await this.page.render(renderContext).promise;
            
            // Clear overlay
            this.clearOverlay();
            
        } catch (error) {
            console.error('Error rendering page:', error);
        }
    },
    
    /**
     * Setup mouse event listeners for interaction
     */
    setupEventListeners() {
        const container = document.getElementById('pdf-container');
        
        this.mouseDownHandler = this.onMouseDown.bind(this);
        this.mouseMoveHandler = this.onMouseMove.bind(this);
        this.mouseUpHandler = this.onMouseUp.bind(this);
        
        container.addEventListener('mousedown', this.mouseDownHandler);
        container.addEventListener('mousemove', this.mouseMoveHandler);
        container.addEventListener('mouseup', this.mouseUpHandler);
        
        // Prevent context menu
        container.addEventListener('contextmenu', (e) => e.preventDefault());
    },
    
    /**
     * Handle mouse down events
     */
    onMouseDown(event) {
        const rect = this.canvas.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        
        this.startPoint = { x, y };
        this.isDrawing = true;
        
        if (this.isCalibrating) {
            this.calibrationStartPoint = { x, y };
        } else if (this.currentTool === 'count') {
            this.addCountPoint(x, y);
        }
        
        event.preventDefault();
    },
    
    /**
     * Handle mouse move events
     */
    onMouseMove(event) {
        if (!this.isDrawing && !this.isCalibrating) return;
        
        const rect = this.canvas.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        
        if (this.isCalibrating && this.calibrationStartPoint) {
            this.drawCalibrationLine(this.calibrationStartPoint.x, this.calibrationStartPoint.y, x, y);
        } else if (this.isDrawing && this.startPoint) {
            this.drawPreview(this.startPoint.x, this.startPoint.y, x, y);
        }
        
        event.preventDefault();
    },
    
    /**
     * Handle mouse up events
     */
    onMouseUp(event) {
        if (!this.isDrawing && !this.isCalibrating) return;
        
        const rect = this.canvas.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        
        if (this.isCalibrating && this.calibrationStartPoint) {
            this.calibrationEndPoint = { x, y };
            this.finishCalibrationLine();
        } else if (this.isDrawing && this.startPoint) {
            this.finishMeasurement(this.startPoint.x, this.startPoint.y, x, y);
        }
        
        this.isDrawing = false;
        this.startPoint = null;
        
        event.preventDefault();
    },
    
    /**
     * Start calibration mode
     */
    startCalibration() {
        this.isCalibrating = true;
        this.calibrationStartPoint = null;
        this.calibrationEndPoint = null;
        this.clearOverlay();
        
        // Update cursor
        const container = document.getElementById('pdf-container');
        container.setAttribute('data-tool', 'calibrate');
    },
    
    /**
     * Set calibration with known distance
     */
    setCalibration(knownDistance) {
        if (this.calibrationStartPoint && this.calibrationEndPoint) {
            const pixelDistance = this.calculateDistance(
                this.calibrationStartPoint.x, this.calibrationStartPoint.y,
                this.calibrationEndPoint.x, this.calibrationEndPoint.y
            );
            
            this.calibrationPixelsPerUnit = pixelDistance / knownDistance;
            this.isCalibrated = true;
            this.isCalibrating = false;
            
            // Clear calibration line
            this.clearOverlay();
            
            console.log(`Calibration set: ${this.calibrationPixelsPerUnit} pixels per unit`);
            
            return this.calibrationPixelsPerUnit;
        }
        
        return 0;
    },
    
    /**
     * Set current tool
     */
    setTool(tool) {
        this.currentTool = tool;
        this.isDrawing = false;
        this.startPoint = null;
        
        // Update cursor
        const container = document.getElementById('pdf-container');
        container.setAttribute('data-tool', tool);
        
        console.log(`Tool changed to: ${tool}`);
    },
    
    /**
     * Draw calibration line preview
     */
    drawCalibrationLine(x1, y1, x2, y2) {
        this.clearOverlay();
        
        this.overlayContext.beginPath();
        this.overlayContext.moveTo(x1, y1);
        this.overlayContext.lineTo(x2, y2);
        this.overlayContext.strokeStyle = '#ff0000';
        this.overlayContext.lineWidth = 2;
        this.overlayContext.setLineDash([5, 5]);
        this.overlayContext.stroke();
        
        // Draw endpoints
        this.drawPoint(x1, y1, '#ff0000');
        this.drawPoint(x2, y2, '#ff0000');
        
        // Show distance in pixels
        const distance = this.calculateDistance(x1, y1, x2, y2);
        this.drawText(`${distance.toFixed(1)} px`, (x1 + x2) / 2, (y1 + y2) / 2 - 10, '#ff0000');
    },
    
    /**
     * Finish calibration line
     */
    finishCalibrationLine() {
        if (this.calibrationStartPoint && this.calibrationEndPoint) {
            this.drawCalibrationLine(
                this.calibrationStartPoint.x, this.calibrationStartPoint.y,
                this.calibrationEndPoint.x, this.calibrationEndPoint.y
            );
        }
    },
    
    /**
     * Draw measurement preview
     */
    drawPreview(x1, y1, x2, y2) {
        this.clearOverlay();
        
        switch (this.currentTool) {
            case 'measure':
                this.drawMeasurementPreview(x1, y1, x2, y2);
                break;
            case 'area':
                this.drawAreaPreview(x1, y1, x2, y2);
                break;
            case 'angle':
                this.drawAnglePreview(x1, y1, x2, y2);
                break;
            case 'highlight':
                this.drawHighlightPreview(x1, y1, x2, y2);
                break;
            case 'arrow':
                this.drawArrowPreview(x1, y1, x2, y2);
                break;
        }
    },
    
    /**
     * Draw linear measurement preview
     */
    drawMeasurementPreview(x1, y1, x2, y2) {
        this.overlayContext.beginPath();
        this.overlayContext.moveTo(x1, y1);
        this.overlayContext.lineTo(x2, y2);
        this.overlayContext.strokeStyle = '#007bff';
        this.overlayContext.lineWidth = 2;
        this.overlayContext.setLineDash([]);
        this.overlayContext.stroke();
        
        // Draw endpoints
        this.drawPoint(x1, y1, '#007bff');
        this.drawPoint(x2, y2, '#007bff');
        
        // Show measurement
        const pixelDistance = this.calculateDistance(x1, y1, x2, y2);
        const realDistance = this.isCalibrated ? pixelDistance / this.calibrationPixelsPerUnit : pixelDistance;
        const units = this.isCalibrated ? this.calibrationUnits : 'px';
        
        this.drawText(`${realDistance.toFixed(2)} ${units}`, (x1 + x2) / 2, (y1 + y2) / 2 - 10, '#007bff');
    },
    
    /**
     * Draw area measurement preview
     */
    drawAreaPreview(x1, y1, x2, y2) {
        this.overlayContext.beginPath();
        this.overlayContext.rect(x1, y1, x2 - x1, y2 - y1);
        this.overlayContext.strokeStyle = '#28a745';
        this.overlayContext.lineWidth = 2;
        this.overlayContext.setLineDash([]);
        this.overlayContext.stroke();
        
        // Calculate area
        const pixelArea = Math.abs((x2 - x1) * (y2 - y1));
        const realArea = this.isCalibrated ? 
            pixelArea / (this.calibrationPixelsPerUnit * this.calibrationPixelsPerUnit) : 
            pixelArea;
        const units = this.isCalibrated ? this.calibrationUnits : 'px';
        
        this.drawText(`${realArea.toFixed(2)} ${units}²`, (x1 + x2) / 2, (y1 + y2) / 2, '#28a745');
    },
    
    /**
     * Draw highlight preview
     */
    drawHighlightPreview(x1, y1, x2, y2) {
        this.overlayContext.beginPath();
        this.overlayContext.rect(x1, y1, x2 - x1, y2 - y1);
        this.overlayContext.fillStyle = 'rgba(255, 255, 0, 0.3)';
        this.overlayContext.fill();
        this.overlayContext.strokeStyle = '#ffc107';
        this.overlayContext.lineWidth = 1;
        this.overlayContext.stroke();
    },
    
    /**
     * Draw arrow preview
     */
    drawArrowPreview(x1, y1, x2, y2) {
        this.drawArrow(x1, y1, x2, y2, '#dc3545');
    },
    
    /**
     * Add count point
     */
    addCountPoint(x, y) {
        this.drawPoint(x, y, '#6f42c1', 8);
        
        // Add to measurements
        const measurement = {
            id: this.generateId(),
            type: 'Count',
            value: 1,
            units: 'items',
            coordinates: [x, y],
            notes: ''
        };
        
        this.measurements.push(measurement);
        this.notifyMeasurementAdded(measurement);
    },
    
    /**
     * Finish measurement
     */
    finishMeasurement(x1, y1, x2, y2) {
        switch (this.currentTool) {
            case 'measure':
                this.addLinearMeasurement(x1, y1, x2, y2);
                break;
            case 'area':
                this.addAreaMeasurement(x1, y1, x2, y2);
                break;
            case 'angle':
                this.addAngleMeasurement(x1, y1, x2, y2);
                break;
            case 'highlight':
                this.addHighlight(x1, y1, x2, y2);
                break;
            case 'text':
                this.addTextAnnotation(x1, y1);
                break;
            case 'arrow':
                this.addArrow(x1, y1, x2, y2);
                break;
        }
    },
    
    /**
     * Add linear measurement
     */
    addLinearMeasurement(x1, y1, x2, y2) {
        const pixelDistance = this.calculateDistance(x1, y1, x2, y2);
        const realDistance = this.isCalibrated ? pixelDistance / this.calibrationPixelsPerUnit : pixelDistance;
        const units = this.isCalibrated ? this.calibrationUnits : 'px';
        
        this.drawMeasurementPreview(x1, y1, x2, y2);
        
        const measurement = {
            id: this.generateId(),
            type: 'Linear',
            value: realDistance,
            units: units,
            coordinates: [x1, y1, x2, y2],
            notes: ''
        };
        
        this.measurements.push(measurement);
        this.notifyMeasurementAdded(measurement);
    },
    
    /**
     * Add area measurement
     */
    addAreaMeasurement(x1, y1, x2, y2) {
        const pixelArea = Math.abs((x2 - x1) * (y2 - y1));
        const realArea = this.isCalibrated ? 
            pixelArea / (this.calibrationPixelsPerUnit * this.calibrationPixelsPerUnit) : 
            pixelArea;
        const units = this.isCalibrated ? this.calibrationUnits : 'px';
        
        this.drawAreaPreview(x1, y1, x2, y2);
        
        const measurement = {
            id: this.generateId(),
            type: 'Area',
            value: realArea,
            units: units,
            coordinates: [x1, y1, x2, y2],
            notes: ''
        };
        
        this.measurements.push(measurement);
        this.notifyMeasurementAdded(measurement);
    },
    
    /**
     * Add highlight markup
     */
    addHighlight(x1, y1, x2, y2) {
        this.drawHighlightPreview(x1, y1, x2, y2);
        
        const markup = {
            id: this.generateId(),
            type: 'Highlight',
            text: '',
            color: '#ffff00',
            coordinates: [x1, y1, x2, y2]
        };
        
        this.markups.push(markup);
        this.notifyMarkupAdded(markup);
    },
    
    /**
     * Add arrow markup
     */
    addArrow(x1, y1, x2, y2) {
        this.drawArrow(x1, y1, x2, y2, '#dc3545');
        
        const markup = {
            id: this.generateId(),
            type: 'Arrow',
            text: '',
            color: '#dc3545',
            coordinates: [x1, y1, x2, y2]
        };
        
        this.markups.push(markup);
        this.notifyMarkupAdded(markup);
    },
    
    /**
     * Utility functions
     */
    calculateDistance(x1, y1, x2, y2) {
        return Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
    },
    
    drawPoint(x, y, color, radius = 4) {
        this.overlayContext.beginPath();
        this.overlayContext.arc(x, y, radius, 0, 2 * Math.PI);
        this.overlayContext.fillStyle = color;
        this.overlayContext.fill();
        this.overlayContext.strokeStyle = 'white';
        this.overlayContext.lineWidth = 2;
        this.overlayContext.stroke();
    },
    
    drawText(text, x, y, color) {
        this.overlayContext.font = '12px Arial';
        this.overlayContext.fillStyle = 'white';
        this.overlayContext.strokeStyle = 'black';
        this.overlayContext.lineWidth = 3;
        this.overlayContext.strokeText(text, x, y);
        this.overlayContext.fillStyle = color;
        this.overlayContext.fillText(text, x, y);
    },
    
    drawArrow(x1, y1, x2, y2, color) {
        const angle = Math.atan2(y2 - y1, x2 - x1);
        const arrowLength = 15;
        const arrowAngle = Math.PI / 6;
        
        // Draw line
        this.overlayContext.beginPath();
        this.overlayContext.moveTo(x1, y1);
        this.overlayContext.lineTo(x2, y2);
        this.overlayContext.strokeStyle = color;
        this.overlayContext.lineWidth = 2;
        this.overlayContext.stroke();
        
        // Draw arrowhead
        this.overlayContext.beginPath();
        this.overlayContext.moveTo(x2, y2);
        this.overlayContext.lineTo(
            x2 - arrowLength * Math.cos(angle - arrowAngle),
            y2 - arrowLength * Math.sin(angle - arrowAngle)
        );
        this.overlayContext.moveTo(x2, y2);
        this.overlayContext.lineTo(
            x2 - arrowLength * Math.cos(angle + arrowAngle),
            y2 - arrowLength * Math.sin(angle + arrowAngle)
        );
        this.overlayContext.stroke();
    },
    
    clearOverlay() {
        this.overlayContext.clearRect(0, 0, this.overlayCanvas.width, this.overlayCanvas.height);
    },
    
    generateId() {
        return Math.random().toString(36).substr(2, 9);
    },
    
    /**
     * Zoom controls
     */
    zoomIn() {
        this.scale *= 1.2;
        this.renderPage(1);
    },
    
    zoomOut() {
        this.scale /= 1.2;
        this.renderPage(1);
    },
    
    resetZoom() {
        this.scale = 1.0;
        this.renderPage(1);
    },
    
    /**
     * Export functionality
     */
    exportTakeoff(takeoffData) {
        const dataStr = JSON.stringify(takeoffData, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });
        
        const link = document.createElement('a');
        link.href = URL.createObjectURL(dataBlob);
        link.download = `takeoff-${takeoffData.DrawingNumber || 'drawing'}-${new Date().toISOString().slice(0, 10)}.json`;
        link.click();
    },
    
    /**
     * Clear all measurements and markups
     */
    clearAll() {
        this.measurements = [];
        this.markups = [];
        this.clearOverlay();
    },
    
    /**
     * Notification methods for Blazor integration
     */
    notifyMeasurementAdded(measurement) {
        if (window.DotNet) {
            DotNet.invokeMethodAsync('FabOS.WebServer', 'OnMeasurementAdded', measurement);
        }
    },
    
    notifyMarkupAdded(markup) {
        if (window.DotNet) {
            DotNet.invokeMethodAsync('FabOS.WebServer', 'OnMarkupAdded', markup);
        }
    }
};
