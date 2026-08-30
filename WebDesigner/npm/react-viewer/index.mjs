import {
  forwardRef,
  useRef,
  useImperativeHandle,
  useEffect,
  createElement,
} from 'react';

const DEFAULT_SCRIPT =
  '/_content/Majorsilence.Reporting.WebDesigner/rdl-viewer/rdl-viewer.js';

const _pending = new Map();

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
    el.onerror = resolve;
    document.head.appendChild(el);
  });
  _pending.set(src, p);
}

export const ReportViewer = forwardRef(function ReportViewer(props, ref) {
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

  const elRef = useRef(null);
  const resolvedSrc = scriptSrc !== undefined ? scriptSrc : DEFAULT_SCRIPT;

  useImperativeHandle(ref, () => ({
    refresh() {
      if (elRef.current) elRef.current.refresh();
    },
  }), []);

  useEffect(() => {
    _ensureScript(resolvedSrc);
  }, [resolvedSrc]);

  const elementProps = Object.assign({}, rest, {
    ref: elRef,
    'render-endpoint': renderEndpoint,
    'parameters-endpoint': parametersEndpoint != null ? parametersEndpoint : '',
    'report-name': reportName != null ? reportName : '',
    rdl: rdl != null ? rdl : '',
    style: style != null ? style : { display: 'block', height: '900px' },
  });
  if (className) elementProps['class'] = className;

  return createElement('report-viewer', elementProps);
});

ReportViewer.displayName = 'ReportViewer';
