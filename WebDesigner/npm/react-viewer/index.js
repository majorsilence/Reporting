'use strict';

const React = require('react');

const DEFAULT_SCRIPT =
  '/_content/Majorsilence.Reporting.WebDesigner/rdl-viewer/rdl-viewer.js';

// Script loading — idempotent, deduped across multiple component instances.
const _pending = new Map(); // src → Promise

function _ensureScript(src) {
  if (!src || typeof document === 'undefined') return;
  if (_pending.has(src)) return;
  const existing = document.querySelector(`script[data-rdl-viewer="${src}"]`);
  if (existing) { _pending.set(src, Promise.resolve()); return; }
  const p = new Promise((resolve) => {
    const el = document.createElement('script');
    el.type = 'module';
    el.src = src;
    el.dataset.rdlViewer = src;
    el.onload = resolve;
    el.onerror = resolve; // don't block render on script error
    document.head.appendChild(el);
  });
  _pending.set(src, p);
}

/**
 * React wrapper around the <report-viewer> Web Component.
 *
 * Usage:
 *   import { ReportViewer } from '@majorsilence/report-viewer-react';
 *
 *   const ref = React.useRef();
 *   <ReportViewer
 *     ref={ref}
 *     renderEndpoint="/rdl-viewer/render"
 *     parametersEndpoint="/rdl-viewer/parameters"
 *     reportName="Invoice"
 *     style={{ display: 'block', height: '900px' }}
 *   />
 *   // later: ref.current.refresh()
 *
 * scriptSrc defaults to the ASP.NET Core static-files path emitted by the
 * Majorsilence.Reporting.WebDesigner NuGet package.  Override it if you serve
 * rdl-viewer.js from a different location.
 */
const ReportViewer = React.forwardRef(function ReportViewer(props, ref) {
  const {
    renderEndpoint,
    parametersEndpoint,
    reportName,
    rdl,
    scriptSrc,
    style,
    className,
    ...rest
  } = props;

  const elRef = React.useRef(null);
  const resolvedSrc = scriptSrc !== undefined ? scriptSrc : DEFAULT_SCRIPT;

  // Expose refresh() via the forwarded ref.
  React.useImperativeHandle(ref, () => ({
    refresh() {
      if (elRef.current) elRef.current.refresh();
    },
  }), []);

  // Load rdl-viewer.js on first mount.
  React.useEffect(() => {
    _ensureScript(resolvedSrc);
  }, [resolvedSrc]);

  // React 16-18: pass 'class' (not className) and kebab-case attributes for
  // custom elements.  React 19 handles this automatically.
  const elementProps = Object.assign({}, rest, {
    ref: elRef,
    'render-endpoint': renderEndpoint,
    'parameters-endpoint': parametersEndpoint != null ? parametersEndpoint : '',
    'report-name': reportName != null ? reportName : '',
    rdl: rdl != null ? rdl : '',
    style: style != null ? style : { display: 'block', height: '900px' },
  });
  if (className) elementProps['class'] = className;

  return React.createElement('report-viewer', elementProps);
});

ReportViewer.displayName = 'ReportViewer';

module.exports = { ReportViewer };
