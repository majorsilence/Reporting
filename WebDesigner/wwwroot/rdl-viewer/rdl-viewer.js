// <report-viewer> — a vanilla-JS custom element that renders an RDL report with a
// parameter panel, refresh, export (PDF/CSV/XLSX), and print, using the ViewerEndpoints
// backend (POST {render-endpoint} and GET {parameters-endpoint}).
//
// Attributes:
//   render-endpoint       (required) e.g. "/rdl-viewer/render"
//   parameters-endpoint   (required) e.g. "/rdl-viewer/parameters"
//   report-name           name of a report on the server (mutually exclusive with `rdl`)
//   rdl                   inline RDL XML to render (mutually exclusive with `report-name`)

const SHADOW_CSS = `
  :host { display: block; font-family: system-ui, -apple-system, sans-serif; }
  .toolbar {
    display: flex; align-items: center; gap: 8px; padding: 8px 12px;
    background: #f5f5f5; border-bottom: 1px solid #ddd; flex-wrap: wrap;
  }
  .toolbar button {
    font: inherit; font-size: 13px; padding: 6px 12px; border: 1px solid #ccc;
    border-radius: 4px; background: white; cursor: pointer;
  }
  .toolbar button:hover { background: #eee; }
  .toolbar select { font: inherit; font-size: 13px; padding: 5px 8px; border: 1px solid #ccc; border-radius: 4px; }
  .params { display: flex; align-items: flex-end; gap: 10px; flex-wrap: wrap; padding: 10px 12px; background: #fafafa; border-bottom: 1px solid #eee; }
  .param-field { display: flex; flex-direction: column; gap: 3px; font-size: 12px; color: #444; }
  .param-field input { font: inherit; font-size: 13px; padding: 4px 6px; border: 1px solid #ccc; border-radius: 3px; min-width: 120px; }
  .param-field input[type="checkbox"] { min-width: 0; }
  .status { font-size: 12px; color: #888; padding: 0 4px; }
  .status.error { color: #c00; }
  .content { position: relative; }
  iframe { width: 100%; height: 800px; border: none; display: block; }
  .spacer { flex: 1; }
`;

const SHADOW_HTML = `
  <style>${SHADOW_CSS}</style>
  <div class="toolbar">
    <button data-action="refresh" title="Refresh">&#8635; Refresh</button>
    <select data-action="export-format">
      <option value="pdf">PDF</option>
      <option value="csv">CSV</option>
      <option value="xlsx">Excel</option>
    </select>
    <button data-action="export">&#8681; Export</button>
    <button data-action="print">&#128424; Print</button>
    <div class="spacer"></div>
    <span class="status" id="status"></span>
  </div>
  <div class="params" id="params" hidden></div>
  <div class="content">
    <iframe id="frame" sandbox="allow-same-origin allow-scripts allow-modals" title="Report output"></iframe>
  </div>
`;

class ReportViewer extends HTMLElement {
  static get observedAttributes() {
    return ['render-endpoint', 'parameters-endpoint', 'report-name', 'rdl'];
  }

  constructor() {
    super();
    this._shadow = this.attachShadow({ mode: 'open' });
    this._paramValues = {};
    this._paramMeta = [];
  }

  connectedCallback() {
    this._shadow.innerHTML = SHADOW_HTML;
    this._frame = this._shadow.getElementById('frame');
    this._statusEl = this._shadow.getElementById('status');
    this._paramsEl = this._shadow.getElementById('params');

    this._shadow.querySelector('[data-action="refresh"]').addEventListener('click', () => this.refresh());
    this._shadow.querySelector('[data-action="export"]').addEventListener('click', () => this._export());
    this._shadow.querySelector('[data-action="print"]').addEventListener('click', () => this._print());

    this._loadParameterMetadata().then(() => this.refresh());
  }

  attributeChangedCallback(name, oldVal, newVal) {
    if (oldVal === newVal || !this._frame) return;
    if (name === 'report-name' || name === 'rdl') {
      this._loadParameterMetadata().then(() => this.refresh());
    }
  }

