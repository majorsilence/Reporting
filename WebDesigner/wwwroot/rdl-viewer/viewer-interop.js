// viewer-interop.js — Blazor JS interop bridge for <report-viewer>
// Loaded lazily via IJSRuntime.InvokeAsync("import", "...").
// All exports accept an ElementReference (Blazor marshals it as a DOM element).

let _ensurePromise = null;

/** Ensure the viewer Web Component script is loaded and registered. */
async function ensureViewer() {
  if (_ensurePromise) return _ensurePromise;

  if (customElements.get('report-viewer')) {
    _ensurePromise = Promise.resolve();
    return _ensurePromise;
  }

  _ensurePromise = new Promise((resolve, reject) => {
    const existing = document.querySelector(
      'script[src*="rdl-viewer/rdl-viewer.js"]');
    if (existing) {
      customElements.whenDefined('report-viewer').then(resolve, reject);
      return;
    }

    const script = document.createElement('script');
    script.type  = 'module';
    script.src   = '/_content/Majorsilence.Reporting.WebDesigner/rdl-viewer/rdl-viewer.js';
    script.onload  = () => customElements.whenDefined('report-viewer').then(resolve, reject);
    script.onerror = reject;
    document.head.appendChild(script);
  });

  return _ensurePromise;
}

/**
 * Re-render the report with its current parameter values.
 * @param {Element} el - ElementReference from Blazor
 */
export async function refresh(el) {
  await ensureViewer();
  el?.refresh();
}
