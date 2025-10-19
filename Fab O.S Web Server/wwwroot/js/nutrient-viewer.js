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
                    selectedCatalogueItem: null // For catalogue-aware measurements
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

            // PSPDFKit requires an absolute URL for baseUrl
            const baseUrl = `${window.location.protocol}//${window.location.host}/assets/pspdfkit/`;
            console.log(`[Nutrient Viewer] Base URL: ${baseUrl}`);

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

                // Annotation tooltip callback to add line thickness control
                annotationTooltipCallback: (annotation) => {
                    // Only show thickness control for annotations that support strokeWidth
                    if (annotation.strokeWidth === undefined) {
                        return null;
                    }

                    // DO NOT show tooltip for distance measurements (toolbar already has width control)
                    // Only show for calibration and other annotation types
                    if (annotation.isMeasurement && annotation.measurementType === 'distance') {
                        console.log('[Nutrient Viewer] Suppressing tooltip for distance measurement');
                        return null;
                    }

                    return [
                        {
                            type: "custom",
                            title: "Line Thickness",
                            id: "line-thickness-control",
                            node: (() => {
                                const container = document.createElement('div');
                                container.style.cssText = 'padding: 8px; min-width: 200px;';

                                const label = document.createElement('label');
                                label.textContent = 'Line Width: ';
                                label.style.cssText = 'font-weight: 600; display: block; margin-bottom: 4px;';

                                const sliderContainer = document.createElement('div');
                                sliderContainer.style.cssText = 'display: flex; align-items: center; gap: 8px;';

                                const slider = document.createElement('input');
                                slider.type = 'range';
                                slider.min = '1';
                                slider.max = '10';
                                slider.step = '0.5';
                                slider.value = annotation.strokeWidth;
                                slider.style.cssText = 'flex: 1;';

                                const valueDisplay = document.createElement('span');
                                valueDisplay.textContent = annotation.strokeWidth + 'pt';
                                valueDisplay.style.cssText = 'min-width: 40px; text-align: right; font-weight: 600;';

                                // Get the instance to update the annotation
                                const containerId = Object.keys(nutrientViewer.instances).find(id =>
                                    nutrientViewer.instances[id].instance
                                );
                                const instanceData = nutrientViewer.instances[containerId];

                                slider.addEventListener('input', async (e) => {
                                    const newWidth = parseFloat(e.target.value);
                                    valueDisplay.textContent = newWidth + 'pt';

                                    if (instanceData?.instance) {
                                        try {
                                            const updated = annotation.set('strokeWidth', newWidth);
                                            await instanceData.instance.update(updated);
                                        } catch (error) {
                                            console.error('[Nutrient Viewer] Error updating stroke width:', error);
                                        }
                                    }
                                });

                                sliderContainer.appendChild(slider);
                                sliderContainer.appendChild(valueDisplay);
                                container.appendChild(label);
                                container.appendChild(sliderContainer);

                                return container;
                            })()
                        }
                    ];
                }
            };

            console.log('[Nutrient Viewer] Loading PSPDFKit instance...');
            instanceData.instance = await PSPDFKit.load(configuration);

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

        // Listen for annotation creation
        instance.addEventListener('annotations.create', async (annotations) => {
            console.log('[Nutrient Viewer] ============================================');
            console.log(`[Nutrient Viewer] Annotation(s) created: ${annotations.size}`);

            for (const annotation of annotations) {
                console.log(`[Nutrient Viewer] Annotation type: ${annotation.constructor.name}`);
                console.log(`[Nutrient Viewer] Annotation ID: ${annotation.id}`);
                console.log(`[Nutrient Viewer] Is measurement: ${annotation.isMeasurement}`);

                if (annotation.isMeasurement) {
                    console.log(`[Nutrient Viewer] Measurement type: ${annotation.measurementType}`);
                    console.log(`[Nutrient Viewer] Measurement value: ${annotation.measurementValue}`);
                    console.log(`[Nutrient Viewer] Measurement config:`, annotation.measurementValueConfiguration);
                }

                // Extract annotation data
                const annotationData = self.extractAnnotationData(annotation);
                console.log('[Nutrient Viewer] Extracted annotation data:', JSON.stringify(annotationData, null, 2));

                console.log(`[Nutrient Viewer] Catalogue item selected: ${instanceData.selectedCatalogueItem !== null}`);
                if (instanceData.selectedCatalogueItem) {
                    console.log(`[Nutrient Viewer] Selected catalogue item: ${instanceData.selectedCatalogueItem.itemCode} - ${instanceData.selectedCatalogueItem.description}`);
                }

                // If this is a measurement annotation and a catalogue item is selected, calculate
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

                // Notify Blazor
                console.log('[Nutrient Viewer] Notifying Blazor about annotation creation');
                if (instanceData.dotNetRef) {
                    await instanceData.dotNetRef.invokeMethodAsync('OnAnnotationCreated', annotationData);
                    console.log('[Nutrient Viewer] ✓ Blazor notified successfully');
                } else {
                    console.error('[Nutrient Viewer] ✗ dotNetRef is null - cannot notify Blazor');
                }
            }
            console.log('[Nutrient Viewer] ============================================');
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
        });

        // Listen for annotation deletion
        instance.addEventListener('annotations.delete', async (annotations) => {
            console.log(`[Nutrient Viewer] Annotation(s) deleted: ${annotations.size}`);

            for (const annotation of annotations) {
                // Notify Blazor
                if (instanceData.dotNetRef) {
                    await instanceData.dotNetRef.invokeMethodAsync('OnAnnotationDeleted', annotation.id);
                }
            }
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
     * Get color for a category (helper function)
     * Maps catalogue categories to trace colors
     * @param {string} category - Category name
     * @returns {object} PSPDFKit Color object
     */
    getCategoryColor: function(category) {
        // Color mapping for different categories
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
     * @param {string} containerId - Container element ID
     * @param {number} catalogueItemId - Catalogue item ID
     * @param {string} itemCode - Item code
     * @param {string} description - Item description
     * @param {string} category - Category for color mapping
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

        instanceData.selectedCatalogueItem = {
            id: catalogueItemId,
            itemCode: itemCode,
            description: description,
            category: category
        };

        console.log(`[Nutrient Viewer] Catalogue item stored in instance data`);

        // Auto-select trace color based on category
        if (category && instanceData.instance) {
            const color = this.getCategoryColor(category);
            console.log(`[Nutrient Viewer] Setting trace color for category '${category}':`, color);

            // Update annotation presets with the new color
            await instanceData.instance.setAnnotationPresets((presets) => {
                presets.distanceMeasurement = {
                    ...presets.distanceMeasurement,
                    strokeColor: color
                };
                presets.perimeterMeasurement = {
                    ...presets.perimeterMeasurement,
                    strokeColor: color
                };
                presets.rectangleAreaMeasurement = {
                    ...presets.rectangleAreaMeasurement,
                    strokeColor: color
                };
                presets.ellipseAreaMeasurement = {
                    ...presets.ellipseAreaMeasurement,
                    strokeColor: color
                };
                presets.polygonAreaMeasurement = {
                    ...presets.polygonAreaMeasurement,
                    strokeColor: color
                };

                console.log('[Nutrient Viewer] ✓ Trace color updated for all measurement types');
                return presets;
            });
        }
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
            let unit = 'm';

            console.log(`[Nutrient Viewer] Annotation measurementValue property: ${annotation.measurementValue}`);
            if (annotation.measurementValue !== undefined) {
                measurementValue = annotation.measurementValue;
                console.log(`[Nutrient Viewer] ✓ Measurement value extracted: ${measurementValue}`);
            } else {
                console.error('[Nutrient Viewer] ✗ Measurement value is undefined!');
            }

            // Determine measurement type from annotation
            console.log(`[Nutrient Viewer] Annotation measurementType property: ${annotation.measurementType}`);
            if (annotation.measurementType) {
                measurementType = annotation.measurementType.toLowerCase();
                console.log(`[Nutrient Viewer] ✓ Using annotation measurementType: ${measurementType}`);
            } else if (annotation.constructor.name) {
                // Fallback to annotation type
                const typeName = annotation.constructor.name.toLowerCase();
                console.log(`[Nutrient Viewer] Using constructor name fallback: ${typeName}`);
                if (typeName.includes('distance') || typeName.includes('line')) {
                    measurementType = 'linear';
                } else if (typeName.includes('area') || typeName.includes('polygon')) {
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
            console.log('[Nutrient Viewer] ----------------------------------------');

            const requestBody = {
                catalogueItemId: catalogueItem.id,
                measurementType: measurementType,
                value: measurementValue,
                unit: unit
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

            // Validate operations array exists and is not empty
            if (!parsed.operations || parsed.operations.length === 0) {
                console.log('[Nutrient Viewer] Instant JSON contains no operations, skipping import');
                return { success: true, message: 'No operations to import' };
            }

            await instanceData.instance.applyOperations(parsed);
            console.log('[Nutrient Viewer] ✓ Annotations imported successfully');
            return { success: true };
        } catch (error) {
            console.error('[Nutrient Viewer] Error importing annotations:', error);
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
    }
};

console.log('[Nutrient Viewer] JavaScript module loaded');