  /// Public API: re-render with the current parameter values.
  refresh() {
    const renderEp = this.getAttribute('render-endpoint');
    if (!renderEp) {
      this._setStatus('Missing render-endpoint attribute.', true);
      return;
    }

    this._setStatus('Loading…');
    const body = {
      name: this.getAttribute('report-name') || undefined,
      rdl: this.getAttribute('rdl') || undefined,
      parameters: this._collectParamValues(),
      format: 'html',
    };

    fetch(renderEp, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
      .then(async (r) => {
        const text = await r.text();
        if (!r.ok) throw new Error(text || `HTTP ${r.status}`);
        return text;
      })
      .then((html) => {
        this._frame.srcdoc = html;
        this._setStatus('');
      })
      .catch((err) => {
        this._frame.srcdoc = `<pre style="color:#c00;font-family:monospace;padding:16px">${this._escape(err.message)}</pre>`;
        this._setStatus('Render failed.', true);
      });
  }

  async _loadParameterMetadata() {
    const paramsEp = this.getAttribute('parameters-endpoint');
    const reportName = this.getAttribute('report-name');
    if (!paramsEp || !reportName) {
      this._paramMeta = [];
      this._paramsEl.hidden = true;
      return;
    }

    try {
      const r = await fetch(`${paramsEp}/${encodeURIComponent(reportName)}`);
      if (!r.ok) { this._paramMeta = []; this._paramsEl.hidden = true; return; }
      const data = await r.json();
      this._paramMeta = data.parameters || [];
      this._renderParamFields();
    } catch {
      this._paramMeta = [];
      this._paramsEl.hidden = true;
    }
  }

  _renderParamFields() {
    if (this._paramMeta.length === 0) {
      this._paramsEl.hidden = true;
      return;
    }

    this._paramsEl.hidden = false;
    this._paramsEl.innerHTML = '';
    for (const p of this._paramMeta) {
      const wrap = document.createElement('div');
      wrap.className = 'param-field';
      const label = document.createElement('label');
      label.textContent = p.prompt || p.name;
      const input = document.createElement('input');
      input.dataset.paramName = p.name;
      input.type = this._inputTypeFor(p.typeName);
      if (p.defaultValue !== null && p.defaultValue !== undefined) {
        input.value = p.defaultValue;
        this._paramValues[p.name] = p.defaultValue;
      }
      input.addEventListener('change', () => {
        this._paramValues[p.name] = input.type === 'checkbox' ? input.checked : input.value;
      });
      wrap.appendChild(label);
      wrap.appendChild(input);
      this._paramsEl.appendChild(wrap);
    }
  }

  _inputTypeFor(typeName) {
    switch (typeName) {
      case 'DateTime': return 'date';
      case 'Boolean': return 'checkbox';
      case 'Int32': case 'Int64': case 'Double': case 'Decimal': case 'Single': return 'number';
      default: return 'text';
    }
  }

  _collectParamValues() {
    const values = {};
    const inputs = this._paramsEl.querySelectorAll('input[data-param-name]');
    inputs.forEach((el) => {
      values[el.dataset.paramName] = el.type === 'checkbox' ? el.checked : el.value;
    });
    return values;
  }

  _export() {
    const renderEp = this.getAttribute('render-endpoint');
    const format = this._shadow.querySelector('[data-action="export-format"]').value;
    if (!renderEp) return;

    this._setStatus(`Exporting ${format.toUpperCase()}…`);
    const body = {
      name: this.getAttribute('report-name') || undefined,
      rdl: this.getAttribute('rdl') || undefined,
      parameters: this._collectParamValues(),
      format,
    };

    fetch(renderEp, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
      .then(async (r) => {
        if (!r.ok) throw new Error(await r.text());
        return r.blob();
      })
      .then((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.getAttribute('report-name') || 'report'}.${format}`;
        a.click();
        URL.revokeObjectURL(url);
        this._setStatus('');
      })
      .catch((err) => this._setStatus(`Export failed: ${err.message}`, true));
  }

  _print() {
    if (this._frame && this._frame.contentWindow) {
      this._frame.contentWindow.focus();
      this._frame.contentWindow.print();
    }
  }

  _setStatus(text, isError = false) {
    this._statusEl.textContent = text;
    this._statusEl.classList.toggle('error', isError);
  }

  _escape(s) {
    return String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }
}

customElements.define('report-viewer', ReportViewer);
