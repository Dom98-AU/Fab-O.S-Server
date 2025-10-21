/**
 * Nutrient (PSPDFKit) PDF Viewer - Production Module
 *
 * Purpose: Production-ready Nutrient Web SDK integration for takeoff/measurement viewing
 * Features: PDF loading, measurement tools, annotations, scale calibration
 */

window.nutrientViewer = {
    instances: {}, // Store multiple instances by containerId
    licenseKey: null,
    // Development license: Webviewer + Annotation + Comments & Replies + Content Editor + Document Editor + Measurement tools
    defaultLicenseKey: "0D8AjKw9jGNdbWXn4JuDDwXVzN6ZSL76NEVWU-A4Y99C8w3THXzIxgncCIBrePSL8qVAFiwszAUotFXjPkfwAr2A1y5hq_Tj-poiQNtLJVhnUudMaeGTVuIG_fbRc5KHQpUFBKnlaBnpXfVlopPjmKa8oDdG_tYfvSIv7Cv802oIRjjEwXfSjw0w8vAFnSqhgMO65NT-df7pljLwhHCkGSgXXQte3vTzi4xzhWJxGBUSDko1iE37nLLwJoZtdLJRxIZAQOJR85LkCnZ9GZXJkZDGciMbam2H3w9wD_50wDB3oGQFSm8aXEZCKS-hsRNV_6dHjSHcAU3xpihrzrrcAzuRDY6ilO5xQvQP",

    // Color pool management for distinct catalogue item colors
    colorPool: [
        { r: 65, g: 105, b: 225 },      // Royal Blue
        { r: 255, g: 140, b: 0 },       // Dark Orange
        { r: 34, g: 139, b: 34 },       // Forest Green
        { r: 220, g: 20, b: 60 },       // Crimson
        { r: 138, g: 43, b: 226 },      // Blue Violet
        { r: 0, g: 139, b: 139 },       // Dark Cyan
        { r: 199, g: 21, b: 133 },      // Medium Violet Red
        { r: 70, g: 130, b: 180 },      // Steel Blue
        { r: 178, g: 34, b: 34 },       // Firebrick
        { r: 50, g: 205, b: 50 },       // Lime Green
        { r: 255, g: 165, b: 0 },       // Orange
        { r: 147, g: 112, b: 219 },     // Medium Purple
        { r: 0, g: 128, b: 0 },         // Green
        { r: 255, g: 99, b: 71 },       // Tomato
        { r: 75, g: 0, b: 130 },        // Indigo
        { r: 139, g: 0, b: 0 },         // Dark Red
        { r: 100, g: 149, b: 237 },     // Cornflower Blue
        { r: 255, g: 127, b: 80 },      // Coral
        { r: 0, g: 206, b: 209 },       // Dark Turquoise
        { r: 72, g: 61, b: 139 }        // Dark Slate Blue
    ],

    /**
     * Initialize Nutrient viewer
     * @param {string} containerId - HTML element ID for PDF container
     * @param {string} licenseKey - Nutrient license key (optional, uses default if not provided)
     * @param {object} dotNetReference - DotNet object reference for callbacks
     */
    initialize: async function(containerId, licenseKey, dotNetReference) {
        console.log(`[Nutrient Viewer] Initializing viewer in container: ${containerId}`);

        try {
            // Use provided license key or default
            this.licenseKey = licenseKey || this.defaultLicenseKey;

            // Check if PSPDFKit is loaded
            if (typeof PSPDFKit === 'undefined') {
                console.error('[Nutrient Viewer] PSPDFKit library not loaded!');
                return { success: false, error: 'PSPDFKit library not loaded' };
            }

            console.log(`[Nutrient Viewer] PSPDFKit library loaded, version: ${PSPDFKit.version}`);

            // Store the instance data
            if (!this.instances[containerId]) {
                this.instances[containerId] = {
                    instance: null,
                    dotNetRef: dotNetReference,
                    annotations: [],
                    currentScale: 50, // Default 1:50
                    selectedCatalogueItem: null, // For catalogue-aware measurements
                    colorPoolIndex: 0, // Track next available color from pool
                    usedColors: new Set() // Track which colors are currently in use
                };
            } else {
                this.instances[containerId].dotNetRef = dotNetReference;
            }

            console.log('[Nutrient Viewer] Initialization successful');
            return { success: true };

        } catch (error) {
            console.error('[Nutrient Viewer] Error during initialization:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Build PSPDFKit configuration object (shared between loadPdf and reloadInstantJson)
     * @param {string} containerId - Container element ID
     * @param {string} documentUrl - URL to PDF document
     * @param {object} instantJSON - Optional Instant JSON to load
     * @returns {object} PSPDFKit configuration object
     */
    buildConfiguration: function(containerId, documentUrl, instantJSON = null) {
        const baseUrl = `${window.location.protocol}//${window.location.host}/assets/pspdfkit/`;

        const configuration = {
                container: '#' + containerId,
                document: documentUrl,
                baseUrl: baseUrl,
                licenseKey: this.licenseKey,

                // Enable snapping for measurements
                measurementSnapping: true,
                snapToContent: true,           // Snap to PDF vector content (lines, shapes from CAD)
                snapToContentVertices: true,   // Snap to line endpoints
                snapToContentEdges: true,      // Snap to line midpoints
                snapDistance: 10,              // Pixels within which snapping activates

                // Hide annotation tooltips for measurement annotations
                annotationTooltipCallback: (annotation) => {
                    // Hide all tooltips - we don't want any tooltips showing during measurements
                    // This includes "Line Width", measurement values, etc.
                    return [];
                },

                // Toolbar configuration with measurement tools
                toolbarItems: [
                    { type: 'sidebar-thumbnails' },
                    { type: 'sidebar-document-outline' },
                    { type: 'pager' },
                    { type: 'pan' },
                    { type: 'zoom-out' },
                    { type: 'zoom-in' },
                    { type: 'zoom-mode' },
                    { type: 'spacer' },

                    // Measurement tools dropdown
                    { type: 'measure' },

                    // Custom line thickness selector (Custom Button Group)
                    {
                        type: 'custom',
                        id: 'line-thickness-selector',
                        title: 'Line Thickness',
                        node: (() => {
                            const container = document.createElement('div');
                            container.style.cssText = `
                                display: flex;
                                align-items: center;
                                gap: 6px;
                                padding: 0 8px;
                                height: 40px;
                                border-left: 1px solid #E0E0E0;
                                border-right: 1px solid #E0E0E0;
                                position: relative;
                            `;

                            const label = document.createElement('label');
                            label.textContent = 'Width:';
                            label.style.cssText = `
                                font-size: 12px;
                                color: #4A5568;
                                font-weight: 500;
                                white-space: nowrap;
                            `;

                            // Create custom dropdown button
                            const dropdownBtn = document.createElement('button');
                            dropdownBtn.textContent = '1pt';
                            dropdownBtn.style.cssText = `
                                padding: 4px 12px;
                                border: 1px solid #D1D5DB;
                                border-radius: 4px;
                                background: white;
                                font-size: 13px;
                                color: #1F2937;
                                cursor: pointer;
                                min-width: 90px;
                                height: 28px;
                                text-align: left;
                                position: relative;
                                z-index: 1;
                            `;

                            // Create dropdown menu (appended to body for proper positioning)
                            const dropdownMenu = document.createElement('div');
                            dropdownMenu.style.cssText = `
                                position: fixed;
                                background: white;
                                border: 1px solid #D1D5DB;
                                border-radius: 4px;
                                box-shadow: 0 4px 8px rgba(0,0,0,0.15);
                                display: none;
                                min-width: 140px;
                                z-index: 999999;
                            `;

                            const options = [
                                { value: '0.5', text: '0.5pt Thin' },
                                { value: '1', text: '1pt Default' },
                                { value: '2', text: '2pt Medium' },
                                { value: '3', text: '3pt Thick' },
                                { value: '5', text: '5pt Bold' }
                            ];

                            // Create menu options
                            options.forEach(opt => {
                                const menuItem = document.createElement('div');
                                menuItem.textContent = opt.text;
                                menuItem.dataset.value = opt.value;
                                menuItem.style.cssText = `
                                    padding: 8px 12px;
                                    cursor: pointer;
                                    font-size: 13px;
                                    color: #1F2937;
                                `;

                                menuItem.addEventListener('mouseenter', () => {
                                    menuItem.style.background = '#F3F4F6';
                                });

                                menuItem.addEventListener('mouseleave', () => {
                                    menuItem.style.background = 'white';
                                });

                                menuItem.addEventListener('click', async (e) => {
                                    e.stopPropagation();
                                    const newWidth = parseFloat(opt.value);
                                    dropdownBtn.textContent = opt.text.split(' ')[0]; // Show just "1pt", "2pt", etc.
                                    dropdownMenu.style.display = 'none';

                                    console.log(`[Nutrient Viewer] Setting stroke width to ${newWidth}pt for container ${containerId}`);

                                    try {
                                        // Look up the instance data
                                        const instanceData = nutrientViewer.instances[containerId];
                                        if (!instanceData || !instanceData.instance) {
                                            console.error('[Nutrient Viewer] Instance not found for container:', containerId);
                                            return;
                                        }

                                        // Update annotation presets dynamically
                                        // PSPDFKit uses both strokeWidth and lineWidth for different annotation types
                                        await instanceData.instance.setAnnotationPresets((presets) => {
                                            // Distance measurement (calibration line)
                                            presets.distanceMeasurement = {
                                                ...presets.distanceMeasurement,
                                                strokeWidth: newWidth,
                                                lineWidth: newWidth  // Some versions use lineWidth
                                            };
                                            // Perimeter measurement
                                            presets.perimeterMeasurement = {
                                                ...presets.perimeterMeasurement,
                                                strokeWidth: newWidth,
                                                lineWidth: newWidth
                                            };
                                            // Area measurements
                                            presets.rectangleAreaMeasurement = {
                                                ...presets.rectangleAreaMeasurement,
                                                strokeWidth: newWidth,
                                                lineWidth: newWidth
                                            };
                                            presets.ellipseAreaMeasurement = {
                                                ...presets.ellipseAreaMeasurement,
                                                strokeWidth: newWidth,
                                                lineWidth: newWidth
                                            };
                                            presets.polygonAreaMeasurement = {
                                                ...presets.polygonAreaMeasurement,
                                                strokeWidth: newWidth,
                                                lineWidth: newWidth
                                            };

                                            console.log('[Nutrient Viewer] Updated presets:', {
                                                distanceMeasurement: presets.distanceMeasurement,
                                                perimeterMeasurement: presets.perimeterMeasurement
                                            });

                                            return presets;
                                        });
                                        console.log(`[Nutrient Viewer] ✓ Stroke width updated to ${newWidth}pt`);
                                    } catch (error) {
                                        console.error('[Nutrient Viewer] Error updating stroke width:', error);
                                    }
                                });

                                dropdownMenu.appendChild(menuItem);
                            });

                            // Toggle dropdown on button click
                            dropdownBtn.addEventListener('click', (e) => {
                                e.stopPropagation();
                                console.log('[Nutrient Viewer] Line thickness dropdown clicked');

                                const isVisible = dropdownMenu.style.display === 'block';

                                if (isVisible) {
                                    dropdownMenu.style.display = 'none';
                                } else {
                                    // Position dropdown below the button using fixed positioning
                                    const rect = dropdownBtn.getBoundingClientRect();
                                    dropdownMenu.style.top = (rect.bottom + 4) + 'px';
                                    dropdownMenu.style.left = rect.left + 'px';
                                    dropdownMenu.style.display = 'block';
                                    console.log('[Nutrient Viewer] Dropdown positioned at:', { top: rect.bottom + 4, left: rect.left });
                                }
                            });

                            // Close dropdown when clicking outside
                            document.addEventListener('click', (e) => {
                                if (!container.contains(e.target) && !dropdownMenu.contains(e.target)) {
                                    dropdownMenu.style.display = 'none';
                                }
                            });

                            // Append dropdown to body to avoid clipping
                            document.body.appendChild(dropdownMenu);

                            container.appendChild(label);
                            container.appendChild(dropdownBtn);
                            return container;
                        })()
                    },

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
                    { type: 'polyline' },
                    { type: 'print' },
                    { type: 'export-pdf' }
                ],

                // Measurement value configuration callback
                // This callback receives scales stored in the document (from Instant JSON)
                // and allows us to add custom scales or modify existing ones
                measurementValueConfiguration: (documentScales) => {
                    console.log('[Nutrient Viewer] ============================================');
                    console.log('[Nutrient Viewer] 📏 Measurement scales loaded from document:');
                    console.log('[Nutrient Viewer] Document scales:', documentScales);
                    console.log('[Nutrient Viewer] Number of scales:', documentScales?.length || 0);

                    if (documentScales && documentScales.length > 0) {
                        documentScales.forEach((scaleObj, index) => {
                            console.log(`[Nutrient Viewer]   Scale ${index + 1}:`, {
                                name: scaleObj.name,
                                unitFrom: scaleObj.scale?.unitFrom,
                                unitTo: scaleObj.scale?.unitTo,
                                from: scaleObj.scale?.from,
                                to: scaleObj.scale?.to,
                                precision: scaleObj.precision,
                                selected: scaleObj.selected
                            });
                        });
                    } else {
                        console.log('[Nutrient Viewer] No document scales found (PDF has no saved calibration)');
                    }

                    console.log('[Nutrient Viewer] ✅ Returning document scales (Nutrient handles persistence automatically)');
                    console.log('[Nutrient Viewer] ============================================');

                    // Return the document scales as-is to preserve calibration
                    // Nutrient automatically saves these with the document via Instant JSON
                    return documentScales || [];
                },

                // Annotation presets with thinner strokeWidth for measurement tools
                // Include both strokeWidth and lineWidth for compatibility
                annotationPresets: {
                    distanceMeasurement: {
                        strokeWidth: 1,
                        lineWidth: 1,
                        strokeColor: new PSPDFKit.Color({ r: 255, g: 0, b: 0 })
                    },
                    perimeterMeasurement: {
                        strokeWidth: 1,
                        lineWidth: 1,
                        strokeColor: new PSPDFKit.Color({ r: 255, g: 0, b: 0 })
                    },
                    rectangleAreaMeasurement: {
                        strokeWidth: 1,
                        lineWidth: 1,
                        strokeColor: new PSPDFKit.Color({ r: 255, g: 165, b: 0 })
                    },
                    ellipseAreaMeasurement: {
                        strokeWidth: 1,
                        lineWidth: 1,
                        strokeColor: new PSPDFKit.Color({ r: 255, g: 165, b: 0 })
                    },
                    polygonAreaMeasurement: {
                        strokeWidth: 1,
                        lineWidth: 1,
                        strokeColor: new PSPDFKit.Color({ r: 255, g: 165, b: 0 })
                    }
                },

                // Hide all annotation tooltips - no tooltips needed for measurements
                annotationTooltipCallback: (annotation) => {
                    // Return empty array to hide all tooltips
                    return [];
                },

                // Add Instant JSON if provided (for reload scenarios)
                ...(instantJSON && { instantJSON: instantJSON })
            };

            return configuration;
        },

    /**
     * Load PDF document from URL
     * @param {string} containerId - Container element ID
     * @param {string} documentUrl - URL to PDF document (can be API endpoint)
     */
    loadPdf: async function(containerId, documentUrl) {
        console.log(`[Nutrient Viewer] Loading PDF in ${containerId} from: ${documentUrl}`);

        try {
            const instanceData = this.instances[containerId];
            if (!instanceData) {
                throw new Error('Viewer not initialized. Call initialize() first.');
            }

            // Unload existing instance if any
            if (instanceData.instance) {
                await PSPDFKit.unload(instanceData.instance);
                instanceData.instance = null;
            }

            // Build configuration using shared function
            const configuration = this.buildConfiguration(containerId, documentUrl);

            console.log('[Nutrient Viewer] Loading PSPDFKit instance...');
            instanceData.instance = await PSPDFKit.load(configuration);

            // Store the document URL for later use (e.g., extracting package drawing ID)
            instanceData.documentUrl = documentUrl;
            console.log('[Nutrient Viewer] Stored document URL:', documentUrl);

            console.log(`[Nutrient Viewer] ✓ PSPDFKit instance loaded successfully! (${instanceData.instance.totalPageCount} pages)`);

            // Setup event listeners
            this.setupEventListeners(containerId);

            // Notify Blazor that PDF loaded
            if (instanceData.dotNetRef) {
                await instanceData.dotNetRef.invokeMethodAsync('OnPdfLoaded', instanceData.instance.totalPageCount);
            }

            return { success: true, pageCount: instanceData.instance.totalPageCount };

        } catch (error) {
            console.error('[Nutrient Viewer] ❌ Error loading PDF:', error);
            console.error('[Nutrient Viewer] Error details:', error.message, error.stack);

            // Notify Blazor of error
            const instanceData = this.instances[containerId];
            if (instanceData?.dotNetRef) {
                const errorMsg = error.message || error.toString() || 'Unknown error';
                await instanceData.dotNetRef.invokeMethodAsync('OnPdfLoadError', errorMsg);
            }

            return { success: false, error: error.message };
        }
    },

    /**
     * Setup event listeners for annotations and measurements
     */
    setupEventListeners: function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot setup listeners - no instance');
            return;
        }

        const instance = instanceData.instance;
        const self = this;

        // Debounced autosave function - saves Instant JSON after 2 seconds of inactivity
        let autosaveTimer = null;
        let lastAutosaveTimestamp = 0; // Track when this tab last triggered autosave
        let autosaveEnabled = true; // Flag to disable autosave during reload operations
        instanceData.lastAutosaveTimestamp = 0; // Make it accessible from reloadInstantJson
        instanceData.autosaveEnabled = true; // Make it accessible from reloadInstantJson

        const triggerAutosave = () => {
            // Skip autosave if disabled (during reload operations)
            if (!autosaveEnabled || !instanceData.autosaveEnabled) {
                console.log('[Nutrient Viewer] ⏭️ Skipping autosave - currently disabled');
                return;
            }

            if (autosaveTimer) {
                clearTimeout(autosaveTimer);
            }

            autosaveTimer = setTimeout(async () => {
                console.log('[Nutrient Viewer] 💾 Autosave triggered...');
                try {
                    // Export Instant JSON
                    const instantJSON = await instance.exportInstantJSON();
                    const instantJSONString = JSON.stringify(instantJSON);

                    // DEBUG: Log what we're about to save
                    console.log('[Nutrient Viewer] 🔍 DEBUG: Exported Instant JSON length:', instantJSONString.length);
                    console.log('[Nutrient Viewer] 🔍 DEBUG: Exported Instant JSON preview (first 500 chars):', instantJSONString.substring(0, 500));
                    console.log('[Nutrient Viewer] 🔍 DEBUG: Number of operations:', instantJSON?.operations?.length || 0);
                    console.log('[Nutrient Viewer] 🔍 DEBUG: Instant JSON object keys:', Object.keys(instantJSON || {}));

                    // Extract package drawing ID from the document URL
                    const urlMatch = instanceData.documentUrl?.match(/\/api\/packagedrawings\/(\d+)/);
                    const packageDrawingId = urlMatch ? parseInt(urlMatch[1]) : null;

                    if (packageDrawingId) {
                        // Mark this tab as having just autosaved (prevent reload loop)
                        lastAutosaveTimestamp = Date.now();
                        instanceData.lastAutosaveTimestamp = lastAutosaveTimestamp;

                        // Save to database via API
                        const response = await fetch(`/api/packagedrawings/${packageDrawingId}/instant-json`, {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({ instantJson: instantJSONString })
                        });

                        if (response.ok) {
                            console.log('[Nutrient Viewer] ✓ Autosave successful - Instant JSON saved to database');
                        } else {
                            console.error('[Nutrient Viewer] ✗ Autosave failed:', await response.text());
                        }
                    }
                } catch (error) {
                    console.error('[Nutrient Viewer] ✗ Autosave error:', error);
                }
            }, 2000); // 2 second debounce
        };

        // Listen for annotation creation
        instance.addEventListener('annotations.create', async (annotations) => {
            console.log('[Nutrient Viewer] ============================================');
            console.log(`[Nutrient Viewer] Annotation(s) created: ${annotations.size}`);

            for (const annotation of annotations) {
                console.log(`[Nutrient Viewer] Annotation type: ${annotation.constructor.name}`);
                console.log(`[Nutrient Viewer] Annotation ID: ${annotation.id}`);
                const isMeasurement = typeof annotation.isMeasurement === 'function' ? annotation.isMeasurement() : annotation.isMeasurement;
                console.log(`[Nutrient Viewer] Is measurement: ${isMeasurement}`);

                // Log the actual colors BEFORE we update them
                if (annotation.strokeColor) {
                    console.log(`[Nutrient Viewer] Original annotation stroke color: rgb(${Math.round(annotation.strokeColor.r)}, ${Math.round(annotation.strokeColor.g)}, ${Math.round(annotation.strokeColor.b)})`);
                } else {
                    console.log(`[Nutrient Viewer] ✗ No stroke color on annotation`);
                }

                // Check if this is a measurement annotation AND we have a selected catalogue item with assigned color
                if (isMeasurement && instanceData.selectedCatalogueItem?.assignedColor) {
                    const colorRGB = instanceData.selectedCatalogueItem.assignedColor;
                    const color = new PSPDFKit.Color(colorRGB);
                    console.log(`[Nutrient Viewer] 🎨 Applying catalogue color to annotation: rgb(${colorRGB.r}, ${colorRGB.g}, ${colorRGB.b})`);

                    try {
                        // Update annotation with the assigned color
                        const updatedAnnotation = annotation
                            .set('strokeColor', color)
                            .set('fillColor', color.set('a', 0.3)); // Semi-transparent fill

                        await instance.update(updatedAnnotation);
                        console.log(`[Nutrient Viewer] ✓ Updated annotation ${annotation.id} with catalogue color`);
                    } catch (error) {
                        console.error(`[Nutrient Viewer] ✗ Failed to update annotation color:`, error);
                    }
                }

                if (isMeasurement) {
                    console.log(`[Nutrient Viewer] Measurement type: ${annotation.measurementType}`);
                    console.log(`[Nutrient Viewer] Measurement value: ${annotation.measurementValue}`);
                    console.log(`[Nutrient Viewer] Measurement config:`, annotation.measurementValueConfiguration);
                    console.log(`[Nutrient Viewer] Measurement scale:`, annotation.measurementScale);
                    console.log(`[Nutrient Viewer] Annotation subject:`, annotation.subject);
                    console.log(`[Nutrient Viewer] Annotation contents:`, annotation.contents);
                    console.log(`[Nutrient Viewer] Annotation note:`, annotation.note);
                    console.log(`[Nutrient Viewer] Start point:`, annotation.startPoint);
                    console.log(`[Nutrient Viewer] End point:`, annotation.endPoint);
                }

                // Extract annotation data
                const annotationData = self.extractAnnotationData(annotation);
                console.log('[Nutrient Viewer] Extracted annotation data:', JSON.stringify(annotationData, null, 2));

                console.log(`[Nutrient Viewer] Catalogue item selected: ${instanceData.selectedCatalogueItem !== null}`);
                if (instanceData.selectedCatalogueItem) {
                    console.log(`[Nutrient Viewer] Selected catalogue item: ${instanceData.selectedCatalogueItem.itemCode} - ${instanceData.selectedCatalogueItem.description}`);
                }

                // IMPORTANT: Save annotation to database FIRST via HTTP API (more reliable than JS interop)
                console.log('[Nutrient Viewer] Saving annotation to database via HTTP API...');
                try {
                    // Extract package drawing ID from the document URL
                    // URL format: /api/packagedrawings/{id}/sharepoint-content
                    const urlMatch = instanceData.documentUrl?.match(/\/api\/packagedrawings\/(\d+)/);
                    const packageDrawingId = urlMatch ? parseInt(urlMatch[1]) : null;

                    if (packageDrawingId) {
                        // Create a simple JSON representation of the annotation
                        const instantJson = JSON.stringify({
                            id: annotation.id,
                            type: annotationData.type,
                            pageIndex: annotationData.pageIndex,
                            isMeasurement: annotationData.isMeasurement,
                            coordinates: annotationData.coordinates
                        });

                        const saveRequest = {
                            annotationId: annotation.id,
                            packageDrawingId: packageDrawingId,
                            annotationType: annotationData.type,
                            pageIndex: annotationData.pageIndex,
                            isMeasurement: annotationData.isMeasurement,
                            instantJson: instantJson
                        };

                        console.log('[Nutrient Viewer] Saving annotation:', saveRequest);

                        const response = await fetch('/api/takeoff/catalogue/annotations', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify(saveRequest)
                        });

                        if (response.ok) {
                            console.log('[Nutrient Viewer] ✓ Annotation saved to database successfully');
                        } else {
                            const errorText = await response.text();
                            console.error('[Nutrient Viewer] ✗ Failed to save annotation:', errorText);
                        }
                    } else {
                        console.error('[Nutrient Viewer] ✗ Could not extract packageDrawingId from URL:', instanceData.documentUrl);
                    }
                } catch (error) {
                    console.error('[Nutrient Viewer] ✗ Error saving annotation:', error);
                }

                // THEN calculate measurement (so annotation exists in DB for linking)
                if (annotationData.isMeasurement && instanceData.selectedCatalogueItem) {
                    console.log('[Nutrient Viewer] ✓ TRIGGERING MEASUREMENT CALCULATION');
                    console.log(`[Nutrient Viewer] Calling calculateMeasurement for annotation ${annotation.id}`);
                    await self.calculateMeasurement(containerId, annotation);
                } else {
                    if (!annotationData.isMeasurement) {
                        console.log('[Nutrient Viewer] ✗ Skipping calculation - not a measurement annotation');
                    }
                    if (!instanceData.selectedCatalogueItem) {
                        console.log('[Nutrient Viewer] ✗ Skipping calculation - no catalogue item selected');
                    }
                }
            }
            console.log('[Nutrient Viewer] ============================================');

            // Trigger autosave after annotation creation
            triggerAutosave();
        });

        // Listen for annotation updates
        instance.addEventListener('annotations.update', async (annotations) => {
            console.log(`[Nutrient Viewer] Annotation(s) updated: ${annotations.size}`);

            for (const annotation of annotations) {
                const annotationData = self.extractAnnotationData(annotation);

                // Notify Blazor
                if (instanceData.dotNetRef) {
                    await instanceData.dotNetRef.invokeMethodAsync('OnAnnotationUpdated', annotationData);
                }
            }

            // Trigger autosave after annotation update
            triggerAutosave();
        });

        // Listen for annotation deletion
        instance.addEventListener('annotations.delete', async (annotations) => {
            console.log(`[Nutrient Viewer] ============================================`);
            console.log(`[Nutrient Viewer] Annotation(s) deleted: ${annotations.size}`);

            for (const annotation of annotations) {
                console.log(`[Nutrient Viewer] Deleting annotation ID: ${annotation.id}`);

                // Call HTTP API endpoint to delete annotation and linked measurement (more reliable than JS interop)
                try {
                    console.log(`[Nutrient Viewer] Calling API DELETE /api/takeoff/catalogue/annotations/${annotation.id}...`);

                    const response = await fetch(`/api/takeoff/catalogue/annotations/${annotation.id}`, {
                        method: 'DELETE',
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    });

                    if (response.ok) {
                        const result = await response.json();
                        console.log(`[Nutrient Viewer] ✓ API successfully deleted annotation and linked measurement:`, result);

                        // Notify UI panel to refresh via SignalR event (handled by server)
                        console.log(`[Nutrient Viewer] ✓ Server should trigger UI refresh via SignalR`);
                    } else {
                        const errorText = await response.text();
                        console.error(`[Nutrient Viewer] ✗ API error deleting annotation: ${response.status} ${response.statusText}`, errorText);
                    }
                } catch (error) {
                    console.error(`[Nutrient Viewer] ✗ Error calling deletion API:`);
                    console.error(`[Nutrient Viewer] Error message: ${error.message}`);
                    console.error(`[Nutrient Viewer] Error stack:`, error.stack);
                    console.error(`[Nutrient Viewer] Full error:`, error);
                }
            }

            console.log(`[Nutrient Viewer] ============================================`);

            // Trigger autosave after annotation deletion
            triggerAutosave();
        });

        // Listen for annotation preset updates (includes calibration changes)
        instance.addEventListener('annotationPresets.update', async (event) => {
            console.log('[Nutrient Viewer] ============================================');
            console.log('[Nutrient Viewer] 🔔 ANNOTATION PRESETS UPDATE EVENT FIRED!');
            console.log('[Nutrient Viewer] Event object:', event);
            console.log('[Nutrient Viewer] Container ID:', containerId);

            try {
                // Get the updated presets
                const presets = await instance.getAnnotationPresets();
                console.log('[Nutrient Viewer] 📋 Current presets (full object):', presets);
                console.log('[Nutrient Viewer] 📋 Preset keys:', Object.keys(presets));

                // Log each measurement type preset
                console.log('[Nutrient Viewer] 📏 distanceMeasurement preset:', presets.distanceMeasurement);
                console.log('[Nutrient Viewer] 📏 perimeterMeasurement preset:', presets.perimeterMeasurement);
                console.log('[Nutrient Viewer] 📏 rectangleAreaMeasurement preset:', presets.rectangleAreaMeasurement);
                console.log('[Nutrient Viewer] 📏 ellipseAreaMeasurement preset:', presets.ellipseAreaMeasurement);
                console.log('[Nutrient Viewer] 📏 polygonAreaMeasurement preset:', presets.polygonAreaMeasurement);

                // Check if distance measurement preset has scale configuration
                if (presets.distanceMeasurement?.measurementValueConfiguration) {
                    const config = presets.distanceMeasurement.measurementValueConfiguration;
                    console.log('[Nutrient Viewer] ✅ Distance measurement config found:', config);
                    console.log('[Nutrient Viewer] Config keys:', Object.keys(config));

                    // Check if scale is configured
                    if (config.scale) {
                        const scale = config.scale;
                        console.log('[Nutrient Viewer] 🎯 SCALE DETECTED!');
                        console.log('[Nutrient Viewer] Scale object:', scale);
                        console.log('[Nutrient Viewer] Scale.from:', scale.from);
                        console.log('[Nutrient Viewer] Scale.to:', scale.to);
                        console.log('[Nutrient Viewer] Scale.unitFrom:', scale.unitFrom);
                        console.log('[Nutrient Viewer] Scale.unitTo:', scale.unitTo);

                        // Extract scale information
                        const scaleRatio = scale.from || 50; // Default 1:50
                        const unit = scale.unitFrom || 'm';
                        const currentPage = instance.viewState.currentPageIndex;

                        console.log(`[Nutrient Viewer] 🎉 CALIBRATION DETECTED: 1:${scaleRatio} (${unit}) on page ${currentPage}`);

                        // Notify Blazor about calibration change
                        if (instanceData.dotNetRef) {
                            console.log('[Nutrient Viewer] 📡 Sending calibration update to Blazor...');
                            await instanceData.dotNetRef.invokeMethodAsync('OnCalibrationUpdated', scaleRatio, unit, currentPage);
                            console.log('[Nutrient Viewer] ✓ Calibration update sent to Blazor successfully');
                        } else {
                            console.warn('[Nutrient Viewer] ⚠️ dotNetRef is null - cannot notify Blazor');
                        }
                    } else {
                        console.log('[Nutrient Viewer] ⚠️ No scale found in measurementValueConfiguration');
                        console.log('[Nutrient Viewer] Config object:', config);
                    }
                } else {
                    console.log('[Nutrient Viewer] ⚠️ No measurementValueConfiguration in distanceMeasurement preset');
                    console.log('[Nutrient Viewer] distanceMeasurement preset:', presets.distanceMeasurement);
                }
            } catch (error) {
                console.error('[Nutrient Viewer] ❌ Error processing preset update:', error);
                console.error('[Nutrient Viewer] Error stack:', error.stack);
            }

            console.log('[Nutrient Viewer] ============================================');
        });

        console.log('[Nutrient Viewer] Event listeners setup complete');
    },

    /**
     * Extract annotation data for storage/processing
     */
    extractAnnotationData: function(annotation) {
        const data = {
            id: annotation.id,
            type: annotation.constructor.name,
            pageIndex: annotation.pageIndex,
            isMeasurement: annotation.isMeasurement || false
        };

        // Extract measurement-specific data
        if (annotation.isMeasurement) {
            data.measurementType = annotation.measurementType; // 'distance', 'perimeter', 'area', etc.
        }

        if (annotation.measurementValue !== undefined) {
            data.value = annotation.measurementValue;
        }

        if (annotation.measurementValueConfiguration) {
            data.scale = annotation.measurementValueConfiguration.scale;
            data.precision = annotation.measurementValueConfiguration.precision;
        }

        // Extract coordinates based on annotation type
        if (annotation.lines && annotation.lines.size > 0) {
            // Line/distance measurement
            data.coordinates = {
                type: 'lines',
                data: annotation.lines.toArray().map(line => ({
                    start: { x: line.start.x, y: line.start.y },
                    end: { x: line.end.x, y: line.end.y }
                }))
            };
        } else if (annotation.points && annotation.points.size > 0) {
            // Polygon/area measurement
            data.coordinates = {
                type: 'points',
                data: annotation.points.toArray().map(p => ({ x: p.x, y: p.y }))
            };
        } else if (annotation.boundingBox) {
            // Rectangle/ellipse
            const bb = annotation.boundingBox;
            data.coordinates = {
                type: 'boundingBox',
                data: { x: bb.left, y: bb.top, width: bb.width, height: bb.height }
            };
        }

        // Extract text/note content
        if (annotation.text) {
            data.text = annotation.text;
        }

        // Extract custom data if present
        if (annotation.customData) {
            data.customData = annotation.customData;
        }

        return data;
    },

    /**
     * Load saved annotations into the viewer
     * @param {string} containerId - Container element ID
     * @param {Array} annotations - Array of annotation data objects
     */
    loadAnnotations: async function(containerId, annotations) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot load annotations - no instance');
            return { success: false };
        }

        try {
            console.log(`[Nutrient Viewer] Loading ${annotations.length} annotations...`);

            // Convert annotation data back to PSPDFKit annotation objects
            const pspdfAnnotations = [];
            for (const annData of annotations) {
                // This would need to be implemented based on your annotation storage format
                // PSPDFKit provides methods to create annotations from JSON
                console.log('[Nutrient Viewer] Loading annotation:', annData);
            }

            // TODO: Implement annotation loading from saved data
            // await instanceData.instance.create(pspdfAnnotations);

            return { success: true, count: annotations.length };
        } catch (error) {
            console.error('[Nutrient Viewer] Error loading annotations:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Get current page number
     */
    getCurrentPageIndex: function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) return 0;
        return instanceData.instance.viewState.currentPageIndex;
    },

    /**
     * Get total page count
     */
    getTotalPageCount: function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) return 0;
        return instanceData.instance.totalPageCount;
    },

    /**
     * Get next available color from the color pool
     * Cycles through colors and ensures maximum visual distinction
     * @param {string} containerId - Container element ID
     * @returns {object} RGB color object { r, g, b }
     */
    getNextAvailableColor: function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData) {
            console.warn('[Nutrient Viewer] Cannot get color - no instance');
            return this.colorPool[0]; // Return first color as fallback
        }

        // Get the next color from the pool using round-robin
        const color = this.colorPool[instanceData.colorPoolIndex];

        // Increment index and wrap around
        instanceData.colorPoolIndex = (instanceData.colorPoolIndex + 1) % this.colorPool.length;

        console.log(`[Nutrient Viewer] Assigned color from pool: rgb(${color.r}, ${color.g}, ${color.b}), next index: ${instanceData.colorPoolIndex}`);

        return color;
    },

    /**
     * Get color for a category (DEPRECATED - kept for backward compatibility)
     * Use getNextAvailableColor() instead for better color distribution
     * @param {string} category - Category name
     * @returns {object} PSPDFKit Color object
     */
    getCategoryColor: function(category) {
        // This function is now deprecated in favor of color pool system
        // Keeping it for backward compatibility but not used in new code
        const categoryColors = {
            'Universal Beams': { r: 65, g: 105, b: 225 },      // Royal Blue
            'Universal Columns': { r: 70, g: 130, b: 180 },    // Steel Blue
            'Parallel Flange Channels': { r: 100, g: 149, b: 237 }, // Cornflower Blue
            'Plates': { r: 128, g: 128, b: 128 },              // Grey
            'Hollow Sections': { r: 255, g: 140, b: 0 },       // Dark Orange
            'Circular Hollow Sections': { r: 255, g: 165, b: 0 }, // Orange
            'Square Hollow Sections': { r: 255, g: 127, b: 80 }, // Coral
            'Rectangular Hollow Sections': { r: 255, g: 99, b: 71 }, // Tomato
            'Bars': { r: 139, g: 0, b: 0 },                    // Dark Red
            'Round Bars': { r: 178, g: 34, b: 34 },            // Firebrick
            'Flat Bars': { r: 220, g: 20, b: 60 },             // Crimson
            'Square Bars': { r: 255, g: 0, b: 0 },             // Red
            'Hexagon Bars': { r: 199, g: 21, b: 133 },         // Medium Violet Red
            'Angles': { r: 34, g: 139, b: 34 },                // Forest Green
            'Equal Angles': { r: 50, g: 205, b: 50 },          // Lime Green
            'Unequal Angles': { r: 0, g: 128, b: 0 },          // Green
            'Pipes': { r: 75, g: 0, b: 130 },                  // Indigo
            'Welded Pipes': { r: 138, g: 43, b: 226 },         // Blue Violet
            'Seamless Pipes': { r: 147, g: 112, b: 219 },      // Medium Purple
            'Grating': { r: 0, g: 139, b: 139 },               // Dark Cyan
            'Mesh': { r: 0, g: 206, b: 209 },                  // Dark Turquoise
            'Handrails': { r: 72, g: 61, b: 139 },             // Dark Slate Blue
            'Fasteners': { r: 169, g: 169, b: 169 },           // Dark Grey
            'Default': { r: 255, g: 0, b: 0 }                  // Red (fallback)
        };

        const color = categoryColors[category] || categoryColors['Default'];
        return new PSPDFKit.Color(color);
    },

    /**
     * Set selected catalogue item for measurements
     * Uses color pool system to assign unique colors to each catalogue item
     * @param {string} containerId - Container element ID
     * @param {number} catalogueItemId - Catalogue item ID
     * @param {string} itemCode - Item code
     * @param {string} description - Item description
     * @param {string} category - Category (for reference, not used for color mapping)
     */
    setSelectedCatalogueItem: async function(containerId, catalogueItemId, itemCode, description, category) {
        const instanceData = this.instances[containerId];
        if (!instanceData) {
            console.warn('[Nutrient Viewer] Cannot set catalogue item - no instance');
            return;
        }

        console.log('[Nutrient Viewer] ============================================');
        console.log('[Nutrient Viewer] CATALOGUE ITEM SELECTED:');
        console.log(`[Nutrient Viewer]   - ID: ${catalogueItemId}`);
        console.log(`[Nutrient Viewer]   - Item Code: ${itemCode}`);
        console.log(`[Nutrient Viewer]   - Description: ${description}`);
        console.log(`[Nutrient Viewer]   - Category: ${category}`);

        // Get next available color from the pool
        const colorRGB = this.getNextAvailableColor(containerId);
        const color = new PSPDFKit.Color(colorRGB);

        instanceData.selectedCatalogueItem = {
            id: catalogueItemId,
            itemCode: itemCode,
            description: description,
            category: category,
            assignedColor: colorRGB // Store the assigned color for this catalogue item
        };

        console.log(`[Nutrient Viewer] Catalogue item stored with assigned color: rgb(${colorRGB.r}, ${colorRGB.g}, ${colorRGB.b})`);

        // Update annotation presets with the new color
        if (instanceData.instance) {
            console.log(`[Nutrient Viewer] Setting annotation color: rgb(${colorRGB.r}, ${colorRGB.g}, ${colorRGB.b})`);

            await instanceData.instance.setAnnotationPresets((presets) => {
                presets.distanceMeasurement = {
                    ...presets.distanceMeasurement,
                    strokeColor: color,
                    fillColor: color.set('a', 0.3) // Semi-transparent fill
                };
                presets.perimeterMeasurement = {
                    ...presets.perimeterMeasurement,
                    strokeColor: color,
                    fillColor: color.set('a', 0.3)
                };
                presets.rectangleAreaMeasurement = {
                    ...presets.rectangleAreaMeasurement,
                    strokeColor: color,
                    fillColor: color.set('a', 0.3)
                };
                presets.ellipseAreaMeasurement = {
                    ...presets.ellipseAreaMeasurement,
                    strokeColor: color,
                    fillColor: color.set('a', 0.3)
                };
                presets.polygonAreaMeasurement = {
                    ...presets.polygonAreaMeasurement,
                    strokeColor: color,
                    fillColor: color.set('a', 0.3)
                };

                console.log('[Nutrient Viewer] ✓ Color updated for all measurement types');
                return presets;
            });

            // Force update the UI by refreshing the annotation toolbar state
            // This makes the color change visible immediately in the UI
            const currentTool = await instanceData.instance.getActiveAnnotationTool();
            if (currentTool) {
                // Re-set the same tool to force UI refresh with new color
                await instanceData.instance.setActiveAnnotationTool(null);
                await instanceData.instance.setActiveAnnotationTool(currentTool);
                console.log('[Nutrient Viewer] ✓ Refreshed annotation toolbar to show new color');
            }
        }

        console.log('[Nutrient Viewer] ============================================');
    },

    /**
     * Clear selected catalogue item
     * @param {string} containerId - Container element ID
     */
    clearSelectedCatalogueItem: function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData) {
            console.warn('[Nutrient Viewer] Cannot clear catalogue item - no instance');
            return;
        }

        instanceData.selectedCatalogueItem = null;
        console.log('[Nutrient Viewer] Catalogue item selection cleared');
    },

    /**
     * Call the calculation API when a measurement is completed
     * @param {string} containerId - Container element ID
     * @param {object} annotation - Measurement annotation
     */
    calculateMeasurement: async function(containerId, annotation) {
        console.log('[Nutrient Viewer] ============================================');
        console.log('[Nutrient Viewer] calculateMeasurement() CALLED');

        const instanceData = this.instances[containerId];
        if (!instanceData?.selectedCatalogueItem) {
            console.error('[Nutrient Viewer] ✗ No catalogue item selected, skipping calculation');
            console.log('[Nutrient Viewer] ============================================');
            return null;
        }

        try {
            const catalogueItem = instanceData.selectedCatalogueItem;
            console.log(`[Nutrient Viewer] Using catalogue item: ${catalogueItem.itemCode}`);

            // Extract measurement value and type
            let measurementValue = 0;
            let measurementType = 'linear';
            let unit = 'm'; // Default unit
            let measurementLabel = '';

            // Try to get measurement value using PSPDFKit's getMeasurementDetails() method
            if (typeof annotation.getMeasurementDetails === 'function') {
                try {
                    const measurementDetails = annotation.getMeasurementDetails();
                    if (measurementDetails && measurementDetails.value !== undefined) {
                        measurementValue = measurementDetails.value;
                        measurementLabel = measurementDetails.label || '';
                        console.log(`[Nutrient Viewer] ✓ Measurement value from getMeasurementDetails(): ${measurementValue}`);
                        console.log(`[Nutrient Viewer] Measurement label: ${measurementLabel}`);

                        // Extract unit from label (e.g., "3.09 in" -> "in")
                        if (measurementLabel) {
                            const unitMatch = measurementLabel.match(/\s+(in|ft|m|mm|cm|km|yd)$/i);
                            if (unitMatch) {
                                unit = unitMatch[1].toLowerCase();
                                console.log(`[Nutrient Viewer] ✓ Extracted unit from label: ${unit}`);
                            }
                        }
                    } else {
                        console.warn('[Nutrient Viewer] getMeasurementDetails() returned undefined value');
                    }
                } catch (error) {
                    console.error('[Nutrient Viewer] Error calling getMeasurementDetails():', error);
                }
            } else {
                console.log(`[Nutrient Viewer] Annotation measurementValue property: ${annotation.measurementValue}`);
                if (annotation.measurementValue !== undefined) {
                    measurementValue = annotation.measurementValue;
                    console.log(`[Nutrient Viewer] ✓ Measurement value from property: ${measurementValue}`);
                } else {
                    console.error('[Nutrient Viewer] ✗ Measurement value is undefined and getMeasurementDetails() not available!');
                }
            }

            // Try to get unit from measurement scale if not already extracted
            if (unit === 'm' && annotation.measurementScale) {
                const scale = annotation.measurementScale;
                if (scale.unitTo) {
                    unit = scale.unitTo.toLowerCase();
                    console.log(`[Nutrient Viewer] ✓ Using unit from measurementScale.unitTo: ${unit}`);
                } else if (scale.unitFrom) {
                    unit = scale.unitFrom.toLowerCase();
                    console.log(`[Nutrient Viewer] ✓ Using unit from measurementScale.unitFrom: ${unit}`);
                }
            }

            // Determine measurement type from annotation using PSPDFKit properties
            console.log(`[Nutrient Viewer] Annotation measurementType property: ${annotation.measurementType}`);
            console.log(`[Nutrient Viewer] Annotation subject property: ${annotation.subject}`);
            console.log(`[Nutrient Viewer] Annotation constructor.name: ${annotation.constructor.name}`);

            // Try annotation.measurementType first (PSPDFKit standard property)
            if (annotation.measurementType) {
                measurementType = annotation.measurementType.toLowerCase();
                console.log(`[Nutrient Viewer] ✓ Using annotation.measurementType: ${measurementType}`);
            }
            // Try annotation.subject (often contains 'Distance', 'Area', 'Perimeter')
            else if (annotation.subject && typeof annotation.subject === 'string') {
                const subject = annotation.subject.toLowerCase();
                console.log(`[Nutrient Viewer] Checking annotation.subject: ${subject}`);
                if (subject.includes('distance') || subject.includes('line')) {
                    measurementType = 'distance';
                } else if (subject.includes('area') || subject.includes('polygon') || subject.includes('rectangle') || subject.includes('ellipse')) {
                    measurementType = 'area';
                } else if (subject.includes('perimeter')) {
                    measurementType = 'perimeter';
                }
                console.log(`[Nutrient Viewer] ✓ Determined measurementType from subject: ${measurementType}`);
            }
            // Check if annotation is instance of specific PSPDFKit classes
            else if (annotation instanceof PSPDFKit.Annotations.PolygonAnnotation ||
                     annotation instanceof PSPDFKit.Annotations.RectangleAnnotation ||
                     annotation instanceof PSPDFKit.Annotations.EllipseAnnotation) {
                measurementType = 'area';
                console.log(`[Nutrient Viewer] ✓ Detected area annotation by instance type`);
            }
            else if (annotation instanceof PSPDFKit.Annotations.LineAnnotation ||
                     annotation instanceof PSPDFKit.Annotations.PolylineAnnotation) {
                measurementType = 'distance';
                console.log(`[Nutrient Viewer] ✓ Detected linear annotation by instance type`);
            }
            // Fallback to constructor name if needed
            else if (annotation.constructor.name) {
                const typeName = annotation.constructor.name.toLowerCase();
                console.log(`[Nutrient Viewer] Using constructor name fallback: ${typeName}`);
                if (typeName.includes('distance') || typeName.includes('line') || typeName.includes('polyline')) {
                    measurementType = 'distance';
                } else if (typeName.includes('area') || typeName.includes('polygon') || typeName.includes('rectangle') || typeName.includes('ellipse')) {
                    measurementType = 'area';
                } else if (typeName.includes('perimeter')) {
                    measurementType = 'perimeter';
                }
                console.log(`[Nutrient Viewer] ✓ Determined measurementType from constructor: ${measurementType}`);
            }

            // Normalize measurement type names
            const typeMap = {
                'distance': 'linear',
                'perimeter': 'linear',
                'line': 'linear',
                'area': 'area',
                'polygon': 'area',
                'rectangle': 'area',
                'ellipse': 'area'
            };
            measurementType = typeMap[measurementType] || measurementType;

            console.log('[Nutrient Viewer] ----------------------------------------');
            console.log('[Nutrient Viewer] CALLING CALCULATION API:');
            console.log(`[Nutrient Viewer]   - Catalogue Item: ${catalogueItem.itemCode}`);
            console.log(`[Nutrient Viewer]   - Catalogue Item ID: ${catalogueItem.id}`);
            console.log(`[Nutrient Viewer]   - Measurement Value: ${measurementValue} ${unit}`);
            console.log(`[Nutrient Viewer]   - Measurement Type: ${measurementType}`);
            console.log(`[Nutrient Viewer]   - Annotation ID: ${annotation.id}`);
            console.log('[Nutrient Viewer] ----------------------------------------');

            const requestBody = {
                catalogueItemId: catalogueItem.id,
                measurementType: measurementType,
                value: measurementValue,
                unit: unit,
                annotationId: annotation.id
            };
            console.log('[Nutrient Viewer] Request body:', JSON.stringify(requestBody, null, 2));

            // Call the calculation API
            const response = await fetch('/api/takeoff/catalogue/calculate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });

            console.log(`[Nutrient Viewer] API response status: ${response.status} ${response.statusText}`);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('[Nutrient Viewer] ✗ Calculation API error:', response.status, errorText);
                console.log('[Nutrient Viewer] ============================================');
                return null;
            }

            const result = await response.json();
            console.log('[Nutrient Viewer] ✓ Calculation result received:');
            console.log(JSON.stringify(result, null, 2));
            console.log(`[Nutrient Viewer] ✓ Annotation ID in result: ${result.annotationId}`);

            // Notify Blazor measurement panel to show the result
            console.log(`[Nutrient Viewer] dotNetRef exists: ${instanceData.dotNetRef !== null}`);
            if (instanceData.dotNetRef) {
                console.log('[Nutrient Viewer] Calling Blazor OnMeasurementCalculated...');

                await instanceData.dotNetRef.invokeMethodAsync('OnMeasurementCalculated', result);

                console.log('[Nutrient Viewer] ✓ Successfully sent calculation result to Blazor');
            } else {
                console.error('[Nutrient Viewer] ✗ dotNetRef is null - cannot notify Blazor!');
            }

            console.log('[Nutrient Viewer] ============================================');
            return result;

        } catch (error) {
            console.error('[Nutrient Viewer] ✗ ERROR calculating measurement:');
            console.error(error);
            console.error('[Nutrient Viewer] Error stack:', error.stack);
            console.log('[Nutrient Viewer] ============================================');
            return null;
        }
    },

    /**
     * Delete annotation from PDF viewer by annotation ID
     * @param {string} containerId - Container element ID
     * @param {string} annotationId - PSPDFKit annotation ID
     */
    deleteAnnotationById: async function(containerId, annotationId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot delete annotation - no instance');
            return;
        }

        try {
            console.log(`[Nutrient Viewer] Deleting annotation ${annotationId} from PDF viewer...`);

            // Get total page count
            const totalPages = instanceData.instance.totalPageCount;

            // Search all pages for the annotation
            let annotationToDelete = null;
            for (let pageIndex = 0; pageIndex < totalPages; pageIndex++) {
                const annotations = await instanceData.instance.getAnnotations(pageIndex);
                annotationToDelete = annotations.find(a => a.id === annotationId);
                if (annotationToDelete) {
                    console.log(`[Nutrient Viewer] Found annotation on page ${pageIndex}`);
                    break;
                }
            }

            if (annotationToDelete) {
                await instanceData.instance.delete(annotationToDelete);
                console.log(`[Nutrient Viewer] ✓ Deleted annotation ${annotationId} from PDF viewer`);
            } else {
                console.warn(`[Nutrient Viewer] Annotation ${annotationId} not found in PDF viewer`);
            }
        } catch (error) {
            console.error(`[Nutrient Viewer] Error deleting annotation ${annotationId}:`, error);
        }
    },

    /**
     * Export all annotations as PSPDFKit Instant JSON
     * @param {string} containerId - Container element ID
     * @returns {Promise<string>} Instant JSON string
     */
    exportAnnotations: async function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot export annotations - no instance');
            return null;
        }

        try {
            console.log('[Nutrient Viewer] Exporting annotations as Instant JSON...');
            const instantJSON = await instanceData.instance.exportInstantJSON();

            // Stringify the object before logging and returning
            const instantJSONString = JSON.stringify(instantJSON);
            console.log('[Nutrient Viewer] ✓ Exported Instant JSON:', instantJSONString.substring(0, 200) + '...');
            return instantJSONString;
        } catch (error) {
            console.error('[Nutrient Viewer] Error exporting annotations:', error);
            return null;
        }
    },

    /**
     * Import annotations from PSPDFKit Instant JSON
     * @param {string} containerId - Container element ID
     * @param {string} instantJSON - Instant JSON string
     */
    importAnnotations: async function(containerId, instantJSON) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot import annotations - no instance');
            return { success: false, error: 'No instance' };
        }

        // Validate instantJSON is not null or empty
        if (!instantJSON) {
            console.log('[Nutrient Viewer] No Instant JSON to import');
            return { success: true, message: 'No annotations to import' };
        }

        try {
            console.log('[Nutrient Viewer] Importing annotations from Instant JSON...');

            const parsed = JSON.parse(instantJSON);

            // Check if there's any content to import
            // Nutrient exports Instant JSON with "annotations" array
            // Operations are for incremental changes (separate format)
            const hasAnnotations = parsed.annotations && parsed.annotations.length > 0;
            const hasOperations = parsed.operations && parsed.operations.length > 0;

            if (!hasAnnotations && !hasOperations) {
                console.log('[Nutrient Viewer] Instant JSON contains no annotations or operations, skipping import');
                return { success: true, message: 'No content to import' };
            }

            console.log(`[Nutrient Viewer] 🔍 DEBUG: Importing - annotations: ${parsed.annotations?.length || 0}, operations: ${parsed.operations?.length || 0}`);

            // For Instant JSON (annotations array): Use applyOperations with applyInstantJson operation type
            // For operations array: Pass operations directly to applyOperations
            if (hasAnnotations) {
                // PSPDFKit/Nutrient Web SDK: applyOperations accepts an array of operation objects
                // To import Instant JSON, use operation type "applyInstantJson"
                await instanceData.instance.applyOperations([
                    {
                        type: "applyInstantJson",
                        instantJson: parsed
                    }
                ]);
                console.log('[Nutrient Viewer] ✓ Instant JSON imported successfully via applyOperations([{type: "applyInstantJson"}])');
            } else if (hasOperations) {
                // For incremental operations, pass the operations array directly
                await instanceData.instance.applyOperations(parsed.operations);
                console.log('[Nutrient Viewer] ✓ Operations applied successfully via applyOperations()');
            }

            return { success: true };
        } catch (error) {
            console.error('[Nutrient Viewer] Error importing annotations:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Reload Instant JSON from database (for multi-tab sync)
     * Uses applyOperations() for seamless annotation updates without full reload
     * Requires Document Creation license
     * @param {string} containerId - Container element ID
     * @param {number} packageDrawingId - Package drawing ID
     */
    reloadInstantJson: async function(containerId, packageDrawingId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot reload Instant JSON - no instance');
            return { success: false, error: 'No instance' };
        }

        try {
            // Check if this tab just triggered autosave (within last 5 seconds)
            const timeSinceAutosave = Date.now() - (instanceData.lastAutosaveTimestamp || 0);
            if (timeSinceAutosave < 5000) {
                console.log('[Nutrient Viewer] ⏭️ Skipping reload - this tab just triggered autosave', timeSinceAutosave, 'ms ago');
                return { success: true, message: 'Skipped - tab triggered autosave' };
            }

            console.log('[Nutrient Viewer] 🔄 Reloading Instant JSON from database for drawing', packageDrawingId);

            // DISABLE autosave during reload to prevent empty saves
            instanceData.autosaveEnabled = false;
            console.log('[Nutrient Viewer] 🚫 Autosave disabled during reload');

            // Fetch latest Instant JSON from database
            const response = await fetch(`/api/packagedrawings/${packageDrawingId}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch drawing: ${response.statusText}`);
            }

            const drawing = await response.json();
            const instantJson = drawing.instantJson;

            console.log('[Nutrient Viewer] 🔍 Received from database - instantJson length:', instantJson?.length || 0);

            if (!instantJson) {
                console.log('[Nutrient Viewer] No Instant JSON in database, skipping reload');
                instanceData.autosaveEnabled = true;
                return { success: true, message: 'No annotations to reload' };
            }

            // Parse Instant JSON
            let parsedInstantJson = null;
            try {
                parsedInstantJson = JSON.parse(instantJson);
                console.log('[Nutrient Viewer] ✓ Parsed Instant JSON - annotations:', parsedInstantJson.annotations?.length || 0);
            } catch (parseError) {
                console.error('[Nutrient Viewer] Failed to parse Instant JSON:', parseError);
                instanceData.autosaveEnabled = true;
                return { success: false, error: 'Failed to parse Instant JSON' };
            }

            // Clear existing annotations from all pages
            console.log('[Nutrient Viewer] 🗑️ Clearing existing annotations...');
            const allAnnotations = [];
            for (let pageIndex = 0; pageIndex < instanceData.instance.totalPageCount; pageIndex++) {
                const pageAnnotations = await instanceData.instance.getAnnotations(pageIndex);
                allAnnotations.push(...pageAnnotations.toArray());
            }

            if (allAnnotations.length > 0) {
                console.log(`[Nutrient Viewer] Removing ${allAnnotations.length} existing annotations...`);
                await instanceData.instance.delete(allAnnotations);
            }

            // Apply Instant JSON using applyOperations (requires Document Creation license)
            console.log('[Nutrient Viewer] 📥 Applying Instant JSON via applyOperations...');
            await instanceData.instance.applyOperations([
                {
                    type: "applyInstantJson",
                    instantJson: parsedInstantJson
                }
            ]);

            console.log('[Nutrient Viewer] ✓ Instant JSON applied successfully - seamless update without full reload');

            // RE-ENABLE autosave after reload completes
            instanceData.autosaveEnabled = true;
            console.log('[Nutrient Viewer] ✅ Autosave re-enabled after reload');

            return { success: true };

        } catch (error) {
            console.error('[Nutrient Viewer] ❌ Error reloading Instant JSON:', error);
            console.error('[Nutrient Viewer] Error details:', error.message);

            // Check if it's a license error
            if (error.message && error.message.includes('license')) {
                console.error('[Nutrient Viewer] ⚠️ LICENSE ERROR: Document Creation license required for applyOperations()');
                console.error('[Nutrient Viewer] Please update license key to include Document Creation feature');
            }

            // Make sure to re-enable autosave even if reload fails
            instanceData.autosaveEnabled = true;
            return { success: false, error: error.message };
        }
    },

    /**
     * Get annotation by ID
     * @param {string} containerId - Container element ID
     * @param {string} annotationId - Annotation ID
     */
    getAnnotationById: async function(containerId, annotationId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot get annotation - no instance');
            return null;
        }

        try {
            const annotations = await instanceData.instance.getAnnotations(0); // Get from first page
            const annotation = annotations.find(a => a.id === annotationId);
            return annotation;
        } catch (error) {
            console.error('[Nutrient Viewer] Error getting annotation:', error);
            return null;
        }
    },

    /**
     * Unload/destroy PSPDFKit instance
     */
    unload: async function(containerId) {
        const instanceData = this.instances[containerId];
        if (instanceData?.instance) {
            console.log(`[Nutrient Viewer] Unloading PSPDFKit instance from ${containerId}`);
            try {
                await PSPDFKit.unload(instanceData.instance);
                instanceData.instance = null;
                console.log('[Nutrient Viewer] ✓ Instance unloaded');
                return { success: true };
            } catch (error) {
                console.error('[Nutrient Viewer] Error unloading:', error);
                return { success: false, error: error.message };
            }
        }
        return { success: true };
    },

    /**
     * Add OCR bounding rectangle overlay
     * @param {string} containerId - Container ID
     * @param {number} x - X coordinate
     * @param {number} y - Y coordinate
     * @param {number} width - Width
     * @param {number} height - Height
     * @param {string} fieldName - Field name (e.g., "Drawing Number")
     * @param {string} value - Extracted value
     * @param {number} confidence - OCR confidence (0-1)
     */
    addOcrRectangle: async function(containerId, x, y, width, height, fieldName, value, confidence) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot add OCR rectangle - no instance');
            return;
        }

        try {
            const annotation = new PSPDFKit.Annotations.RectangleAnnotation({
                pageIndex: 0,
                boundingBox: new PSPDFKit.Geometry.Rect({
                    left: x,
                    top: y,
                    width: width,
                    height: height
                }),
                strokeColor: new PSPDFKit.Color({ r: 0, g: 200, b: 0 }), // Green
                strokeWidth: 2,
                fillColor: new PSPDFKit.Color({ r: 0, g: 200, b: 0, a: 0.1 }), // 10% opacity
                note: `${fieldName}: ${value}\nConfidence: ${(confidence * 100).toFixed(0)}%`,
                isEditable: false,
                noPrint: true,
                customData: {
                    isOcrField: true,
                    fieldName: fieldName,
                    extractedValue: value,
                    confidence: confidence
                }
            });

            await instanceData.instance.create(annotation);
            console.log(`[Nutrient Viewer] ✓ OCR rectangle added: ${fieldName} = "${value}" (${(confidence * 100).toFixed(0)}%)`);
        } catch (error) {
            console.error('[Nutrient Viewer] Error adding OCR rectangle:', error);
        }
    },

    /**
     * Set measurement scale configuration for PSPDFKit
     * @param {string} containerId - Container ID
     * @param {number} scale - Scale ratio (e.g., 50 for 1:50)
     * @param {string} unit - Unit of measurement (mm, m, ft, in)
     * @returns {Promise<object>} Success result
     */
    setMeasurementScale: async function(containerId, scale, unit) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot set measurement scale - no instance');
            return { success: false, error: 'No instance' };
        }

        try {
            console.log(`[Nutrient Viewer] Setting measurement scale: 1:${scale} (${unit})`);

            // Store scale in instance data
            instanceData.currentScale = scale;

            // PSPDFKit measurement configuration
            // The scale is applied through measurement presets
            // For a 1:50 scale, 1 unit in PDF = 50 units in real world
            const scaleConfig = {
                scale: {
                    unitFrom: unit,  // Real-world unit
                    unitTo: unit,    // PDF unit (same)
                    from: scale,     // Real-world distance
                    to: 1            // PDF distance
                },
                precision: 2  // Decimal places
            };

            await instanceData.instance.setAnnotationPresets((presets) => {
                // Apply to all measurement types
                if (presets.distanceMeasurement) {
                    presets.distanceMeasurement = {
                        ...presets.distanceMeasurement,
                        measurementValueConfiguration: scaleConfig
                    };
                }

                if (presets.perimeterMeasurement) {
                    presets.perimeterMeasurement = {
                        ...presets.perimeterMeasurement,
                        measurementValueConfiguration: scaleConfig
                    };
                }

                if (presets.rectangleAreaMeasurement) {
                    presets.rectangleAreaMeasurement = {
                        ...presets.rectangleAreaMeasurement,
                        measurementValueConfiguration: scaleConfig
                    };
                }

                if (presets.ellipseAreaMeasurement) {
                    presets.ellipseAreaMeasurement = {
                        ...presets.ellipseAreaMeasurement,
                        measurementValueConfiguration: scaleConfig
                    };
                }

                if (presets.polygonAreaMeasurement) {
                    presets.polygonAreaMeasurement = {
                        ...presets.polygonAreaMeasurement,
                        measurementValueConfiguration: scaleConfig
                    };
                }

                console.log('[Nutrient Viewer] ✓ Measurement scale configuration applied to all measurement types');
                return presets;
            });

            console.log(`[Nutrient Viewer] ✓ Measurement scale set to 1:${scale} (${unit})`);
            return { success: true, scale: scale, unit: unit };

        } catch (error) {
            console.error('[Nutrient Viewer] Error setting measurement scale:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Export PDF with embedded annotations for SharePoint upload
     * Returns base64-encoded PDF to avoid SignalR message size limits with large byte arrays
     * @param {string} containerId - Container ID
     * @returns {Promise<string>} Base64-encoded PDF or null
     */
    exportPDF: async function(containerId) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot export PDF - no instance');
            return null;
        }

        try {
            console.log('[Nutrient Viewer] Exporting PDF with embedded annotations...');

            // Export PDF with all annotations embedded
            const arrayBuffer = await instanceData.instance.exportPDF();

            console.log(`[Nutrient Viewer] PDF exported from PSPDFKit (${arrayBuffer.byteLength} bytes)`);

            // Convert ArrayBuffer to Base64 to avoid SignalR message size limits
            // This is more efficient than transferring large byte arrays over SignalR
            const uint8Array = new Uint8Array(arrayBuffer);
            let binaryString = '';
            const chunkSize = 8192;

            // Build binary string in chunks to avoid stack overflow
            for (let i = 0; i < uint8Array.length; i += chunkSize) {
                const chunk = uint8Array.subarray(i, Math.min(i + chunkSize, uint8Array.length));
                binaryString += String.fromCharCode.apply(null, chunk);
            }

            const base64 = btoa(binaryString);

            console.log(`[Nutrient Viewer] ✓ PDF exported successfully (${arrayBuffer.byteLength} bytes, base64: ${base64.length} chars)`);
            return base64;

        } catch (error) {
            console.error('[Nutrient Viewer] ❌ Error exporting PDF:', error);
            console.error('[Nutrient Viewer] Error type:', error.constructor.name);
            console.error('[Nutrient Viewer] Error message:', error.message);
            console.error('[Nutrient Viewer] Error stack:', error.stack);
            return null;
        }
    },

    /**
     * Export measurement/calibration configuration (DEPRECATED)
     *
     * NOTE: This function is no longer needed. Measurement scales are automatically
     * embedded in the PDF when using exportPDF(), and are automatically extracted
     * from the PDF using the measurementValueConfiguration callback on load.
     *
     * Returning null here prevents errors while maintaining API compatibility.
     *
     * @param {string} containerId - Container ID
     * @returns {Promise<null>} Always returns null (scales embedded in PDF)
     */
    exportMeasurementConfig: async function(containerId) {
        console.log('[Nutrient Viewer] Skipping measurement config export - scales are automatically embedded in PDF via exportPDF()');
        return null;
    },

    /**
     * Import measurement/calibration configuration (annotation presets with scale info)
     * This restores the calibration scale settings
     * @param {string} containerId - Container ID
     * @param {string} configJSON - JSON string with calibration config
     * @returns {Promise<object>} Success result
     */
    importMeasurementConfig: async function(containerId, configJSON) {
        const instanceData = this.instances[containerId];
        if (!instanceData?.instance) {
            console.warn('[Nutrient Viewer] Cannot import measurement config - no instance');
            return { success: false, error: 'No instance' };
        }

        if (!configJSON) {
            console.log('[Nutrient Viewer] No measurement config to import');
            return { success: true, message: 'No config to import' };
        }

        try {
            console.log('[Nutrient Viewer] Importing measurement/calibration configuration...');

            const config = JSON.parse(configJSON);
            console.log('[Nutrient Viewer] Parsed config:', config);

            // Apply the measurement configuration to annotation presets
            await instanceData.instance.setAnnotationPresets((presets) => {
                // Apply distance measurement config
                if (config.distanceMeasurement) {
                    presets.distanceMeasurement = {
                        ...presets.distanceMeasurement,
                        measurementValueConfiguration: config.distanceMeasurement
                    };
                    console.log('[Nutrient Viewer] ✓ Applied distanceMeasurement config');
                }

                // Apply perimeter measurement config
                if (config.perimeterMeasurement) {
                    presets.perimeterMeasurement = {
                        ...presets.perimeterMeasurement,
                        measurementValueConfiguration: config.perimeterMeasurement
                    };
                    console.log('[Nutrient Viewer] ✓ Applied perimeterMeasurement config');
                }

                // Apply rectangle area measurement config
                if (config.rectangleAreaMeasurement) {
                    presets.rectangleAreaMeasurement = {
                        ...presets.rectangleAreaMeasurement,
                        measurementValueConfiguration: config.rectangleAreaMeasurement
                    };
                    console.log('[Nutrient Viewer] ✓ Applied rectangleAreaMeasurement config');
                }

                // Apply ellipse area measurement config
                if (config.ellipseAreaMeasurement) {
                    presets.ellipseAreaMeasurement = {
                        ...presets.ellipseAreaMeasurement,
                        measurementValueConfiguration: config.ellipseAreaMeasurement
                    };
                    console.log('[Nutrient Viewer] ✓ Applied ellipseAreaMeasurement config');
                }

                // Apply polygon area measurement config
                if (config.polygonAreaMeasurement) {
                    presets.polygonAreaMeasurement = {
                        ...presets.polygonAreaMeasurement,
                        measurementValueConfiguration: config.polygonAreaMeasurement
                    };
                    console.log('[Nutrient Viewer] ✓ Applied polygonAreaMeasurement config');
                }

                return presets;
            });

            // Restore current scale in instance data
            if (config.currentScale) {
                instanceData.currentScale = config.currentScale;
            }

            console.log('[Nutrient Viewer] ✓ Measurement config imported successfully');
            return { success: true };

        } catch (error) {
            console.error('[Nutrient Viewer] Error importing measurement config:', error);
            return { success: false, error: error.message };
        }
    },

    /**
     * Enter fullscreen mode for the specified container
     * @param {string} containerId - ID of the element to fullscreen
     */
    enterFullscreen: async function(containerId) {
        try {
            console.log(`[Nutrient Viewer] Entering fullscreen for container: ${containerId}`);

            const element = document.getElementById(containerId);
            if (!element) {
                console.error(`[Nutrient Viewer] Container element not found: ${containerId}`);
                return;
            }

            // Add body class for CSS targeting
            document.body.classList.add('viewer-fullscreen');

            // Force measurement panel to become part of flex layout
            // Catalogue sidebar visibility is handled by CSS based on .catalogue-sidebar-open class
            const measurementPanel = document.querySelector('.takeoff-measurement-panel');
            const measurementFooter = document.querySelector('.takeoff-measurement-footer');

            if (measurementPanel) {
                measurementPanel.style.position = 'relative';
                measurementPanel.style.right = '0';
                measurementPanel.style.top = '0';
                measurementPanel.style.height = '100%';
                measurementPanel.style.zIndex = '1';
                console.log('[Nutrient Viewer] Forced measurement panel to relative position for fullscreen');
            }

            // Use the Fullscreen API
            if (element.requestFullscreen) {
                await element.requestFullscreen();
            } else if (element.webkitRequestFullscreen) { // Safari
                await element.webkitRequestFullscreen();
            } else if (element.mozRequestFullScreen) { // Firefox
                await element.mozRequestFullScreen();
            } else if (element.msRequestFullscreen) { // IE11
                await element.msRequestFullscreen();
            }

            // Update footer position for fullscreen mode using the shared function
            setTimeout(() => {
                updateModalAndFooterPositions();
                console.log('[Nutrient Viewer] Updated positions after entering fullscreen');
            }, 100);

            console.log('[Nutrient Viewer] ✓ Entered fullscreen mode');
        } catch (error) {
            console.error('[Nutrient Viewer] Error entering fullscreen:', error);
            // Remove class if fullscreen failed
            document.body.classList.remove('viewer-fullscreen');
        }
    },

    /**
     * Exit fullscreen mode
     */
    exitFullscreen: async function() {
        try {
            console.log('[Nutrient Viewer] Exiting fullscreen');

            // Remove body class
            document.body.classList.remove('viewer-fullscreen');

            // Restore catalogue sidebar, measurement panel, and footer to fixed positioning
            const catalogueSidebar = document.querySelector('.takeoff-catalogue-sidebar');
            const measurementPanel = document.querySelector('.takeoff-measurement-panel');
            const measurementFooter = document.querySelector('.takeoff-measurement-footer');

            if (catalogueSidebar) {
                catalogueSidebar.style.position = '';
                catalogueSidebar.style.left = '';
                catalogueSidebar.style.top = '';
                catalogueSidebar.style.height = '';
                catalogueSidebar.style.zIndex = '';
                console.log('[Nutrient Viewer] Restored catalogue sidebar to default positioning');
            }

            if (measurementPanel) {
                measurementPanel.style.position = '';
                measurementPanel.style.right = '';
                measurementPanel.style.top = '';
                measurementPanel.style.height = '';
                measurementPanel.style.zIndex = '';
                console.log('[Nutrient Viewer] Restored measurement panel to default positioning');
            }

            // Reset footer positioning
            if (measurementFooter) {
                measurementFooter.style.left = '';
                measurementFooter.style.width = '';
                console.log('[Nutrient Viewer] Measurement footer reset to default');
            }

            if (document.exitFullscreen) {
                await document.exitFullscreen();
            } else if (document.webkitExitFullscreen) { // Safari
                await document.webkitExitFullscreen();
            } else if (document.msExitFullscreen) { // IE11
                await document.msExitFullscreen();
            } else if (document.mozCancelFullScreen) { // Firefox
                await document.mozCancelFullScreen();
            }

            // Update positions after exiting fullscreen
            setTimeout(() => {
                updateModalAndFooterPositions();
                console.log('[Nutrient Viewer] Updated positions after exiting fullscreen');
            }, 100);

            console.log('[Nutrient Viewer] ✓ Exited fullscreen mode');
        } catch (error) {
            console.error('[Nutrient Viewer] Error exiting fullscreen:', error);
        }
    }
};

