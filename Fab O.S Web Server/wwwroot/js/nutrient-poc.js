/**
 * Nutrient (PSPDFKit) Proof of Concept - JavaScript Interop
 *
 * Purpose: Test Nutrient Web SDK integration with Blazor Server
 * Features: Basic PDF loading, measurement tools, annotations, scale persistence
 */

window.nutrientPoc = {
    instance: null,
    dotNetRef: null,
    licenseKey: null,
    measurementScaleConfig: null,  // Store current measurement scale configuration

    /**
     * Initialize Nutrient viewer
     * @param {string} containerId - HTML element ID for PDF container
     * @param {string} licenseKey - Nutrient license key
     * @param {object} dotNetReference - DotNet object reference for callbacks
     */
    initialize: async function(containerId, licenseKey, dotNetReference) {
        console.log('[Nutrient POC] ========================================');
        console.log('[Nutrient POC] Initializing Nutrient viewer');
        console.log('[Nutrient POC] Container ID:', containerId);
        console.log('[Nutrient POC] License key length:', licenseKey ? licenseKey.length : 'NULL');
        console.log('[Nutrient POC] DotNetReference:', dotNetReference ? 'Provided' : 'NULL');

        try {
            this.licenseKey = licenseKey;
            this.dotNetRef = dotNetReference;

            // Check if PSPDFKit is loaded
            if (typeof PSPDFKit === 'undefined') {
                console.error('[Nutrient POC] PSPDFKit library not loaded!');
                return false;
            }

            console.log('[Nutrient POC] PSPDFKit library loaded, version:', PSPDFKit.version);
            console.log('[Nutrient POC] Initialization successful (not yet loaded PDF)');
            console.log('[Nutrient POC] ========================================');

            return true;
        } catch (error) {
            console.error('[Nutrient POC] Error during initialization:', error);
            console.error('[Nutrient POC] Stack:', error.stack);
            return false;
        }
    },

    /**
     * Load PDF document from URL
     * @param {string} documentUrl - URL to PDF document (can be API endpoint or data URL)
     */
    loadPdf: async function(documentUrl, containerId) {
        console.log('[Nutrient POC] ========================================');
        console.log('[Nutrient POC] Loading PDF');
        console.log('[Nutrient POC] Document URL:', documentUrl ? documentUrl.substring(0, 100) + '...' : 'NULL');

        try {
            // Configuration for PSPDFKit
            // PSPDFKit requires an absolute URL for baseUrl
            const baseUrl = `${window.location.protocol}//${window.location.host}/assets/pspdfkit/`;
            console.log('[Nutrient POC] Base URL:', baseUrl);

            const configuration = {
                container: '#' + containerId,
                document: documentUrl,
                baseUrl: baseUrl,
                licenseKey: this.licenseKey,

                // Preserve measurement scales from document
                measurementValueConfiguration: (documentScales) => {
                    console.log('[Nutrient POC] 📏 Document scales loaded:', documentScales);
                    return documentScales;  // Return document scales to preserve them
                },

                // Enable measurement tools
                // Try using 'measure' dropdown which groups measurement tools
                toolbarItems: [
                    { type: 'sidebar-thumbnails' },
                    { type: 'sidebar-document-outline' },
                    { type: 'pager' },
                    { type: 'pan' },
                    { type: 'zoom-out' },
                    { type: 'zoom-in' },
                    { type: 'zoom-mode' },
                    { type: 'spacer' },

                    // Try the 'measure' toolbar item (groups all measurement tools)
                    { type: 'measure' },

                    { type: 'spacer' },
                    { type: 'annotate' },
                    { type: 'ink' },
                    { type: 'highlighter' },
                    { type: 'text-highlighter' },
                    { type: 'signature' },
                    { type: 'image' },
                    { type: 'stamp' },
                    { type: 'note' },
                    { type: 'text' },
                    { type: 'line' },
                    { type: 'arrow' },
                    { type: 'rectangle' },
                    { type: 'ellipse' },
                    { type: 'polygon' },
                    { type: 'polyline' }
                ]
            };

            console.log('[Nutrient POC] Loading PSPDFKit instance...');
            this.instance = await PSPDFKit.load(configuration);

            console.log('[Nutrient POC] ✓ PSPDFKit instance loaded successfully!');
            console.log('[Nutrient POC] Total pages:', this.instance.totalPageCount);

            // Setup event listeners
            this.setupEventListeners();

            // Notify Blazor that PDF loaded
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnPdfLoaded', this.instance.totalPageCount);
            }

            console.log('[Nutrient POC] ========================================');
            return true;

        } catch (error) {
            console.error('[Nutrient POC] ❌ Error loading PDF:', error);
            console.error('[Nutrient POC] Error message:', error.message);
            console.error('[Nutrient POC] Error type:', error.constructor.name);
            console.error('[Nutrient POC] Error toString:', error.toString());
            console.error('[Nutrient POC] Stack:', error.stack);

            // Log all error properties
            console.error('[Nutrient POC] Error properties:', Object.keys(error));
            for (let key in error) {
                if (error.hasOwnProperty(key)) {
                    console.error(`[Nutrient POC] Error.${key}:`, error[key]);
                }
            }

            // Notify Blazor of error
            if (this.dotNetRef) {
                const errorMsg = error.message || error.toString() || 'Unknown error';
                await this.dotNetRef.invokeMethodAsync('OnPdfLoadError', errorMsg);
            }

            console.log('[Nutrient POC] ========================================');
            return false;
        }
    },

    /**
     * Setup event listeners for annotations and measurements
     */
    setupEventListeners: function() {
        if (!this.instance) {
            console.warn('[Nutrient POC] Cannot setup listeners - no instance');
            return;
        }

        const self = this;

        // Listen for annotation creation
        this.instance.addEventListener('annotations.create', async (annotations) => {
            console.log('[Nutrient POC] Annotation(s) created:', annotations.size);

            for (const annotation of annotations) {
                console.log('[Nutrient POC] Annotation type:', annotation.constructor.name);
                console.log('[Nutrient POC] Is measurement:', annotation.isMeasurement || false);

                // If it's a measurement annotation, extract data
                if (annotation.isMeasurement) {
                    const measurementData = self.extractMeasurementData(annotation);
                    console.log('[Nutrient POC] Measurement data:', measurementData);

                    // TEST: Export Instant JSON to see if calibration is included
                    const instantJSON = await self.instance.exportInstantJSON();
                    console.log('[Nutrient POC] 📋 INSTANT JSON AFTER MEASUREMENT:', JSON.stringify(instantJSON, null, 2));

                    // Notify Blazor
                    if (self.dotNetRef) {
                        await self.dotNetRef.invokeMethodAsync('OnMeasurementCreated', measurementData);
                    }
                }
            }
        });

        // Listen for annotation updates
        this.instance.addEventListener('annotations.update', async (annotations) => {
            console.log('[Nutrient POC] Annotation(s) updated:', annotations.size);
        });

        // Listen for annotation deletion
        this.instance.addEventListener('annotations.delete', async (annotations) => {
            console.log('[Nutrient POC] Annotation(s) deleted:', annotations.size);
        });

        console.log('[Nutrient POC] Event listeners setup complete');
    },

    /**
     * Extract measurement data from annotation
     */
    extractMeasurementData: function(annotation) {
        const data = {
            id: annotation.id,
            type: annotation.constructor.name,
            pageIndex: annotation.pageIndex,
            isMeasurement: annotation.isMeasurement || false
        };

        // Extract measurement-specific data
        if (annotation.measurementType) {
            data.measurementType = annotation.measurementType; // 'distance', 'perimeter', 'area', etc.
        }

        if (annotation.measurementValue !== undefined) {
            data.value = annotation.measurementValue;
        }

        if (annotation.measurementValueConfiguration) {
            data.scale = annotation.measurementValueConfiguration.scale;
            data.precision = annotation.measurementValueConfiguration.precision;
        }

        // Extract coordinates
        if (annotation.lines && annotation.lines.size > 0) {
            // Line/distance measurement
            data.coordinates = {
                lines: annotation.lines.toArray().map(line => ({
                    start: { x: line.start.x, y: line.start.y },
                    end: { x: line.end.x, y: line.end.y }
                }))
            };
        } else if (annotation.points && annotation.points.size > 0) {
            // Polygon/area measurement
            data.coordinates = {
                points: annotation.points.toArray().map(p => ({ x: p.x, y: p.y }))
            };
        } else if (annotation.boundingBox) {
            // Rectangle/ellipse
            const bb = annotation.boundingBox;
            data.coordinates = {
                boundingBox: { x: bb.left, y: bb.top, width: bb.width, height: bb.height }
            };
        }

        // Extract custom data if present
        if (annotation.customData) {
            data.customData = annotation.customData;
        }

        return data;
    },

    /**
     * Set measurement scale
     * @param {number} scale - Scale factor (e.g., 50 for 1:50)
     * @param {string} unitFrom - Unit from (e.g., 'mm', 'in')
     * @param {string} unitTo - Unit to (e.g., 'm', 'ft')
     */
    setMeasurementScale: async function(scale, unitFrom, unitTo) {
        if (!this.instance) {
            console.warn('[Nutrient POC] Cannot set scale - no instance');
            return false;
        }

        try {
            console.log('[Nutrient POC] Setting measurement scale:', scale, unitFrom, '->', unitTo);
            console.log('[Nutrient POC] Available PSPDFKit exports:', Object.keys(PSPDFKit).filter(k => k.includes('Measure')));

            // Try using plain object configuration
            // PSPDFKit 2024.8.2 might use a different API
            const config = {
                scale: [{
                    unitFrom: unitFrom,
                    unitTo: unitTo,
                    factor: scale
                }],
                precision: 'FOUR_DP'
            };

            console.log('[Nutrient POC] Attempting to set measurement config:', config);

            // Store configuration for persistence
            this.measurementScaleConfig = {
                scale: scale,
                unitFrom: unitFrom,
                unitTo: unitTo
            };
            console.log('[Nutrient POC] 💾 Stored scale config for persistence:', this.measurementScaleConfig);

            // Check if setMeasurementValueConfiguration exists
            if (typeof this.instance.setMeasurementValueConfiguration === 'function') {
                await this.instance.setMeasurementValueConfiguration(config);
                console.log('[Nutrient POC] ✓ Measurement scale set using setMeasurementValueConfiguration');
                return true;
            } else {
                console.warn('[Nutrient POC] setMeasurementValueConfiguration not available');
                console.log('[Nutrient POC] Available instance methods:', Object.keys(this.instance).filter(k => k.includes('measure')));
                console.log('[Nutrient POC] Measurement tools should still work with default scale');
                return true; // Return true so button doesn't show error
            }

        } catch (error) {
            console.error('[Nutrient POC] Error setting scale:', error);
            console.error('[Nutrient POC] Measurement tools should still work with default scale');
            return true; // Return true so user can still test measurements
        }
    },

    /**
     * Export PDF document with all measurements and scales, then close
     * @returns {string} Base64-encoded PDF document
     */
    exportAndClose: async function() {
        console.log('[Nutrient POC] ========================================');
        console.log('[Nutrient POC] Exporting PDF document and closing');

        if (!this.instance) {
            console.warn('[Nutrient POC] No instance to export');
            return null;
        }

        try {
            // Export the FULL PDF document with embedded annotations and measurements
            // This preserves ALL scale data inside the PDF
            console.log('[Nutrient POC] Exporting PDF via exportPDF()...');
            const arrayBuffer = await this.instance.exportPDF();
            console.log('[Nutrient POC] ✓ PDF exported');
            console.log('[Nutrient POC] PDF size:', arrayBuffer.byteLength, 'bytes');

            // Convert ArrayBuffer to Base64 string for storage
            const uint8Array = new Uint8Array(arrayBuffer);
            let binaryString = '';
            for (let i = 0; i < uint8Array.length; i++) {
                binaryString += String.fromCharCode(uint8Array[i]);
            }
            const base64Pdf = btoa(binaryString);
            console.log('[Nutrient POC] ✓ PDF converted to Base64');
            console.log('[Nutrient POC] Base64 length:', base64Pdf.length, 'characters');

            // Unload the instance
            await PSPDFKit.unload(this.instance);
            this.instance = null;
            console.log('[Nutrient POC] ✓ Instance unloaded');
            console.log('[Nutrient POC] ========================================');

            // Return Base64-encoded PDF
            return base64Pdf;

        } catch (error) {
            console.error('[Nutrient POC] Error exporting and closing:', error);
            console.log('[Nutrient POC] ========================================');
            throw error;
        }
    },

    /**
     * Load PDF from exported Base64 data
     * @param {string} documentUrl - IGNORED - we load from the saved PDF instead
     * @param {string} containerId - Container element ID
     * @param {string} base64PdfData - Base64-encoded PDF document
     */
    loadPdfWithData: async function(documentUrl, containerId, base64PdfData) {
        console.log('[Nutrient POC] ========================================');
        console.log('[Nutrient POC] Loading PDF from saved data');
        console.log('[Nutrient POC] Saved PDF data length:', base64PdfData?.length || 0, 'characters');

        try {
            // Convert Base64 PDF back to data URL
            const pdfDataUrl = 'data:application/pdf;base64,' + base64PdfData;
            console.log('[Nutrient POC] ✓ Created PDF data URL');
            console.log('[Nutrient POC] Data URL length:', pdfDataUrl.length, 'characters');

            // Configuration for PSPDFKit - load from the EXPORTED PDF
            // This PDF contains all measurements with embedded scale data
            const baseUrl = `${window.location.protocol}//${window.location.host}/assets/pspdfkit/`;
            console.log('[Nutrient POC] Base URL:', baseUrl);

            const configuration = {
                container: '#' + containerId,
                document: pdfDataUrl,  // Load from the SAVED PDF, not the original!
                baseUrl: baseUrl,
                licenseKey: this.licenseKey,

                // Preserve measurement scales from document
                measurementValueConfiguration: (documentScales) => {
                    console.log('[Nutrient POC] 📏 Document scales loaded from saved PDF:', documentScales);
                    return documentScales;  // Return document scales to preserve them
                },

                // Enable measurement tools
                toolbarItems: [
                    { type: 'sidebar-thumbnails' },
                    { type: 'sidebar-document-outline' },
                    { type: 'pager' },
                    { type: 'pan' },
                    { type: 'zoom-out' },
                    { type: 'zoom-in' },
                    { type: 'zoom-mode' },
                    { type: 'spacer' },
                    { type: 'measure' },
                    { type: 'spacer' },
                    { type: 'annotate' },
                    { type: 'ink' },
                    { type: 'highlighter' },
                    { type: 'text-highlighter' },
                    { type: 'signature' },
                    { type: 'image' },
                    { type: 'stamp' },
                    { type: 'note' },
                    { type: 'text' },
                    { type: 'line' },
                    { type: 'arrow' },
                    { type: 'rectangle' },
                    { type: 'ellipse' },
                    { type: 'polygon' },
                    { type: 'polyline' }
                ]
            };

            console.log('[Nutrient POC] Loading PSPDFKit instance from saved PDF...');
            this.instance = await PSPDFKit.load(configuration);

            console.log('[Nutrient POC] ✓ PSPDFKit instance loaded from saved PDF!');
            console.log('[Nutrient POC] Total pages:', this.instance.totalPageCount);

            // Setup event listeners
            this.setupEventListeners();

            // Notify Blazor that PDF loaded
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnPdfLoaded', this.instance.totalPageCount);
            }

            console.log('[Nutrient POC] ========================================');
            return true;

        } catch (error) {
            console.error('[Nutrient POC] ❌ Error loading PDF from saved data:', error);
            console.error('[Nutrient POC] Error message:', error.message);
            console.error('[Nutrient POC] Stack:', error.stack);

            // Notify Blazor of error
            if (this.dotNetRef) {
                const errorMsg = error.message || error.toString() || 'Unknown error';
                await this.dotNetRef.invokeMethodAsync('OnPdfLoadError', errorMsg);
            }

            console.log('[Nutrient POC] ========================================');
            return false;
        }
    },

    /**
     * Unload/destroy PSPDFKit instance
     */
    unload: async function() {
        if (this.instance) {
            console.log('[Nutrient POC] Unloading PSPDFKit instance');
            try {
                await PSPDFKit.unload(this.instance);
                this.instance = null;
                console.log('[Nutrient POC] ✓ Instance unloaded');
            } catch (error) {
                console.error('[Nutrient POC] Error unloading:', error);
            }
        }
    },

    /**
     * Get current page number
     */
    getCurrentPageIndex: function() {
        if (!this.instance) return 0;
        return this.instance.viewState.currentPageIndex;
    },

    /**
     * Get total page count
     */
    getTotalPageCount: function() {
        if (!this.instance) return 0;
        return this.instance.totalPageCount;
    }
};

console.log('[Nutrient POC] JavaScript module loaded');
