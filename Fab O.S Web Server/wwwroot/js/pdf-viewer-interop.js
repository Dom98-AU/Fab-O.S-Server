// PDF Viewer JavaScript Interop for Takeoff UI
window.pdfViewerInterop = {
    pdfDoc: null,
    pageNum: 1,
    pageRendering: false,
    pageNumPending: null,
    scale: 1.5,
    canvas: null,
    ctx: null,
    dotNetRef: null,
    currentPage: null,
    overlayCanvas: null,
    overlayCtx: null,

    // Measurement state
    measurementMode: null,
    measurementPoints: [],
    currentMeasurement: null,
    measurements: [],
    pixelsPerUnit: 1,
    calibrationScale: 1,

    // Initialize the PDF viewer
    initialize: function(canvasId, overlayCanvasId, dotNetReference) {
        this.canvas = document.getElementById(canvasId);
        this.overlayCanvas = document.getElementById(overlayCanvasId);

        if (!this.canvas || !this.overlayCanvas) {
            console.error('Canvas elements not found');
            return false;
        }

        this.ctx = this.canvas.getContext('2d');
        this.overlayCtx = this.overlayCanvas.getContext('2d');
        this.dotNetRef = dotNetReference;

        // Set up overlay canvas events
        this.setupOverlayEvents();

        return true;
    },

    // Load PDF from URL
    loadPdfFromUrl: async function(url) {
        try {
            console.log('Loading PDF from:', url);

            // Load the PDF
            const loadingTask = pdfjsLib.getDocument(url);
            this.pdfDoc = await loadingTask.promise;

            console.log('PDF loaded. Total pages:', this.pdfDoc.numPages);

            // Initial page render
            await this.renderPage(this.pageNum);

            // Notify Blazor
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnPdfLoaded', this.pdfDoc.numPages);
            }

            return true;
        } catch (error) {
            console.error('Error loading PDF:', error);
            return false;
        }
    },

    // Render specific page
    renderPage: async function(num) {
        if (!this.pdfDoc) return;

        this.pageRendering = true;

        try {
            // Get page
            const page = await this.pdfDoc.getPage(num);
            this.currentPage = page;

            const viewport = page.getViewport({ scale: this.scale });

            // Set canvas dimensions
            this.canvas.width = viewport.width;
            this.canvas.height = viewport.height;
            this.overlayCanvas.width = viewport.width;
            this.overlayCanvas.height = viewport.height;

            // Render PDF page
            const renderContext = {
                canvasContext: this.ctx,
                viewport: viewport
            };

            await page.render(renderContext).promise;

            this.pageRendering = false;

            // Redraw measurements
            this.redrawMeasurements();

            if (this.pageNumPending !== null) {
                const pending = this.pageNumPending;
                this.pageNumPending = null;
                await this.renderPage(pending);
            }
        } catch (error) {
            console.error('Error rendering page:', error);
            this.pageRendering = false;
        }
    },

    // Navigation functions
    previousPage: function() {
        if (this.pageNum <= 1) return;
        this.pageNum--;
        this.queueRenderPage(this.pageNum);
    },

    nextPage: function() {
        if (!this.pdfDoc || this.pageNum >= this.pdfDoc.numPages) return;
        this.pageNum++;
        this.queueRenderPage(this.pageNum);
    },

    goToPage: function(pageNum) {
        if (!this.pdfDoc || pageNum < 1 || pageNum > this.pdfDoc.numPages) return;
        this.pageNum = pageNum;
        this.queueRenderPage(this.pageNum);
    },

    queueRenderPage: function(num) {
        if (this.pageRendering) {
            this.pageNumPending = num;
        } else {
            this.renderPage(num);
        }
    },

    // Zoom functions
    zoomIn: function() {
        this.scale = this.scale * 1.2;
        this.queueRenderPage(this.pageNum);
    },

    zoomOut: function() {
        this.scale = this.scale / 1.2;
        this.queueRenderPage(this.pageNum);
    },

    fitToWidth: function() {
        if (!this.currentPage) return;

        const container = this.canvas.parentElement;
        const pageViewport = this.currentPage.getViewport({ scale: 1 });
        this.scale = container.clientWidth / pageViewport.width;
        this.queueRenderPage(this.pageNum);
    },

    fitToHeight: function() {
        if (!this.currentPage) return;

        const container = this.canvas.parentElement;
        const pageViewport = this.currentPage.getViewport({ scale: 1 });
        this.scale = container.clientHeight / pageViewport.height;
        this.queueRenderPage(this.pageNum);
    },

    // Measurement functions
    setMeasurementMode: function(mode) {
        this.measurementMode = mode;
        this.measurementPoints = [];
        this.currentMeasurement = null;

        // Clear overlay
        this.clearOverlay();
        this.redrawMeasurements();

        // Update cursor
        if (mode) {
            this.overlayCanvas.style.cursor = 'crosshair';
        } else {
            this.overlayCanvas.style.cursor = 'default';
        }
    },

    setupOverlayEvents: function() {
        const self = this;

        this.overlayCanvas.addEventListener('click', function(e) {
            if (!self.measurementMode) return;

            const rect = self.overlayCanvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            self.handleMeasurementClick(x, y);
        });

        this.overlayCanvas.addEventListener('mousemove', function(e) {
            if (!self.measurementMode || self.measurementPoints.length === 0) return;

            const rect = self.overlayCanvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            self.drawPreviewLine(x, y);
        });

        this.overlayCanvas.addEventListener('contextmenu', function(e) {
            e.preventDefault();
            if (self.measurementMode) {
                self.finishCurrentMeasurement();
            }
        });
    },

    handleMeasurementClick: function(x, y) {
        this.measurementPoints.push({ x: x, y: y });

        if (this.measurementMode === 'linear') {
            if (this.measurementPoints.length === 2) {
                this.finishLinearMeasurement();
            }
        } else if (this.measurementMode === 'area') {
            // Draw current polygon
            this.drawPolygon(this.measurementPoints, false);
        } else if (this.measurementMode === 'count') {
            this.addCountMeasurement(x, y);
        }
    },

    finishLinearMeasurement: function() {
        if (this.measurementPoints.length < 2) return;

        const p1 = this.measurementPoints[0];
        const p2 = this.measurementPoints[1];

        const distance = Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
        const realDistance = (distance / this.pixelsPerUnit) * this.calibrationScale;

        const measurement = {
            type: 'linear',
            points: [...this.measurementPoints],
            value: realDistance,
            page: this.pageNum
        };

        this.measurements.push(measurement);
        this.measurementPoints = [];

        // Draw the measurement
        this.drawLineMeasurement(p1, p2, realDistance);

        // Notify Blazor
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnMeasurementAdded', measurement);
        }
    },

    finishCurrentMeasurement: function() {
        if (this.measurementMode === 'area' && this.measurementPoints.length >= 3) {
            this.finishAreaMeasurement();
        }

        this.measurementPoints = [];
    },

    finishAreaMeasurement: function() {
        if (this.measurementPoints.length < 3) return;

        // Calculate area using shoelace formula
        let area = 0;
        const n = this.measurementPoints.length;

        for (let i = 0; i < n; i++) {
            const j = (i + 1) % n;
            area += this.measurementPoints[i].x * this.measurementPoints[j].y;
            area -= this.measurementPoints[j].x * this.measurementPoints[i].y;
        }

        area = Math.abs(area / 2);
        const realArea = (area / (this.pixelsPerUnit * this.pixelsPerUnit)) * (this.calibrationScale * this.calibrationScale);

        const measurement = {
            type: 'area',
            points: [...this.measurementPoints],
            value: realArea,
            page: this.pageNum
        };

        this.measurements.push(measurement);

        // Draw the measurement
        this.drawPolygon(this.measurementPoints, true);

        // Notify Blazor
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnMeasurementAdded', measurement);
        }
    },

    addCountMeasurement: function(x, y) {
        const measurement = {
            type: 'count',
            points: [{ x: x, y: y }],
            value: 1,
            page: this.pageNum
        };

        this.measurements.push(measurement);

        // Draw count marker
        this.drawCountMarker(x, y, this.measurements.filter(m => m.type === 'count').length);

        // Notify Blazor
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnMeasurementAdded', measurement);
        }

        this.measurementPoints = [];
    },

    // Drawing functions
    clearOverlay: function() {
        this.overlayCtx.clearRect(0, 0, this.overlayCanvas.width, this.overlayCanvas.height);
    },

    redrawMeasurements: function() {
        this.clearOverlay();

        let countIndex = 1;
        for (const measurement of this.measurements) {
            if (measurement.page !== this.pageNum) continue;

            if (measurement.type === 'linear') {
                this.drawLineMeasurement(measurement.points[0], measurement.points[1], measurement.value);
            } else if (measurement.type === 'area') {
                this.drawPolygon(measurement.points, true);
            } else if (measurement.type === 'count') {
                this.drawCountMarker(measurement.points[0].x, measurement.points[0].y, countIndex++);
            }
        }
    },

    drawLineMeasurement: function(p1, p2, value) {
        this.overlayCtx.strokeStyle = '#FF0000';
        this.overlayCtx.lineWidth = 2;
        this.overlayCtx.beginPath();
        this.overlayCtx.moveTo(p1.x, p1.y);
        this.overlayCtx.lineTo(p2.x, p2.y);
        this.overlayCtx.stroke();

        // Draw endpoints
        this.overlayCtx.fillStyle = '#FF0000';
        this.overlayCtx.beginPath();
        this.overlayCtx.arc(p1.x, p1.y, 4, 0, 2 * Math.PI);
        this.overlayCtx.fill();
        this.overlayCtx.beginPath();
        this.overlayCtx.arc(p2.x, p2.y, 4, 0, 2 * Math.PI);
        this.overlayCtx.fill();

        // Draw label
        const midX = (p1.x + p2.x) / 2;
        const midY = (p1.y + p2.y) / 2;
        this.overlayCtx.fillStyle = '#FFFFFF';
        this.overlayCtx.fillRect(midX - 30, midY - 12, 60, 24);
        this.overlayCtx.fillStyle = '#000000';
        this.overlayCtx.font = '12px Arial';
        this.overlayCtx.textAlign = 'center';
        this.overlayCtx.fillText(value.toFixed(2) + ' m', midX, midY + 4);
    },

    drawPolygon: function(points, closed) {
        if (points.length < 2) return;

        this.overlayCtx.strokeStyle = '#0000FF';
        this.overlayCtx.lineWidth = 2;
        this.overlayCtx.fillStyle = 'rgba(0, 0, 255, 0.2)';

        this.overlayCtx.beginPath();
        this.overlayCtx.moveTo(points[0].x, points[0].y);

        for (let i = 1; i < points.length; i++) {
            this.overlayCtx.lineTo(points[i].x, points[i].y);
        }

        if (closed) {
            this.overlayCtx.closePath();
            this.overlayCtx.fill();
        }

        this.overlayCtx.stroke();

        // Draw vertices
        this.overlayCtx.fillStyle = '#0000FF';
        for (const point of points) {
            this.overlayCtx.beginPath();
            this.overlayCtx.arc(point.x, point.y, 4, 0, 2 * Math.PI);
            this.overlayCtx.fill();
        }
    },

    drawCountMarker: function(x, y, number) {
        // Draw circle
        this.overlayCtx.fillStyle = '#00FF00';
        this.overlayCtx.strokeStyle = '#00AA00';
        this.overlayCtx.lineWidth = 2;
        this.overlayCtx.beginPath();
        this.overlayCtx.arc(x, y, 15, 0, 2 * Math.PI);
        this.overlayCtx.fill();
        this.overlayCtx.stroke();

        // Draw number
        this.overlayCtx.fillStyle = '#000000';
        this.overlayCtx.font = 'bold 14px Arial';
        this.overlayCtx.textAlign = 'center';
        this.overlayCtx.textBaseline = 'middle';
        this.overlayCtx.fillText(number.toString(), x, y);
    },

    drawPreviewLine: function(x, y) {
        if (this.measurementPoints.length === 0) return;

        this.clearOverlay();
        this.redrawMeasurements();

        const lastPoint = this.measurementPoints[this.measurementPoints.length - 1];

        this.overlayCtx.strokeStyle = 'rgba(128, 128, 128, 0.5)';
        this.overlayCtx.lineWidth = 1;
        this.overlayCtx.setLineDash([5, 5]);
        this.overlayCtx.beginPath();
        this.overlayCtx.moveTo(lastPoint.x, lastPoint.y);
        this.overlayCtx.lineTo(x, y);
        this.overlayCtx.stroke();
        this.overlayCtx.setLineDash([]);
    },

    // Calibration
    setCalibration: function(knownDistance, measuredPixels) {
        this.pixelsPerUnit = measuredPixels / knownDistance;
        this.calibrationScale = knownDistance / measuredPixels;
    },

    startCalibration: function() {
        this.setMeasurementMode('calibration');
        this.measurementPoints = [];
    },

    finishCalibration: function(knownDistance) {
        if (this.measurementPoints.length !== 2) return false;

        const p1 = this.measurementPoints[0];
        const p2 = this.measurementPoints[1];
        const measuredPixels = Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));

        this.setCalibration(knownDistance, measuredPixels);
        this.setMeasurementMode(null);

        return true;
    },

    // Get canvas bounding rect for positioning
    getBoundingClientRect: function() {
        if (!this.canvas) return null;
        const rect = this.canvas.getBoundingClientRect();
        return {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
    },

    // Clear all measurements
    clearMeasurements: function() {
        this.measurements = [];
        this.clearOverlay();
    },

    // Delete last measurement
    deleteLastMeasurement: function() {
        if (this.measurements.length > 0) {
            this.measurements.pop();
            this.redrawMeasurements();
        }
    }
};