console.log('[Nutrient Viewer] JavaScript module loaded');

// ========================================
// DRAG-TO-RESIZE FUNCTIONALITY
// ========================================

// Constants for resize limits
const SIDEBAR_MIN_WIDTH = 320;
const SIDEBAR_MAX_WIDTH = 576; // 80% larger than default (320 + 256)
const FOOTER_MIN_HEIGHT = 200;
const FOOTER_MAX_HEIGHT = 360; // 80% larger than default (200 + 160)

// Global storage keys (not per-takeoff)
const STORAGE_KEY_SIDEBAR_WIDTH = 'fabos-catalogue-sidebar-width';
const STORAGE_KEY_FOOTER_HEIGHT = 'fabos-measurement-footer-height';

// Track which elements we've already initialized
const initializedResizeHandles = new WeakSet();

// Setup sidebar horizontal resize
function setupSidebarResize() {
    const sidebar = document.querySelector('.takeoff-catalogue-sidebar');
    const resizeHandle = document.querySelector('.sidebar-resize-handle');

    if (!sidebar || !resizeHandle) {
        return false;
    }

    // Check if already initialized
    if (initializedResizeHandles.has(resizeHandle)) {
        return true;
    }

    console.log('[Nutrient Viewer] Setting up sidebar resize');

    // Apply saved width from localStorage
    const savedWidth = localStorage.getItem(STORAGE_KEY_SIDEBAR_WIDTH);
    if (savedWidth) {
        const width = parseInt(savedWidth);
        if (width >= SIDEBAR_MIN_WIDTH && width <= SIDEBAR_MAX_WIDTH) {
            sidebar.style.width = `${width}px`;
            console.log(`[Nutrient Viewer] Applied saved sidebar width: ${width}px`);

            // Also adjust modal and footer for the saved width
            const isFullscreen = document.body.classList.contains('viewer-fullscreen');
            if (!isFullscreen) {
                // Determine main sidebar width from body classes
                let mainSidebarWidth = 280;
                if (document.body.classList.contains('sidebar-collapsed')) {
                    mainSidebarWidth = 60;
                } else if (document.body.classList.contains('sidebar-expanded')) {
                    mainSidebarWidth = 420;
                }

                const modal = document.querySelector('.modal-fullscreen');
                if (modal) {
                    modal.style.setProperty('left', `${mainSidebarWidth + width}px`, 'important');
                }

                const measurementFooter = document.querySelector('.takeoff-measurement-footer');
                if (measurementFooter && measurementFooter.classList.contains('visible')) {
                    measurementFooter.style.setProperty('left', `${mainSidebarWidth + width}px`, 'important');
                    measurementFooter.style.setProperty('width', `calc(100% - ${mainSidebarWidth + width}px)`, 'important');
                }
            }
        }
    }

    let isResizing = false;
    let startX = 0;
    let startWidth = 0;

    resizeHandle.addEventListener('mousedown', (e) => {
        isResizing = true;
        startX = e.clientX;
        startWidth = sidebar.offsetWidth;
        resizeHandle.classList.add('dragging');
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    });

    document.addEventListener('mousemove', (e) => {
        if (!isResizing) return;

        const deltaX = e.clientX - startX;
        let newWidth = startWidth + deltaX;

        // Clamp to min/max
        newWidth = Math.max(SIDEBAR_MIN_WIDTH, Math.min(SIDEBAR_MAX_WIDTH, newWidth));

        // Apply new width
        sidebar.style.width = `${newWidth}px`;

        // Check if in fullscreen mode
        const isFullscreen = document.body.classList.contains('viewer-fullscreen');

        // Adjust modal position if it exists (in normal mode, not fullscreen)
        const modal = document.querySelector('.modal-fullscreen');
        if (modal && !isFullscreen) {
            // Determine main sidebar width from body classes
            let mainSidebarWidth = 280;
            if (document.body.classList.contains('sidebar-collapsed')) {
                mainSidebarWidth = 60;
            } else if (document.body.classList.contains('sidebar-expanded')) {
                mainSidebarWidth = 420;
            }
            modal.style.setProperty('left', `${mainSidebarWidth + newWidth}px`, 'important');
        }

        // Adjust measurement footer if visible
        const measurementFooter = document.querySelector('.takeoff-measurement-footer');
        if (measurementFooter && measurementFooter.classList.contains('visible')) {
            if (!isFullscreen) {
                // In normal mode: footer needs to account for main sidebar + catalogue sidebar
                let mainSidebarWidth = 280;
                if (document.body.classList.contains('sidebar-collapsed')) {
                    mainSidebarWidth = 60;
                } else if (document.body.classList.contains('sidebar-expanded')) {
                    mainSidebarWidth = 420;
                }
                measurementFooter.style.setProperty('left', `${mainSidebarWidth + newWidth}px`, 'important');
                measurementFooter.style.setProperty('width', `calc(100% - ${mainSidebarWidth + newWidth}px)`, 'important');
            } else {
                // In fullscreen mode: footer only needs to account for catalogue sidebar
                measurementFooter.style.setProperty('left', `${newWidth}px`, 'important');
                measurementFooter.style.setProperty('width', `calc(100% - ${newWidth}px)`, 'important');
            }
        }
    });

    document.addEventListener('mouseup', () => {
        if (!isResizing) return;

        isResizing = false;
        resizeHandle.classList.remove('dragging');
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // Save to localStorage (global)
        const currentWidth = sidebar.offsetWidth;
        localStorage.setItem(STORAGE_KEY_SIDEBAR_WIDTH, currentWidth.toString());
        console.log(`[Nutrient Viewer] Saved sidebar width to localStorage: ${currentWidth}px`);
    });

    initializedResizeHandles.add(resizeHandle);
    return true;
}

