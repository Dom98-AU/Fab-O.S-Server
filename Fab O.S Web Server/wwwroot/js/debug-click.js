// Debug script to track ALL click events on the page
document.addEventListener('click', function(e) {
    console.log('🔍 CLICK DETECTED:', {
        target: e.target,
        tagName: e.target.tagName,
        className: e.target.className,
        id: e.target.id,
        coordinates: { x: e.clientX, y: e.clientY },
        eventPhase: e.eventPhase,
        defaultPrevented: e.defaultPrevented,
        href: e.target.href,
        type: e.target.type,
        elementUnderCursor: document.elementFromPoint(e.clientX, e.clientY)
    });
}, true);

console.log('[Debug] Click tracker loaded - all clicks will be logged');
