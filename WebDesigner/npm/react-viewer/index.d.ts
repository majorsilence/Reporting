import * as React from 'react';

/** Public API exposed via a forwarded ref. */
export interface ReportViewerHandle {
  /** Re-render the report with its current parameter values. */
  refresh(): void;
}

export interface ReportViewerProps
  extends React.HTMLAttributes<HTMLElement> {
  /**
   * POST endpoint that renders a report to HTML, PDF, CSV, or XLSX.
   * @example "/rdl-viewer/render"
   */
  renderEndpoint: string;

  /**
   * GET endpoint (report name is appended) that returns report-parameter metadata.
   * Required for the auto-built parameter panel to appear.
   * @example "/rdl-viewer/parameters"
   */
  parametersEndpoint?: string;

  /** Name of a report on the server. Mutually exclusive with `rdl`. */
  reportName?: string;

  /** Inline RDL XML to render. Mutually exclusive with `reportName`. */
  rdl?: string;

  /**
   * URL of the rdl-viewer.js Web Component script to load.
   * Defaults to the ASP.NET Core static-files path:
   * `/_content/Majorsilence.Reporting.WebDesigner/rdl-viewer/rdl-viewer.js`
   *
   * Set to `null` if you have already loaded the script elsewhere.
   */
  scriptSrc?: string | null;
}

/**
 * React wrapper around the `<report-viewer>` Web Component.
 *
 * @example
 * ```tsx
 * const ref = React.useRef<ReportViewerHandle>(null);
 *
 * <ReportViewer
 *   ref={ref}
 *   renderEndpoint="/rdl-viewer/render"
 *   parametersEndpoint="/rdl-viewer/parameters"
 *   reportName="Invoice"
 *   style={{ display: 'block', height: '900px' }}
 * />
 *
 * // later
 * ref.current?.refresh();
 * ```
 */
export declare const ReportViewer: React.ForwardRefExoticComponent<
  ReportViewerProps & React.RefAttributes<ReportViewerHandle>
>;
