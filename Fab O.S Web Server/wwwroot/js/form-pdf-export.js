/**
 * Form PDF Export - Nutrient (PSPDFKit) Integration
 *
 * Purpose: Export forms to PDF using HTML rendering and Nutrient SDK
 * Approach: Fetch HTML from server, render in hidden frame, export via print/PSPDFKit
 */

window.formPdfExport = {

    /**
     * Export form to PDF by fetching HTML and triggering download
     * @param {string} apiUrl - API endpoint that returns HTML
     * @param {string} filename - Desired filename (without .pdf extension)
     * @returns {boolean} Success status
     */
    exportToPdf: async function(apiUrl, filename) {
        try {
            console.log('[FormPdfExport] Starting PDF export for:', filename);

            // Get auth token from cookie or storage
            const token = this.getAuthToken();

            // Fetch HTML from API
            const response = await fetch(apiUrl, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'text/html'
                },
                credentials: 'include'
            });

            if (!response.ok) {
                console.error('[FormPdfExport] Failed to fetch HTML:', response.status);
                return false;
            }

            const html = await response.text();
            console.log('[FormPdfExport] Fetched HTML, length:', html.length);

            // Try PSPDFKit conversion if available
            if (typeof PSPDFKit !== 'undefined' && PSPDFKit.convertToPDF) {
                return await this.exportViaPspdfkit(html, filename);
            }

            // Fallback: Open in new window for print-to-PDF
            return await this.exportViaPrintWindow(html, filename);

        } catch (error) {
            console.error('[FormPdfExport] Error exporting PDF:', error);
            return false;
        }
    },

    /**
     * Export using PSPDFKit's HTML-to-PDF conversion
     */
    exportViaPspdfkit: async function(html, filename) {
        try {
            console.log('[FormPdfExport] Using PSPDFKit conversion');

            // Create a hidden container for rendering
            const container = document.createElement('div');
            container.id = 'pdf-export-container';
            container.style.position = 'absolute';
            container.style.left = '-9999px';
            container.style.top = '-9999px';
            document.body.appendChild(container);

            // Use PSPDFKit Document Compressor/Converter if available
            const blob = await PSPDFKit.convertToPDF({
                container: '#pdf-export-container',
                html: html,
                options: {
                    pageSize: 'A4',
                    margins: { top: 20, bottom: 20, left: 20, right: 20 }
                }
            });

            // Clean up container
            document.body.removeChild(container);

            // Trigger download
            this.downloadBlob(blob, `${filename}.pdf`);

            console.log('[FormPdfExport] PDF exported successfully via PSPDFKit');
            return true;

        } catch (error) {
            console.error('[FormPdfExport] PSPDFKit conversion failed:', error);
            // Fallback to print window
            return await this.exportViaPrintWindow(html, filename);
        }
    },

    /**
     * Export by opening HTML in new window for print-to-PDF
     * This uses the browser's built-in print dialog which allows saving as PDF
     */
    exportViaPrintWindow: async function(html, filename) {
        try {
            console.log('[FormPdfExport] Using print window export');

            // Create new window with the HTML content
            const printWindow = window.open('', '_blank', 'width=800,height=600');

            if (!printWindow) {
                console.error('[FormPdfExport] Popup blocked - cannot open print window');
                alert('Please allow popups to export PDF');
                return false;
            }

            // Write HTML to the new window
            printWindow.document.open();
            printWindow.document.write(html);
            printWindow.document.close();

            // Set the document title for the PDF filename
            printWindow.document.title = filename;

            // Wait for content to load, then trigger print
            printWindow.onload = function() {
                setTimeout(() => {
                    printWindow.print();
                }, 500);
            };

            // For browsers that fire onload before images load
            setTimeout(() => {
                if (printWindow && !printWindow.closed) {
                    printWindow.print();
                }
            }, 1000);

            console.log('[FormPdfExport] Print window opened - user can save as PDF');
            return true;

        } catch (error) {
            console.error('[FormPdfExport] Print window export failed:', error);
            return false;
        }
    },

    /**
     * Print form by opening in new window with print dialog
     * @param {string} apiUrl - API endpoint that returns HTML
     */
    printForm: async function(apiUrl) {
        try {
            console.log('[FormPdfExport] Starting print for:', apiUrl);

            // Get auth token
            const token = this.getAuthToken();

            // Fetch HTML from API
            const response = await fetch(apiUrl, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'text/html'
                },
                credentials: 'include'
            });

            if (!response.ok) {
                console.error('[FormPdfExport] Failed to fetch HTML:', response.status);
                return;
            }

            const html = await response.text();

            // Open print window
            const printWindow = window.open('', '_blank', 'width=800,height=600');

            if (!printWindow) {
                console.error('[FormPdfExport] Popup blocked');
                alert('Please allow popups to print');
                return;
            }

            printWindow.document.open();
            printWindow.document.write(html);
            printWindow.document.close();

            // Trigger print after content loads
            printWindow.onload = function() {
                printWindow.focus();
                printWindow.print();
            };

            setTimeout(() => {
                if (printWindow && !printWindow.closed) {
                    printWindow.focus();
                    printWindow.print();
                }
            }, 1000);

        } catch (error) {
            console.error('[FormPdfExport] Error printing:', error);
        }
    },

    /**
     * Download a blob as a file
     * @param {Blob} blob - The blob to download
     * @param {string} filename - The filename
     */
    downloadBlob: function(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    /**
     * Get authentication token from cookies or localStorage
     * @returns {string} JWT token
     */
    getAuthToken: function() {
        // Try to get from cookie first
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'AuthToken' || name === 'jwt' || name === 'access_token') {
                return value;
            }
        }

        // Try localStorage
        let token = localStorage.getItem('authToken') ||
                    localStorage.getItem('jwt') ||
                    localStorage.getItem('access_token');

        if (token) {
            return token;
        }

        // Try sessionStorage
        token = sessionStorage.getItem('authToken') ||
                sessionStorage.getItem('jwt') ||
                sessionStorage.getItem('access_token');

        return token || '';
    },

    /**
     * Preview form HTML in a modal (for debugging/preview)
     * @param {string} apiUrl - API endpoint that returns HTML
     */
    previewHtml: async function(apiUrl) {
        try {
            const token = this.getAuthToken();

            const response = await fetch(apiUrl, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'text/html'
                },
                credentials: 'include'
            });

            if (!response.ok) {
                console.error('[FormPdfExport] Failed to fetch HTML');
                return;
            }

            const html = await response.text();

            // Create preview modal
            const modal = document.createElement('div');
            modal.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.8);z-index:9999;display:flex;align-items:center;justify-content:center;';

            const content = document.createElement('div');
            content.style.cssText = 'background:white;width:90%;height:90%;overflow:auto;border-radius:8px;position:relative;';

            const closeBtn = document.createElement('button');
            closeBtn.innerText = 'Close';
            closeBtn.style.cssText = 'position:absolute;top:10px;right:10px;padding:8px 16px;cursor:pointer;z-index:10;';
            closeBtn.onclick = () => document.body.removeChild(modal);

            const iframe = document.createElement('iframe');
            iframe.style.cssText = 'width:100%;height:100%;border:none;';

            content.appendChild(closeBtn);
            content.appendChild(iframe);
            modal.appendChild(content);
            document.body.appendChild(modal);

            // Write HTML to iframe
            iframe.contentDocument.open();
            iframe.contentDocument.write(html);
            iframe.contentDocument.close();

        } catch (error) {
            console.error('[FormPdfExport] Error previewing:', error);
        }
    }
};

// Also expose for direct use
window.downloadBlob = function(blob, filename) {
    window.formPdfExport.downloadBlob(blob, filename);
};

console.log('[FormPdfExport] Module loaded');