// Setup footer vertical resize
function setupFooterResize() {
    const footer = document.querySelector('.takeoff-measurement-footer');
    const resizeHandle = document.querySelector('.footer-resize-handle');

    if (!footer || !resizeHandle) {
        return false;
    }

    // Check if already initialized
    if (initializedResizeHandles.has(resizeHandle)) {
        return true;
    }

    console.log('[Nutrient Viewer] Setting up footer resize');

    // Apply saved height from localStorage
    const savedHeight = localStorage.getItem(STORAGE_KEY_FOOTER_HEIGHT);
    if (savedHeight) {
        const height = parseInt(savedHeight);
        if (height >= FOOTER_MIN_HEIGHT && height <= FOOTER_MAX_HEIGHT) {
            footer.style.height = `${height}px`;
            console.log(`[Nutrient Viewer] Applied saved footer height: ${height}px`);

            // Also adjust modal bottom for the saved height (works in both normal and fullscreen modes)
            if (footer.classList.contains('visible')) {
                const modal = document.querySelector('.modal-fullscreen');
                if (modal) {
                    modal.style.setProperty('bottom', `${height}px`, 'important');
                }
            }
        }
    }

    let isResizing = false;
    let startY = 0;
    let startHeight = 0;

    resizeHandle.addEventListener('mousedown', (e) => {
        isResizing = true;
        startY = e.clientY;
        startHeight = footer.offsetHeight;
        resizeHandle.classList.add('dragging');
        document.body.style.cursor = 'row-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    });

    document.addEventListener('mousemove', (e) => {
        if (!isResizing) return;

        const deltaY = startY - e.clientY; // Inverted because footer is at bottom
        let newHeight = startHeight + deltaY;

        // Clamp to min/max
        newHeight = Math.max(FOOTER_MIN_HEIGHT, Math.min(FOOTER_MAX_HEIGHT, newHeight));

        // Apply new height
        footer.style.height = `${newHeight}px`;

        // Adjust modal bottom position if it exists (works in both normal and fullscreen modes)
        const modal = document.querySelector('.modal-fullscreen');
        if (modal && footer.classList.contains('visible')) {
            modal.style.setProperty('bottom', `${newHeight}px`, 'important');
        }
    });

    document.addEventListener('mouseup', () => {
        if (!isResizing) return;

        isResizing = false;
        resizeHandle.classList.remove('dragging');
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // Save to localStorage (global)
        const currentHeight = footer.offsetHeight;
        localStorage.setItem(STORAGE_KEY_FOOTER_HEIGHT, currentHeight.toString());
        console.log(`[Nutrient Viewer] Saved footer height to localStorage: ${currentHeight}px`);
    });

    initializedResizeHandles.add(resizeHandle);
    return true;
}

// Function to update modal and footer positions based on current sidebar widths
function updateModalAndFooterPositions() {
    const modal = document.querySelector('.modal-fullscreen');
    const catalogueSidebar = document.querySelector('.takeoff-catalogue-sidebar');
    const measurementFooter = document.querySelector('.takeoff-measurement-footer');

    // Check if in fullscreen mode (using CSS class, not Fullscreen API)
    const isFullscreen = document.body.classList.contains('viewer-fullscreen');

    // Determine main sidebar width based on body classes (not offsetWidth which can be mid-transition)
    let mainSidebarWidth = 280; // Default
    if (document.body.classList.contains('sidebar-collapsed')) {
        mainSidebarWidth = 60;
    } else if (document.body.classList.contains('sidebar-expanded')) {
        mainSidebarWidth = 420;
    }

    // Check if catalogue sidebar is visible - use body class to avoid race condition with element class
    const isCatalogueSidebarVisible = document.body.classList.contains('catalogue-sidebar-open');
    const catalogueWidth = isCatalogueSidebarVisible && catalogueSidebar ? (catalogueSidebar.offsetWidth || 320) : 0;

    // Handle sidebar visibility - manage inline left style
    if (catalogueSidebar) {
        if (!isCatalogueSidebarVisible) {
            // Closing: Get the current width and hide it off-screen to the left
            const currentWidth = catalogueSidebar.offsetWidth || 320;
            catalogueSidebar.style.setProperty('left', `-${currentWidth}px`, 'important');
            console.log(`[Nutrient Viewer] Hiding catalogue sidebar off-screen: -${currentWidth}px`);
        } else {
            // Opening: Remove inline style to let CSS control position
            catalogueSidebar.style.removeProperty('left');
            console.log(`[Nutrient Viewer] Showing catalogue sidebar - removed inline left style, CSS will position it`);
        }
    }

    // Update modal and footer positions (normal mode only - fullscreen uses CSS)
    if (!isFullscreen) {
        // NORMAL MODE: Use fixed positioning with calculated offsets
        if (modal) {
            const totalWidth = mainSidebarWidth + catalogueWidth;
            modal.style.setProperty('left', `${totalWidth}px`, 'important');
            console.log(`[Nutrient Viewer] Updated modal left to ${totalWidth}px (main: ${mainSidebarWidth}px + catalogue: ${catalogueWidth}px, visible: ${isCatalogueSidebarVisible})`);

            // Update modal bottom for footer
            if (measurementFooter && measurementFooter.classList.contains('visible')) {
                const footerHeight = measurementFooter.offsetHeight || 200;
                modal.style.setProperty('bottom', `${footerHeight}px`, 'important');
                console.log(`[Nutrient Viewer] Updated modal bottom to ${footerHeight}px for footer`);
            } else {
                modal.style.setProperty('bottom', '0px', 'important');
                console.log(`[Nutrient Viewer] Reset modal bottom to 0px (no footer)`);
            }
        }

        // Update footer position in normal mode
        if (measurementFooter && measurementFooter.classList.contains('visible')) {
            const totalWidth = mainSidebarWidth + catalogueWidth;
            measurementFooter.style.setProperty('left', `${totalWidth}px`, 'important');
            measurementFooter.style.setProperty('width', `calc(100% - ${totalWidth}px)`, 'important');
            console.log(`[Nutrient Viewer] Updated footer in normal mode: left=${totalWidth}px (main: ${mainSidebarWidth}px + catalogue: ${catalogueWidth}px, visible: ${isCatalogueSidebarVisible})`);
        }
    }
    // FULLSCREEN MODE: CSS handles all positioning via fixed positioning and z-index layering
}

// Watch for Blazor components being added to the DOM and body class changes
const resizeObserver = new MutationObserver((mutations) => {
    // Try to setup sidebar resize if not already done
    setupSidebarResize();

    // Try to setup footer resize if not already done
    setupFooterResize();

    // Check if body classes changed (for sidebar collapse/expand)
    for (const mutation of mutations) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class' && mutation.target === document.body) {
            // Body classes changed, update positions
            updateModalAndFooterPositions();
            break;
        }
    }
});

// Start observing the document body for changes
resizeObserver.observe(document.body, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ['class']
});

// Also try to initialize immediately
console.log('[Nutrient Viewer] Initializing drag-to-resize functionality');
setupSidebarResize();
setupFooterResize();
