// Simplified PDF Viewer for Upload Modal Preview
window.uploadPdfPreview = {
    pdfDoc: null,
    pageNum: 1,
    pageRendering: false,
    pageNumPending: null,
    scale: 1.2,
    canvas: null,
    ctx: null,
    overlayCanvas: null,
    overlayCtx: null,
    dotNetRef: null,
    currentPage: null,
    isLoading: false,  // Prevent concurrent load operations

    // Initialize the PDF preview viewer
    initialize: async function(canvasId, overlayCanvasId, dotNetReference) {
        console.log('[UploadPdfPreview] ========================================');
        console.log('[UploadPdfPreview] Initializing PDF preview viewer');
        console.log('[UploadPdfPreview] Canvas ID:', canvasId);
        console.log('[UploadPdfPreview] Overlay Canvas ID:', overlayCanvasId);
        console.log('[UploadPdfPreview] DotNetReference:', dotNetReference ? 'Provided' : 'NULL');
        console.log('[UploadPdfPreview] PDF.js available:', typeof pdfjsLib !== 'undefined');

        // Wait for canvas elements to be available (retry up to 30 times = 3 seconds)
        let attempts = 0;
        const maxAttempts = 30;

        while (attempts < maxAttempts) {
            this.canvas = document.getElementById(canvasId);
            this.overlayCanvas = document.getElementById(overlayCanvasId);

            if (this.canvas && this.overlayCanvas) {
                console.log('[UploadPdfPreview] Canvas elements found after', attempts, 'attempts');
                break;
            }

            console.log('[UploadPdfPreview] Waiting for canvas elements... attempt', attempts + 1);
            await new Promise(resolve => setTimeout(resolve, 100));
            attempts++;
        }

        console.log('[UploadPdfPreview] Canvas element found:', this.canvas ? 'YES' : 'NO');
        console.log('[UploadPdfPreview] Overlay canvas element found:', this.overlayCanvas ? 'YES' : 'NO');

        if (!this.canvas) {
            console.error('[UploadPdfPreview] Canvas element not found after', maxAttempts, 'attempts:', canvasId);
            return false;
        }

        if (!this.overlayCanvas) {
            console.error('[UploadPdfPreview] Overlay canvas element not found after', maxAttempts, 'attempts:', overlayCanvasId);
            return false;
        }

        this.ctx = this.canvas.getContext('2d');
        this.overlayCtx = this.overlayCanvas.getContext('2d');
        this.dotNetRef = dotNetReference;

        console.log('[UploadPdfPreview] Canvas context:', this.ctx ? 'OK' : 'FAILED');
        console.log('[UploadPdfPreview] Overlay context:', this.overlayCtx ? 'OK' : 'FAILED');

        // Setup OCR interactivity (hover and click)
        this.setupOcrInteractivity();

        console.log('[UploadPdfPreview] Initialized successfully with OCR interactivity');
        console.log('[UploadPdfPreview] ========================================');
        return true;
    },

    // Load PDF from base64 data URL
    loadPdfFromDataUrl: async function(dataUrl) {
        // Prevent concurrent load operations
        if (this.isLoading) {
            console.log('[UploadPdfPreview] ⚠️ Load already in progress, skipping duplicate request');
            return false;
        }

        this.isLoading = true;

        try {
            console.log('[UploadPdfPreview] ========================================');
            console.log('[UploadPdfPreview] Loading PDF from data URL');
            console.log('[UploadPdfPreview] Data URL length:', dataUrl ? dataUrl.length : 'NULL');
            console.log('[UploadPdfPreview] Data URL prefix:', dataUrl ? dataUrl.substring(0, 50) : 'NULL');
            console.log('[UploadPdfPreview] Canvas available:', this.canvas ? 'YES' : 'NO');
            console.log('[UploadPdfPreview] Context available:', this.ctx ? 'YES' : 'NO');

            // Reset state
            this.pageNum = 1;
            this.scale = 1.2;

            console.log('[UploadPdfPreview] Starting PDF.js loading task...');

            // Load the PDF from base64 data URL
            const loadingTask = pdfjsLib.getDocument(dataUrl);
            this.pdfDoc = await loadingTask.promise;

            console.log('[UploadPdfPreview] ✓ PDF loaded successfully!');
            console.log('[UploadPdfPreview] Total pages:', this.pdfDoc.numPages);

            // Initial page render
            console.log('[UploadPdfPreview] Starting first page render...');
            await this.renderPage(this.pageNum);
            console.log('[UploadPdfPreview] ✓ First page rendered!');

            // Notify Blazor that PDF is loaded - Blazor will handle UI visibility via CSS classes
            if (this.dotNetRef) {
                console.log('[UploadPdfPreview] Notifying Blazor that PDF loaded...');
                await this.dotNetRef.invokeMethodAsync('OnPreviewPdfLoaded', this.pdfDoc.numPages);
                console.log('[UploadPdfPreview] ✓ Blazor notified!');
            }

            console.log('[UploadPdfPreview] ========================================');
            this.isLoading = false;
            return true;
        } catch (error) {
            console.error('[UploadPdfPreview] ❌ Error loading PDF:', error);
            console.error('[UploadPdfPreview] Error stack:', error.stack);

            // Notify Blazor of error
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnPreviewPdfLoadError', error.message);
            }

            this.isLoading = false;
            return false;
        }
    },

    // Show PDF viewer (called after PDF loads)
    showPdfViewer: function() {
        // Show canvas container
        const canvasContainer = this.canvas.parentElement;
        if (canvasContainer) {
            canvasContainer.style.visibility = 'visible';
        }

        // Show toolbar
        const toolbar = document.querySelector('.pdf-preview-toolbar');
        if (toolbar) {
            toolbar.style.display = 'flex';
        }

        // Hide loading spinner
        const spinner = document.querySelector('.pdf-preview-frame .text-center.py-5');
        if (spinner) {
            spinner.style.display = 'none';
        }

        console.log('[UploadPdfPreview] UI visibility updated');
    },

    // Refresh canvas references (called when Blazor recreates the canvas)
    refreshCanvasReferences: function() {
        const newCanvas = document.getElementById('upload-pdf-canvas');
        const newOverlayCanvas = document.getElementById('upload-ocr-overlay');

        if (!newCanvas || !newOverlayCanvas) {
            console.warn('[UploadPdfPreview] Cannot refresh - canvas elements not found');
            return false;
        }

        // If canvas hasn't changed, no need to refresh
        if (this.canvas === newCanvas && this.overlayCanvas === newOverlayCanvas) {
            return false;
        }

        console.log('[UploadPdfPreview] Canvas was recreated by Blazor - refreshing references');

        // Save old canvas content
        const oldCanvas = this.canvas;
        const oldWidth = oldCanvas?.width;
        const oldHeight = oldCanvas?.height;

        // Update references
        this.canvas = newCanvas;
        this.overlayCanvas = newOverlayCanvas;
        this.ctx = newCanvas.getContext('2d');
        this.overlayCtx = newOverlayCanvas.getContext('2d');

        // If old canvas had content, copy it to new canvas
        if (oldCanvas && oldWidth && oldHeight) {
            console.log('[UploadPdfPreview] Copying content from old canvas to new canvas');
            this.canvas.width = oldWidth;
            this.canvas.height = oldHeight;
            this.overlayCanvas.width = oldWidth;
            this.overlayCanvas.height = oldHeight;
            this.ctx.drawImage(oldCanvas, 0, 0);

            // Redraw OCR overlay if present
            if (this.ocrResults) {
                this.redrawOcrOverlay();
            }
        }

        console.log('[UploadPdfPreview] Canvas references refreshed successfully');
        return true;
    },

    // Render specific page
    renderPage: async function(num) {
        if (!this.pdfDoc) {
            console.warn('[UploadPdfPreview] No PDF document loaded');
            return;
        }

        // Check if canvas was recreated and refresh references
        this.refreshCanvasReferences();

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

            // Clear overlay (for future OCR bounding boxes)
            this.clearOverlay();

            console.log('[UploadPdfPreview] Page', num, 'rendered successfully');

            // If there's a pending page render, do it now
            if (this.pageNumPending !== null) {
                const pending = this.pageNumPending;
                this.pageNumPending = null;
                await this.renderPage(pending);
            }
        } catch (error) {
            console.error('[UploadPdfPreview] Error rendering page:', error);
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
        this.scale = this.scale * 1.25;
        this.queueRenderPage(this.pageNum);
    },

    zoomOut: function() {
        if (this.scale <= 0.5) return; // Minimum scale
        this.scale = this.scale / 1.25;
        this.queueRenderPage(this.pageNum);
    },

    fitToWidth: function() {
        if (!this.currentPage) return;

        const container = this.canvas.parentElement;
        const pageViewport = this.currentPage.getViewport({ scale: 1 });

        // Account for padding
        const availableWidth = container.clientWidth - 40;
        this.scale = availableWidth / pageViewport.width;
        this.queueRenderPage(this.pageNum);
    },

    fitToHeight: function() {
        if (!this.currentPage) return;

        const container = this.canvas.parentElement;
        const pageViewport = this.currentPage.getViewport({ scale: 1 });

        // Account for toolbar and padding
        const availableHeight = container.clientHeight - 80;
        this.scale = availableHeight / pageViewport.height;
        this.queueRenderPage(this.pageNum);
    },

    resetZoom: function() {
        this.scale = 1.2;
        this.queueRenderPage(this.pageNum);
    },

    // Get current page info
    getCurrentPageInfo: function() {
        if (!this.pdfDoc) return null;

        return {
            currentPage: this.pageNum,
            totalPages: this.pdfDoc.numPages,
            scale: this.scale
        };
    },

    // Overlay functions (for future OCR bounding boxes)
    clearOverlay: function() {
        if (this.overlayCtx) {
            this.overlayCtx.clearRect(0, 0, this.overlayCanvas.width, this.overlayCanvas.height);
        }
    },

    // OCR Visualization State
    ocrResults: null,
    hoveredField: null,

    // Draw bounding box
    drawBoundingBox: function(x, y, width, height, label, color = '#FF0000', confidence = null) {
        if (!this.overlayCtx) return;

        // Draw rectangle with semi-transparent fill
        this.overlayCtx.strokeStyle = color;
        this.overlayCtx.lineWidth = 2;
        this.overlayCtx.strokeRect(x, y, width, height);

        // Fill with semi-transparent color
        this.overlayCtx.fillStyle = color + '20'; // Add alpha for transparency
        this.overlayCtx.fillRect(x, y, width, height);

        // Draw label background
        const labelText = confidence ? `${label} (${(confidence * 100).toFixed(0)}%)` : label;
        const labelWidth = this.overlayCtx.measureText(labelText).width + 8;

        this.overlayCtx.fillStyle = color;
        this.overlayCtx.fillRect(x, y - 22, labelWidth, 20);

        // Draw label text
        this.overlayCtx.fillStyle = '#FFFFFF';
        this.overlayCtx.font = 'bold 11px Arial';
        this.overlayCtx.fillText(labelText, x + 4, y - 8);
    },

    // Get color for field type
    getFieldColor: function(fieldName) {
        const colorMap = {
            'Drawing Number': '#FF6B6B',      // Red
            'Drawing Title': '#4ECDC4',       // Cyan
            'Project Name': '#45B7D1',        // Blue
            'Client': '#96CEB4',              // Green
            'Scale': '#FFEAA7',               // Yellow
            'Revision': '#DDA15E',            // Orange
            'Date': '#BC6C25'                 // Brown
        };
        return colorMap[fieldName] || '#6C757D'; // Default gray
    },

    // Display OCR results overlay
    displayOcrResults: function(ocrData) {
        console.log('[UploadPdfPreview] Displaying OCR results:', ocrData);

        this.ocrResults = ocrData;
        this.clearOverlay();

        if (!ocrData || !ocrData.extractedFields) {
            console.warn('[UploadPdfPreview] No OCR data to display');
            return;
        }

        // Draw titleblock bounds first (if available)
        if (ocrData.titleblockBounds) {
            const tb = ocrData.titleblockBounds;
            this.overlayCtx.strokeStyle = '#6C757D';
            this.overlayCtx.lineWidth = 1;
            this.overlayCtx.setLineDash([5, 5]);
            this.overlayCtx.strokeRect(tb.x, tb.y, tb.width, tb.height);
            this.overlayCtx.setLineDash([]);

            // Label
            this.overlayCtx.fillStyle = '#6C757D';
            this.overlayCtx.font = '10px Arial';
            this.overlayCtx.fillText('Titleblock Region', tb.x, tb.y - 5);
        }

        // Draw individual field bounding boxes
        ocrData.extractedFields.forEach(field => {
            if (field.bounds) {
                const color = this.getFieldColor(field.fieldName);
                this.drawBoundingBox(
                    field.bounds.x,
                    field.bounds.y,
                    field.bounds.width,
                    field.bounds.height,
                    field.fieldName,
                    color,
                    field.confidence
                );
            }
        });
    },

    // Clear OCR overlay
    clearOcrOverlay: function() {
        this.ocrResults = null;
        this.hoveredField = null;
        this.clearOverlay();
    },

    // Enable interactive OCR overlay (hover effects)
    setupOcrInteractivity: function() {
        if (!this.overlayCanvas) return;

        const self = this;

        // Mouse move for hover effects
        this.overlayCanvas.addEventListener('mousemove', function(e) {
            if (!self.ocrResults || !self.ocrResults.extractedFields) return;

            const rect = self.overlayCanvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            // Check if mouse is over any field
            let foundField = null;
            for (const field of self.ocrResults.extractedFields) {
                if (field.bounds) {
                    const b = field.bounds;
                    if (x >= b.x && x <= b.x + b.width &&
                        y >= b.y && y <= b.y + b.height) {
                        foundField = field;
                        break;
                    }
                }
            }

            // Update hover state and redraw if changed
            if (foundField !== self.hoveredField) {
                self.hoveredField = foundField;
                self.redrawOcrOverlay();

                // Update cursor
                self.overlayCanvas.style.cursor = foundField ? 'pointer' : 'default';
            }
        });

        // Click to show field details
        this.overlayCanvas.addEventListener('click', function(e) {
            if (self.hoveredField && self.dotNetRef) {
                self.dotNetRef.invokeMethodAsync('OnOcrFieldClicked', {
                    fieldName: self.hoveredField.fieldName,
                    value: self.hoveredField.value,
                    confidence: self.hoveredField.confidence
                });
            }
        });
    },

    // Redraw OCR overlay with hover effects
    redrawOcrOverlay: function() {
        if (!this.ocrResults) return;

        this.clearOverlay();

        // Draw titleblock bounds
        if (this.ocrResults.titleblockBounds) {
            const tb = this.ocrResults.titleblockBounds;
            this.overlayCtx.strokeStyle = '#6C757D';
            this.overlayCtx.lineWidth = 1;
            this.overlayCtx.setLineDash([5, 5]);
            this.overlayCtx.strokeRect(tb.x, tb.y, tb.width, tb.height);
            this.overlayCtx.setLineDash([]);

            this.overlayCtx.fillStyle = '#6C757D';
            this.overlayCtx.font = '10px Arial';
            this.overlayCtx.fillText('Titleblock Region', tb.x, tb.y - 5);
        }

        // Draw field bounding boxes with hover effect
        this.ocrResults.extractedFields.forEach(field => {
            if (field.bounds) {
                const isHovered = field === this.hoveredField;
                const color = this.getFieldColor(field.fieldName);

                // Enhance hovered field
                if (isHovered) {
                    this.overlayCtx.strokeStyle = color;
                    this.overlayCtx.lineWidth = 3;
                    this.overlayCtx.strokeRect(
                        field.bounds.x - 2,
                        field.bounds.y - 2,
                        field.bounds.width + 4,
                        field.bounds.height + 4
                    );
                }

                this.drawBoundingBox(
                    field.bounds.x,
                    field.bounds.y,
                    field.bounds.width,
                    field.bounds.height,
                    field.fieldName,
                    color,
                    field.confidence
                );

                // Show value tooltip on hover
                if (isHovered) {
                    const tooltipText = `Value: ${field.value}`;
                    const tooltipWidth = this.overlayCtx.measureText(tooltipText).width + 16;
                    const tooltipX = field.bounds.x;
                    const tooltipY = field.bounds.y + field.bounds.height + 5;

                    // Tooltip background
                    this.overlayCtx.fillStyle = 'rgba(0, 0, 0, 0.8)';
                    this.overlayCtx.fillRect(tooltipX, tooltipY, tooltipWidth, 24);

                    // Tooltip text
                    this.overlayCtx.fillStyle = '#FFFFFF';
                    this.overlayCtx.font = '12px Arial';
                    this.overlayCtx.fillText(tooltipText, tooltipX + 8, tooltipY + 16);
                }
            }
        });
    },

    // Clear all state
    reset: function() {
        this.pdfDoc = null;
        this.pageNum = 1;
        this.scale = 1.2;
        this.currentPage = null;
        this.clearOverlay();

        if (this.ctx) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        }

        console.log('[UploadPdfPreview] Reset complete');
    }
};
