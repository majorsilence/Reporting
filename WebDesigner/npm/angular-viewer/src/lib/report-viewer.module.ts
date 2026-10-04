import { NgModule } from '@angular/core';
import { RdlViewerComponent } from './report-viewer.component';

/**
 * Import this module to use <rdl-viewer> in your Angular application.
 *
 * ```ts
 * import { RdlViewerModule } from '@majorsilence/report-viewer-angular';
 *
 * @NgModule({ imports: [RdlViewerModule] })
 * export class AppModule {}
 * ```
 */
@NgModule({
  imports: [RdlViewerComponent],
  exports: [RdlViewerComponent],
})
export class RdlViewerModule {}
