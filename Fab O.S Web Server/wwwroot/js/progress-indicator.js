// Progress indicator for long operations (e.g., SharePoint upload)
window.showProgressIndicator = function(message) {
    let indicator = document.getElementById('global-progress-indicator');
    if (!indicator) {
        indicator = document.createElement('div');
        indicator.id = 'global-progress-indicator';
        indicator.innerHTML = `
            <div class="progress-backdrop"></div>
            <div class="progress-content">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="progress-message"></p>
            </div>
        `;
        document.body.appendChild(indicator);
    }

    indicator.querySelector('.progress-message').textContent = message;
    indicator.style.display = 'flex';
};

window.updateProgressIndicator = function(message) {
    const indicator = document.getElementById('global-progress-indicator');
    if (indicator) {
        indicator.querySelector('.progress-message').textContent = message;
    }
};

window.hideProgressIndicator = function() {
    const indicator = document.getElementById('global-progress-indicator');
    if (indicator) {
        indicator.style.display = 'none';
    }
};

// Toast notifications for warnings and info messages
window.showToast = function(message, type = 'info') {
    const toastContainer = getOrCreateToastContainer();

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;

    const icon = getToastIcon(type);
    toast.innerHTML = `
        <div class="toast-icon">${icon}</div>
        <div class="toast-message">${escapeHtml(message)}</div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;

    toastContainer.appendChild(toast);

    // Animate in
    setTimeout(() => toast.classList.add('show'), 10);

    // Auto-remove after 8 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 8000);
};

function getOrCreateToastContainer() {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }
    return container;
}

function getToastIcon(type) {
    switch (type) {
        case 'success':
            return '<i class="fas fa-check-circle"></i>';
        case 'error':
            return '<i class="fas fa-exclamation-circle"></i>';
        case 'warning':
            return '<i class="fas fa-exclamation-triangle"></i>';
        case 'info':
        default:
            return '<i class="fas fa-info-circle"></i>';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
