import {
  Component,
  ElementRef,
  Input,
  OnInit,
  ViewChild,
  CUSTOM_ELEMENTS_SCHEMA,
} from '@angular/core';

const DEFAULT_SCRIPT =
  '/_content/Majorsilence.Reporting.WebDesigner/rdl-viewer/rdl-viewer.js';

// Module-level dedup: only inject the script tag once per page.
const _loaded = new Set<string>();

function ensureScript(src: string): void {
  if (!src || typeof document === 'undefined') return;
  if (_loaded.has(src)) return;
  if (document.querySelector(`script[data-rdl-viewer="${src}"]`)) {
    _loaded.add(src);
    return;
  }
  const el = document.createElement('script');
  el.type = 'module';
  el.src = src;
  el.dataset['rdlViewer'] = src;
  document.head.appendChild(el);
  _loaded.add(src);
}

/** Minimal type for the <report-viewer> DOM element methods. */
interface ReportViewerElement extends HTMLElement {
  refresh(): void;
}

/**
 * Angular standalone wrapper around the `<report-viewer>` Web Component.
 *
 * Usage (Angular 14+):
 * ```ts
 * // app.module.ts  (or standalone component imports)
 * import { RdlViewerModule } from '@majorsilence/report-viewer-angular';
 * @NgModule({ imports: [RdlViewerModule], ... })
 * export class AppModule {}
 * ```
 *
 * Template:
 * ```html
 * <rdl-viewer #viewer
 *   renderEndpoint="/rdl-viewer/render"
 *   parametersEndpoint="/rdl-viewer/parameters"
 *   reportName="Invoice"
 *   style="display:block;height:900px">
 * </rdl-viewer>
 * ```
 *
 * Component class:
 * ```ts
 * @ViewChild('viewer') viewer!: RdlViewerComponent;
 *
 * refresh() { this.viewer.refresh(); }
 * ```
 */
@Component({
  selector: 'rdl-viewer',
  standalone: true,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  template: `<report-viewer #el
    [attr.render-endpoint]="renderEndpoint"
    [attr.parameters-endpoint]="parametersEndpoint ?? ''"
    [attr.report-name]="reportName ?? ''"
    [attr.rdl]="rdl ?? ''"
    style="display:block;width:100%;height:100%">
  </report-viewer>`,
  styles: [':host { display: block; }'],
})
export class RdlViewerComponent implements OnInit {
  /** POST endpoint that renders a report to HTML, PDF, CSV, or XLSX. */
  @Input() renderEndpoint = '/rdl-viewer/render';

  /** GET endpoint (report name is appended) that returns report-parameter metadata. */
  @Input() parametersEndpoint?: string;

  /** Name of a report on the server. Mutually exclusive with `rdl`. */
  @Input() reportName?: string;

  /** Inline RDL XML to render. Mutually exclusive with `reportName`. */
  @Input() rdl?: string;

  /**
   * URL of rdl-viewer.js.  Defaults to the ASP.NET Core static-files path.
   * Set to empty string to skip automatic script loading.
   */
  @Input() scriptSrc = DEFAULT_SCRIPT;

  @ViewChild('el') private _el!: ElementRef<ReportViewerElement>;

  ngOnInit(): void {
    if (this.scriptSrc) ensureScript(this.scriptSrc);
  }

  /** Re-render the report with its current parameter values. */
  refresh(): void {
    this._el?.nativeElement?.refresh();
  }
}
