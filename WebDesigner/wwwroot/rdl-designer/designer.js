// designer.js — Majorsilence Report Designer  (Phase 1–4, vanilla JS, no build step)
// Usage:  <script type="module" src="/_content/Majorsilence.Reporting.WebDesigner/rdl-designer/designer.js"></script>
//         <report-designer preview-endpoint="/rdl-designer/preview"
//                          save-endpoint="/rdl-designer/save"
//                          load-endpoint="/rdl-designer/load"
//                          style="height:700px;display:block"></report-designer>

'use strict';

const DPI = 96; // logical inches → screen pixels at 100 % zoom

// Providers that support schema discovery via the /schema endpoint.
const FILE_PROVIDERS = new Set(['json', 'text', 'xml', 'filedirectory']);

// ── Helpers ───────────────────────────────────────────────────────────────────

function esc(s) {
  return String(s)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;')
    .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function escHtml(s) { return esc(s); }

function toHex(color) {
  if (!color || color === 'transparent' || color === '') return '#ffffff';
  if (/^#[0-9a-f]{6}$/i.test(color)) return color;
  const ctx = document.createElement('canvas').getContext('2d');
  ctx.fillStyle = color;
  return ctx.fillStyle;
}

function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }
function snap(v, grid = 0.05) { return Math.round(v / grid) * grid; }

// ── Layout item model ─────────────────────────────────────────────────────────

class RdlStyle {
  constructor() {
    this.fontFamily    = 'Arial';
    this.fontSize      = '10pt';
    this.fontWeight    = 'Normal';
    this.fontStyle     = 'Normal';
    this.color         = 'Black';
    this.backgroundColor = '';
    this.textAlign     = 'Left';
    this.verticalAlign = 'Top';
    this.borderStyle   = 'None';
    this.borderColor   = 'Black';
    this.borderWidth   = '1pt';
  }
}

class RdlItem {
  constructor(type, name) {
    this.type   = type;
    this.name   = name;
    this.top    = 0.5;
    this.left   = 0.5;
    this.width  = 2.0;
    this.height = 0.25;
    this.style  = new RdlStyle();
  }
}

class RdlTextBox extends RdlItem {
  constructor(name) {
    super('Textbox', name);
    this.value   = 'Text';
    this.canGrow = true;
  }
}

class RdlRectangle extends RdlItem {
  constructor(name) {
    super('Rectangle', name);
    this.width  = 2.0;
    this.height = 1.0;
    this.style.borderStyle = 'Solid';
  }
}

class RdlLine extends RdlItem {
  constructor(name) {
    super('Line', name);
    this.width  = 2.0;
    this.height = 0.01;
    this.style.borderStyle = 'Solid';
  }
}

class RdlImage extends RdlItem {
  constructor(name) {
    super('Image', name);
    this.source = 'External';
    this.value  = '';
    this.width  = 1.5;
    this.height = 1.5;
  }
}

class RdlChart extends RdlItem {
  constructor(name) {
    super('Chart', name);
    this.width         = 3.0;
    this.height        = 2.5;
    this.chartType     = 'Column';
    this.chartSubtype  = 'Plain';
    this.dataSetName   = '';
    this.title         = '';
    this.categoryField = '';
    this.seriesField   = '';
    this.valueExpr     = '=Count(Fields!Value.Value)';
    this.noRows        = 'Query returned no rows!';
    this.palette       = 'Default';
    this.style.borderStyle = 'Solid';
    this.style.backgroundColor = 'White';
  }
}

class RdlTable extends RdlItem {
  constructor(name) {
    super('Table', name);
    this.dataSetName = '';
    this.noRows      = 'Query returned no rows!';
    this.columns     = []; // [{header, fieldExpr, width}]
    this.height      = 0.6;
    this.style.borderStyle = 'Solid';
  }
}

// ── Data model ────────────────────────────────────────────────────────────────

class RdlField {
  constructor(name, dataField = '') {
    this.name      = name;
    this.dataField = dataField || name;
    this.typeName  = 'System.String';
  }
}

class RdlDataSet {
  constructor(name) {
    this.name           = name;
    this.dataSourceName = '';
    this.commandType    = 'Text';
    this.commandText    = '';
    this.fields         = []; // RdlField[]
  }
}

class RdlDataSource {
  constructor(name) {
    this.name         = name;
    this.dataProvider = 'SQL';
    this.connectString = '';
  }
}

// ── Report root ───────────────────────────────────────────────────────────────

class RdlReport {
  constructor() {
    this.pageWidth    = 8.5;
    this.pageHeight   = 11.0;
    this.topMargin    = 1.0;
    this.bottomMargin = 1.0;
    this.leftMargin   = 1.0;
    this.rightMargin  = 1.0;
    this.items        = [];
    this.dataSources  = []; // RdlDataSource[]
    this.dataSets     = []; // RdlDataSet[]
    this._counter     = 0;
  }

  get bodyWidth()  { return this.pageWidth  - this.leftMargin - this.rightMargin; }
  get bodyHeight() { return this.pageHeight - this.topMargin  - this.bottomMargin; }

  _nextName(prefix) { return `${prefix}${++this._counter}`; }

  createItem(type) {
    let item;
    switch (type) {
      case 'Textbox':   item = new RdlTextBox(this._nextName('TextBox'));     break;
      case 'Rectangle': item = new RdlRectangle(this._nextName('Rectangle')); break;
      case 'Line':      item = new RdlLine(this._nextName('Line'));           break;
      case 'Image':     item = new RdlImage(this._nextName('Image'));         break;
      case 'Chart':     item = new RdlChart(this._nextName('Chart'));         break;
      case 'Table':     item = new RdlTable(this._nextName('Table'));         break;
      default:          item = new RdlItem(type, this._nextName(type));       break;
    }
    this.items.push(item);
    return item;
  }

  remove(item) { this.items = this.items.filter(i => i !== item); }
}

// ── RDL Serializer ────────────────────────────────────────────────────────────

const NS    = 'http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition';
const NS_RD = 'http://schemas.microsoft.com/SQLServer/reporting/reportdesigner';

const RdlSerializer = {
  in4(v) { return typeof v === 'number' ? v.toFixed(4) + 'in' : String(v); },

  toXml(r) {
    const n = v => this.in4(v);
    const lines = [
      `<?xml version="1.0" encoding="UTF-8"?>`,
      `<Report xmlns="${NS}" xmlns:rd="${NS_RD}">`,
      `  <Width>${n(r.pageWidth)}</Width>`,
      `  <PageWidth>${n(r.pageWidth)}</PageWidth>`,
      `  <PageHeight>${n(r.pageHeight)}</PageHeight>`,
      `  <TopMargin>${n(r.topMargin)}</TopMargin>`,
      `  <BottomMargin>${n(r.bottomMargin)}</BottomMargin>`,
      `  <LeftMargin>${n(r.leftMargin)}</LeftMargin>`,
      `  <RightMargin>${n(r.rightMargin)}</RightMargin>`,
    ];

    // DataSources
    if (r.dataSources.length > 0) {
      lines.push(`  <DataSources>`);
      for (const ds of r.dataSources) {
        lines.push(
          `    <DataSource Name="${esc(ds.name)}">`,
          `      <ConnectionProperties>`,
          `        <DataProvider>${esc(ds.dataProvider)}</DataProvider>`,
          `        <ConnectString>${esc(ds.connectString)}</ConnectString>`,
          `      </ConnectionProperties>`,
          `    </DataSource>`,
        );
      }
      lines.push(`  </DataSources>`);
    }

    // DataSets
    if (r.dataSets.length > 0) {
      lines.push(`  <DataSets>`);
      for (const ds of r.dataSets) {
        // Resolve data provider to skip CommandType for providers that ignore it
        const srcForDs2 = r.dataSources.find(d => d.name === ds.dataSourceName);
        const isFileProv = srcForDs2 && FILE_PROVIDERS.has(srcForDs2.dataProvider.toLowerCase());
        lines.push(
          `    <DataSet Name="${esc(ds.name)}">`,
          `      <Query>`,
          `        <DataSourceName>${esc(ds.dataSourceName)}</DataSourceName>`,
        );
        if (!isFileProv)
          lines.push(`        <CommandType>${esc(ds.commandType)}</CommandType>`);
        lines.push(
          `        <CommandText>${esc(ds.commandText)}</CommandText>`,
          `      </Query>`,
        );
        if (ds.fields.length > 0) {
          lines.push(`      <Fields>`);
          for (const f of ds.fields) {
            lines.push(
              `        <Field Name="${esc(f.name)}">`,
              `          <DataField>${esc(f.dataField)}</DataField>`,
              `          <rd:TypeName>${esc(f.typeName)}</rd:TypeName>`,
              `        </Field>`,
            );
          }
          lines.push(`      </Fields>`);
        }
        lines.push(`    </DataSet>`);
      }
      lines.push(`  </DataSets>`);
    }

    // Body
    lines.push(`  <Body>`);
    let bodyH = r.bodyHeight;
    for (const it of r.items) {
      const bottom = it.top + it.height + 0.25;
      if (bottom > bodyH) bodyH = bottom;
    }
    lines.push(`    <Height>${n(bodyH)}</Height>`);

    if (r.items.length > 0) {
      lines.push(`    <ReportItems>`);
      for (const it of r.items) {
        lines.push(`      <${it.type} Name="${esc(it.name)}">`);
        lines.push(
          `        <Top>${n(it.top)}</Top>`,
          `        <Left>${n(it.left)}</Left>`,
          `        <Height>${n(Math.max(0.005, it.height))}</Height>`,
          `        <Width>${n(it.width)}</Width>`,
        );
        if (it.type === 'Textbox') {
          lines.push(`        <Value>${esc(it.value)}</Value>`);
          if (it.canGrow) lines.push(`        <CanGrow>true</CanGrow>`);
        }
        if (it.type === 'Image') {
          lines.push(
            `        <Source>${esc(it.source)}</Source>`,
            `        <Value>${esc(it.value)}</Value>`,
          );
        }
        if (it.type === 'Table') {
          if (it.dataSetName) lines.push(`        <DataSetName>${esc(it.dataSetName)}</DataSetName>`);
          if (it.noRows)      lines.push(`        <NoRows>${esc(it.noRows)}</NoRows>`);
          lines.push(`        <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>`);
          const colW = it.columns.length > 0 ? (it.width / it.columns.length) : 1.5;
          lines.push(`        <TableColumns>`);
          for (const col of it.columns)
            lines.push(`          <TableColumn><Width>${n(col.width || colW)}</Width></TableColumn>`);
          lines.push(`        </TableColumns>`);
          // Header row
          lines.push(
            `        <Header><TableRows><TableRow><Height>0.25in</Height><TableCells>`,
          );
          for (let i = 0; i < it.columns.length; i++) {
            const hname = esc(`${it.name}_H${i}`);
            lines.push(
              `          <TableCell><ReportItems><Textbox Name="${hname}">`,
              `            <Value>${esc(it.columns[i].header)}</Value>`,
              `            <Style><FontWeight>Bold</FontWeight><TextAlign>Center</TextAlign>`,
              `              <BorderStyle><Default>Solid</Default></BorderStyle></Style>`,
              `          </Textbox></ReportItems></TableCell>`,
            );
          }
          lines.push(`        </TableCells></TableRow></TableRows></Header>`);
          // Details row
          lines.push(
            `        <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>`,
          );
          for (let i = 0; i < it.columns.length; i++) {
            const dname = esc(`${it.name}_D${i}`);
            lines.push(
              `          <TableCell><ReportItems><Textbox Name="${dname}">`,
              `            <Value>${esc(it.columns[i].fieldExpr)}</Value>`,
              `            <CanGrow>true</CanGrow>`,
              `            <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>`,
              `          </Textbox></ReportItems></TableCell>`,
            );
          }
          lines.push(`        </TableCells></TableRow></TableRows></Details>`);
          lines.push(`      </Table>`);
          continue;
        }
        if (it.type === 'Chart') {
          if (it.dataSetName) lines.push(`        <DataSetName>${esc(it.dataSetName)}</DataSetName>`);
          if (it.noRows)      lines.push(`        <NoRows>${esc(it.noRows)}</NoRows>`);
          const cs = it.style;
          const csl = [];
          if (cs.borderStyle && cs.borderStyle !== 'None')
            csl.push(`          <BorderStyle><Default>${cs.borderStyle}</Default></BorderStyle>`);
          if (cs.backgroundColor)
            csl.push(`          <BackgroundColor>${esc(cs.backgroundColor)}</BackgroundColor>`);
          if (csl.length) lines.push(`        <Style>`, ...csl, `        </Style>`);
          lines.push(
            `        <Type>${esc(it.chartType || 'Column')}</Type>`,
            `        <Subtype>${esc(it.chartSubtype || 'Plain')}</Subtype>`,
            `        <Palette>${esc(it.palette || 'Default')}</Palette>`,
          );
          if (it.categoryField) {
            lines.push(
              `        <CategoryGroupings>`,
              `          <CategoryGrouping>`,
              `            <DynamicCategories>`,
              `              <Grouping Name="DynamicCategoriesGroup1">`,
              `                <GroupExpressions>`,
              `                  <GroupExpression>${esc(it.categoryField)}</GroupExpression>`,
              `                </GroupExpressions>`,
              `              </Grouping>`,
              `            </DynamicCategories>`,
              `          </CategoryGrouping>`,
              `        </CategoryGroupings>`,
            );
          }
          if (it.seriesField) {
            lines.push(
              `        <SeriesGroupings>`,
              `          <SeriesGrouping>`,
              `            <DynamicSeries>`,
              `              <Grouping Name="DynamicSeriesGroup1">`,
              `                <GroupExpressions>`,
              `                  <GroupExpression>${esc(it.seriesField)}</GroupExpression>`,
              `                </GroupExpressions>`,
              `              </Grouping>`,
              `              <Label>${esc(it.seriesField)}</Label>`,
              `            </DynamicSeries>`,
              `          </SeriesGrouping>`,
              `        </SeriesGroupings>`,
            );
          }
          lines.push(
            `        <ChartData>`,
            `          <ChartSeries>`,
            `            <DataPoints>`,
            `              <DataPoint>`,
            `                <DataValues>`,
            `                  <DataValue>`,
            `                    <Value>${esc(it.valueExpr || '=Count(Fields!Value.Value)')}</Value>`,
            `                  </DataValue>`,
            `                </DataValues>`,
            `                <DataLabel><Visible>False</Visible></DataLabel>`,
            `              </DataPoint>`,
            `            </DataPoints>`,
            `          </ChartSeries>`,
            `        </ChartData>`,
            `        <Legend><Visible>true</Visible></Legend>`,
          );
          if (it.title)
            lines.push(`        <Title><Caption>${esc(it.title)}</Caption></Title>`);
          lines.push(`      </Chart>`);
          continue;
        }
        const s  = it.style;
        const sl = [];
        if (s.fontFamily) sl.push(`          <FontFamily>${esc(s.fontFamily)}</FontFamily>`);
        if (s.fontSize)   sl.push(`          <FontSize>${esc(s.fontSize)}</FontSize>`);
        if (s.fontWeight && s.fontWeight !== 'Normal') sl.push(`          <FontWeight>${s.fontWeight}</FontWeight>`);
        if (s.fontStyle  && s.fontStyle  !== 'Normal') sl.push(`          <FontStyle>${s.fontStyle}</FontStyle>`);
        if (s.color && s.color !== 'Black') sl.push(`          <Color>${esc(s.color)}</Color>`);
        if (s.backgroundColor) sl.push(`          <BackgroundColor>${esc(s.backgroundColor)}</BackgroundColor>`);
        if (s.textAlign && s.textAlign !== 'Left') sl.push(`          <TextAlign>${s.textAlign}</TextAlign>`);
        if (s.borderStyle && s.borderStyle !== 'None') {
          sl.push(`          <BorderStyle><Default>${s.borderStyle}</Default></BorderStyle>`);
          sl.push(`          <BorderColor><Default>${esc(s.borderColor || 'Black')}</Default></BorderColor>`);
          sl.push(`          <BorderWidth><Default>${esc(s.borderWidth || '1pt')}</Default></BorderWidth>`);
        }
        if (sl.length > 0) lines.push(`        <Style>`, ...sl, `        </Style>`);
        lines.push(`      </${it.type}>`);
      }
      lines.push(`    </ReportItems>`);
    }

    lines.push(`  </Body>`, `</Report>`);
    return lines.join('\n');
  },

  fromXml(xml) {
    const doc = new DOMParser().parseFromString(xml, 'text/xml');
    if (doc.querySelector('parsererror'))
      throw new Error('Invalid RDL XML: ' + doc.querySelector('parsererror').textContent.slice(0, 120));

    const r    = new RdlReport();
    const root = doc.documentElement;

    const inch = (el, tag, def) => {
      const c = el.querySelector(tag);
      if (!c) return def;
      const f = parseFloat(c.textContent);
      return isNaN(f) ? def : f;
    };
    const text = (el, tag, def = '') => {
      const c = el.querySelector(tag);
      return c ? c.textContent.trim() : def;
    };

    r.pageWidth    = inch(root, 'PageWidth',    8.5);
    r.pageHeight   = inch(root, 'PageHeight',   11.0);
    r.topMargin    = inch(root, 'TopMargin',     1.0);
    r.bottomMargin = inch(root, 'BottomMargin',  1.0);
    r.leftMargin   = inch(root, 'LeftMargin',    1.0);
    r.rightMargin  = inch(root, 'RightMargin',   1.0);

    // DataSources
    for (const dsEl of root.querySelectorAll('DataSources > DataSource')) {
      const ds    = new RdlDataSource(dsEl.getAttribute('Name') || 'DataSource');
      const cp    = dsEl.querySelector('ConnectionProperties');
      if (cp) {
        ds.dataProvider  = text(cp, 'DataProvider', 'SQL');
        ds.connectString = text(cp, 'ConnectString', '');
      }
      r.dataSources.push(ds);
    }

    // DataSets
    for (const dsEl of root.querySelectorAll('DataSets > DataSet')) {
      const ds = new RdlDataSet(dsEl.getAttribute('Name') || 'DataSet');
      const q  = dsEl.querySelector('Query');
      if (q) {
        ds.dataSourceName = text(q, 'DataSourceName', '');
        ds.commandType    = text(q, 'CommandType', 'Text');
        ds.commandText    = text(q, 'CommandText', '');
      }
      for (const fEl of dsEl.querySelectorAll('Fields > Field')) {
        const f = new RdlField(fEl.getAttribute('Name') || 'Field');
        f.dataField = text(fEl, 'DataField', f.name);
        // Handle both <TypeName> and <rd:TypeName> (Microsoft designer format)
        const tnEl = fEl.querySelector('TypeName') || [...fEl.children].find(c => c.localName === 'TypeName');
        f.typeName = tnEl ? tnEl.textContent.trim() : 'System.String';
        ds.fields.push(f);
      }
      r.dataSets.push(ds);
    }

    // Body items
    const body = root.querySelector('Body');
    if (body) {
      for (const el of body.querySelectorAll('ReportItems > *')) {
        const type = el.tagName;
        const name = el.getAttribute('Name') || (type + (++r._counter));
        let item;
        switch (type) {
          case 'Textbox':
            item = new RdlTextBox(name);
            item.value   = text(el, 'Value', '');
            item.canGrow = text(el, 'CanGrow', 'true').toLowerCase() === 'true';
            break;
          case 'Rectangle': item = new RdlRectangle(name); break;
          case 'Line':      item = new RdlLine(name);      break;
          case 'Image':
            item = new RdlImage(name);
            item.source = text(el, 'Source', 'External');
            item.value  = text(el, 'Value',  '');
            break;
          case 'Chart': {
            item = new RdlChart(name);
            item.chartType     = text(el, 'Type',        'Column');
            item.chartSubtype  = text(el, 'Subtype',     'Plain');
            item.dataSetName   = text(el, 'DataSetName', '');
            item.noRows        = text(el, 'NoRows',      '');
            item.palette       = text(el, 'Palette',     'Default');
            const titleEl = el.querySelector('Title');
            item.title = titleEl ? (titleEl.querySelector('Caption')?.textContent.trim() || '') : '';
            const catExpr = el.querySelector('CategoryGroupings GroupExpression');
            if (catExpr) item.categoryField = catExpr.textContent.trim();
            const serExpr = el.querySelector('SeriesGroupings GroupExpression');
            if (serExpr) item.seriesField = serExpr.textContent.trim();
            const valExpr = el.querySelector('ChartData DataValue Value');
            if (valExpr) item.valueExpr = valExpr.textContent.trim();
            const bgEl = el.querySelector('Style BackgroundColor');
            if (bgEl) item.style.backgroundColor = bgEl.textContent.trim();
            const bsEl = el.querySelector('Style BorderStyle Default');
            if (bsEl) item.style.borderStyle = bsEl.textContent.trim();
            break;
          }
          case 'Table': {
            item = new RdlTable(name);
            item.dataSetName = text(el, 'DataSetName', '');
            item.noRows      = text(el, 'NoRows', 'Query returned no rows!');
            // Read columns from TableColumns + Header + Details
            const colEls  = [...el.querySelectorAll('TableColumns > TableColumn')];
            const hdrCells = [...el.querySelectorAll('Header TableCell')];
            const detCells = [...el.querySelectorAll('Details TableCell')];
            const count = colEls.length || Math.max(hdrCells.length, detCells.length);
            item.columns = [];
            for (let i = 0; i < count; i++) {
              const colEl = colEls[i];
              const w = colEl ? inch(colEl, 'Width', 1.5) : 1.5;
              const header    = hdrCells[i] ? (hdrCells[i].querySelector('Textbox Value')?.textContent.trim() || '') : '';
              const fieldExpr = detCells[i] ? (detCells[i].querySelector('Textbox Value')?.textContent.trim() || '') : '';
              item.columns.push({ header, fieldExpr, width: w });
            }
            if (item.columns.length > 0)
              item.width = item.columns.reduce((s, c) => s + (c.width || 1.5), 0);
            break;
          }
          default: continue;
        }
        item.top    = inch(el, 'Top',    0.5);
        item.left   = inch(el, 'Left',   0.5);
        // For Table: prefer column-sum width; only override if XML has an explicit <Width> child
        if (!(item.type === 'Table' && item.width > 0 && !el.querySelector(':scope > Width')))
          item.width = inch(el, 'Width', 2.0);
        item.height = inch(el, 'Height', 0.25);

        const s = el.querySelector('Style');
        if (s) {
          item.style.fontFamily      = text(s, 'FontFamily',    'Arial');
          item.style.fontSize        = text(s, 'FontSize',      '10pt');
          item.style.fontWeight      = text(s, 'FontWeight',    'Normal');
          item.style.fontStyle       = text(s, 'FontStyle',     'Normal');
          item.style.color           = text(s, 'Color',         'Black');
          item.style.backgroundColor = text(s, 'BackgroundColor', '');
          item.style.textAlign       = text(s, 'TextAlign',     'Left');
          const bs = s.querySelector('BorderStyle Default');
          if (bs) item.style.borderStyle = bs.textContent.trim();
          const bc = s.querySelector('BorderColor Default');
          if (bc) item.style.borderColor = bc.textContent.trim();
          const bw = s.querySelector('BorderWidth Default');
          if (bw) item.style.borderWidth = bw.textContent.trim();
        }

        r.items.push(item);
      }
      r._counter = Math.max(r._counter, r.items.length);
    }

    return r;
  },
};

// ── Shadow DOM template ───────────────────────────────────────────────────────

const SHADOW_HTML = /* html */`
<style>
  :host {
    display: flex; flex-direction: column;
    font-family: system-ui, -apple-system, sans-serif;
    font-size: 13px;
    background: #f0f0f0;
    overflow: hidden;
    position: relative;
    box-sizing: border-box;
    user-select: none;
  }
  * { box-sizing: border-box; }

  /* ── Toolbar ── */
  #toolbar {
    display: flex; align-items: center; gap: 6px;
    padding: 5px 10px;
    background: #1e1e1e; color: #ccc; flex-shrink: 0; flex-wrap: wrap;
  }
  #toolbar span.label { font-size: 11px; color: #888; }
  #toolbar .sep { width: 1px; height: 20px; background: #444; margin: 0 2px; flex-shrink: 0; }
  button {
    padding: 4px 10px; border: 1px solid #555;
    background: #3a3a3a; color: #ddd; border-radius: 3px;
    cursor: pointer; font-size: 12px; white-space: nowrap;
  }
  button:hover { background: #555; color: #fff; }
  button:disabled { opacity: 0.4; cursor: default; }
  select#zoom-sel {
    font-size: 12px; padding: 3px 4px;
    background: #3a3a3a; color: #ddd;
    border: 1px solid #555; border-radius: 3px; cursor: pointer;
  }

  /* ── Main ── */
  #main { display: flex; flex: 1; overflow: hidden; }

  /* ── Toolbox ── */
  #toolbox {
    width: 108px; flex-shrink: 0;
    background: #fff; border-right: 1px solid #ddd;
    display: flex; flex-direction: column;
    padding: 8px 5px; gap: 4px; overflow-y: auto;
  }
  .tool-heading {
    font-size: 10px; font-weight: 700; color: #999;
    text-transform: uppercase; letter-spacing: 0.5px;
    padding: 2px 3px 4px;
  }
  .tool-item {
    display: flex; align-items: center; gap: 6px;
    padding: 5px 7px; border: 1px solid #e0e0e0; border-radius: 3px;
    background: #fafafa; cursor: grab; font-size: 12px; white-space: nowrap;
  }
  .tool-item:hover { background: #e8f0fe; border-color: #4285f4; color: #1a73e8; }
  .tool-icon { font-size: 13px; line-height: 1; }

  /* ── Canvas area ── */
  #canvas-area {
    flex: 1; overflow: auto; background: #6d6d6d;
    padding: 24px; display: flex; align-items: flex-start; justify-content: center;
  }

  /* ── Page ── */
  #page {
    position: relative; background: #fff;
    box-shadow: 0 3px 12px rgba(0,0,0,0.5);
    flex-shrink: 0; overflow: visible;
  }
  #page.drag-over { outline: 3px dashed #4285f4; }

  /* ── Report items ── */
  .r-item { position: absolute; cursor: move; overflow: hidden; }
  .r-item:hover::after {
    content: ''; position: absolute; inset: 0;
    outline: 1px dashed #4285f4; pointer-events: none;
  }
  .r-item.selected::after {
    content: ''; position: absolute; inset: 0;
    outline: 2px solid #4285f4; pointer-events: none;
  }
  .r-item .item-inner { position: absolute; inset: 0; overflow: hidden; pointer-events: none; display: flex; }

  /* resize handles */
  .r-handle {
    position: absolute; width: 8px; height: 8px;
    background: #fff; border: 1.5px solid #4285f4; border-radius: 1px; z-index: 10;
  }
  .r-handle[data-dir=nw]{ top:-4px; left:-4px; cursor:nw-resize; }
  .r-handle[data-dir=n] { top:-4px; left:calc(50% - 4px); cursor:n-resize; }
  .r-handle[data-dir=ne]{ top:-4px; right:-4px; cursor:ne-resize; }
  .r-handle[data-dir=e] { top:calc(50% - 4px); right:-4px; cursor:e-resize; }
  .r-handle[data-dir=se]{ bottom:-4px; right:-4px; cursor:se-resize; }
  .r-handle[data-dir=s] { bottom:-4px; left:calc(50% - 4px); cursor:s-resize; }
  .r-handle[data-dir=sw]{ bottom:-4px; left:-4px; cursor:sw-resize; }
  .r-handle[data-dir=w] { top:calc(50% - 4px); left:-4px; cursor:w-resize; }

  /* ── Right panel (Properties + Data tabs) ── */
  #right-panel {
    width: 240px; flex-shrink: 0;
    background: #fff; border-left: 1px solid #ddd;
    display: flex; flex-direction: column; overflow: hidden;
  }

  /* Tab strip */
  #panel-tabs { display: flex; border-bottom: 1px solid #ddd; flex-shrink: 0; }
  .tab-btn {
    flex: 1; padding: 7px 4px;
    border: none; border-bottom: 2px solid transparent;
    background: #f5f5f5; cursor: pointer;
    font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.4px;
    color: #999;
  }
  .tab-btn:hover { background: #eee; color: #555; }
  .tab-btn.active { background: #fff; color: #333; border-bottom-color: #4285f4; }

  /* Tab panes */
  .tab-pane { flex: 1; overflow-y: auto; display: none; flex-direction: column; }
  .tab-pane.active { display: flex; }

  /* Properties pane */
  #no-sel { padding: 14px 10px; color: #aaa; font-size: 12px; font-style: italic; }
  .pgroup { padding: 6px 8px; border-bottom: 1px solid #eee; }
  .pgroup-title {
    font-size: 10px; font-weight: 700; color: #aaa;
    text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 5px;
  }
  .prow { display: flex; align-items: center; gap: 4px; margin-bottom: 4px; }
  .prow label {
    width: 84px; font-size: 11px; color: #555; flex-shrink: 0;
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  }
  .prow input[type=text], .prow input[type=number], .prow select, .prow textarea {
    flex: 1; min-width: 0; font-size: 11px; padding: 2px 4px;
    border: 1px solid #ccc; border-radius: 2px; background: #fff; color: #333;
  }
  .prow input[type=color] {
    flex: 1; height: 22px; padding: 0 2px; cursor: pointer;
    border: 1px solid #ccc; border-radius: 2px;
  }
  .prow textarea { resize: vertical; min-height: 38px; }

  /* Data pane */
  .data-section { padding: 6px 8px; border-bottom: 1px solid #eee; }
  .data-section-head {
    display: flex; align-items: center; justify-content: space-between;
    margin-bottom: 5px;
  }
  .data-section-title {
    font-size: 10px; font-weight: 700; color: #888;
    text-transform: uppercase; letter-spacing: 0.5px;
  }
  .data-add-btn {
    border: 1px solid #ccc; background: #fafafa; color: #555;
    padding: 1px 6px; font-size: 11px; border-radius: 2px; cursor: pointer;
  }
  .data-add-btn:hover { background: #e8f0fe; border-color: #4285f4; color: #1a73e8; }
  .data-row {
    display: flex; align-items: center; justify-content: space-between;
    padding: 3px 4px; border-radius: 2px; font-size: 11px; color: #333;
    cursor: pointer;
  }
  .data-row:hover { background: #f0f4ff; }
  .data-row.selected { background: #e8f0fe; }
  .data-row-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; flex: 1; }
  .data-row-del {
    border: none; background: none; color: #bbb; cursor: pointer;
    font-size: 12px; padding: 0 2px; line-height: 1; flex-shrink: 0;
  }
  .data-row-del:hover { color: #c00; }
  .field-row {
    display: flex; align-items: center; justify-content: space-between;
    padding: 2px 4px 2px 12px; border-radius: 2px; font-size: 11px; color: #555;
    cursor: grab;
  }
  .field-row:hover { background: #f0f4ff; color: #1a73e8; }
  .field-icon { margin-right: 4px; font-size: 10px; color: #aaa; }
  #data-empty { padding: 12px 10px; font-size: 11px; color: #aaa; font-style: italic; }

  /* ── Table column reorder list ── */
  .tbl-col-row {
    display: flex; align-items: center; gap: 2px;
    padding: 2px 0; border-bottom: 1px solid #f0f0f0;
  }
  .tbl-col-name {
    flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    font-size: 11px; color: #333;
  }
  .tbl-col-btn {
    border: 1px solid #ddd; background: #f5f5f5; color: #666;
    padding: 1px 5px; font-size: 10px; border-radius: 2px; cursor: pointer;
    line-height: 1.4; flex-shrink: 0;
  }
  .tbl-col-btn:hover:not(:disabled) { background: #e8f0fe; border-color: #4285f4; color: #1a73e8; }
  .tbl-col-btn:disabled { opacity: 0.3; cursor: default; }

  /* ── Dialog ── */
  #dlg-overlay {
    display: none; position: absolute; inset: 0;
    background: rgba(0,0,0,0.45); z-index: 200;
    align-items: center; justify-content: center;
  }
  #dlg-overlay.open { display: flex; }
  #dlg-box {
    background: #fff; border-radius: 5px;
    box-shadow: 0 4px 24px rgba(0,0,0,0.3);
    min-width: 340px; max-width: 480px;
    display: flex; flex-direction: column; overflow: hidden;
  }
  #dlg-title { padding: 12px 14px; font-weight: 600; background: #f5f5f5; border-bottom: 1px solid #ddd; }
  #dlg-body  { padding: 14px; display: flex; flex-direction: column; gap: 8px; }
  #dlg-body label { font-size: 12px; color: #555; display: flex; flex-direction: column; gap: 3px; }
  #dlg-body input, #dlg-body select, #dlg-body textarea {
    font-size: 12px; padding: 5px 7px;
    border: 1px solid #ccc; border-radius: 3px; width: 100%;
  }
  #dlg-body textarea { resize: vertical; min-height: 60px; }
  #dlg-footer {
    display: flex; justify-content: flex-end; gap: 8px;
    padding: 10px 14px; border-top: 1px solid #eee; background: #fafafa;
  }
  #dlg-footer button { padding: 5px 14px; font-size: 13px; }

  /* ── Preview overlay ── */
  #preview-overlay {
    display: none; position: absolute; inset: 0;
    background: rgba(0,0,0,0.55); z-index: 100;
    align-items: center; justify-content: center;
  }
  #preview-overlay.open { display: flex; }
  #preview-box {
    background: #fff; border-radius: 4px; width: 90%; height: 90%;
    display: flex; flex-direction: column; overflow: hidden;
    box-shadow: 0 4px 24px rgba(0,0,0,0.4);
  }
  #preview-bar {
    display: flex; align-items: center; padding: 8px 12px;
    background: #1e1e1e; color: #ccc; gap: 10px; flex-shrink: 0;
  }
  #preview-bar span { flex: 1; font-weight: 600; }
  #preview-iframe { flex: 1; border: none; }

  /* ── Status bar ── */
  #statusbar {
    padding: 2px 10px; background: #1e1e1e; color: #888;
    font-size: 11px; flex-shrink: 0; display: flex; gap: 16px;
  }
</style>

<div id="toolbar">
  <button id="btn-new">New</button>
  <button id="btn-load">Load…</button>
  <button id="btn-save">Save…</button>
  <div class="sep"></div>
  <button id="btn-preview">Preview</button>
  <div class="sep"></div>
  <button id="btn-delete" disabled>Delete</button>
  <div class="sep"></div>
  <span class="label">Zoom</span>
  <select id="zoom-sel">
    <option value="0.5">50%</option>
    <option value="0.75">75%</option>
    <option value="1" selected>100%</option>
    <option value="1.25">125%</option>
    <option value="1.5">150%</option>
  </select>
</div>

<div id="main">
  <!-- Toolbox -->
  <div id="toolbox">
    <div class="tool-heading">Items</div>
    <div class="tool-item" draggable="true" data-type="Textbox">
      <span class="tool-icon">T</span>TextBox
    </div>
    <div class="tool-item" draggable="true" data-type="Rectangle">
      <span class="tool-icon">▭</span>Rectangle
    </div>
    <div class="tool-item" draggable="true" data-type="Line">
      <span class="tool-icon">╱</span>Line
    </div>
    <div class="tool-item" draggable="true" data-type="Image">
      <span class="tool-icon">🖼</span>Image
    </div>
    <div class="tool-item" draggable="true" data-type="Chart">
      <span class="tool-icon">📊</span>Chart
    </div>
    <div class="tool-item" draggable="true" data-type="Table">
      <span class="tool-icon">⊞</span>Table
    </div>
  </div>

  <!-- Canvas -->
  <div id="canvas-area">
    <div id="page"></div>
  </div>

  <!-- Right panel -->
  <div id="right-panel">
    <div id="panel-tabs">
      <button class="tab-btn active" data-tab="properties">Properties</button>
      <button class="tab-btn" data-tab="data">Data</button>
    </div>

    <!-- Properties tab -->
    <div id="tab-properties" class="tab-pane active">
      <div id="no-sel">Select an item to view its properties.</div>
      <div id="prop-content"></div>
    </div>

    <!-- Data tab -->
    <div id="tab-data" class="tab-pane">
      <div id="data-empty" style="display:none">
        No data sources defined. Click + to add one.
      </div>

      <!-- DataSources section -->
      <div class="data-section">
        <div class="data-section-head">
          <span class="data-section-title">Data Sources</span>
          <button class="data-add-btn" id="btn-add-ds">+</button>
        </div>
        <div id="ds-list"></div>
      </div>

      <!-- DataSets section -->
      <div class="data-section">
        <div class="data-section-head">
          <span class="data-section-title">Data Sets</span>
          <button class="data-add-btn" id="btn-add-dset">+</button>
        </div>
        <div id="dset-list"></div>
      </div>

      <!-- Fields section -->
      <div class="data-section" id="fields-section" style="display:none">
        <div class="data-section-head">
          <span class="data-section-title" id="fields-title">Fields</span>
          <button class="data-add-btn" id="btn-discover-fields" title="Auto-discover fields from data source">↺</button>
          <button class="data-add-btn" id="btn-insert-table" title="Insert table for this dataset onto canvas">⊞</button>
          <button class="data-add-btn" id="btn-add-field">+</button>
        </div>
        <div id="field-list"></div>
      </div>
    </div>
  </div>
</div>

<div id="statusbar">
  <span id="sb-item">No selection</span>
  <span id="sb-pos"></span>
</div>

<!-- Dialog -->
<div id="dlg-overlay">
  <div id="dlg-box">
    <div id="dlg-title"></div>
    <div id="dlg-body"></div>
    <div id="dlg-footer">
      <button id="dlg-ok">OK</button>
      <button id="dlg-cancel">Cancel</button>
    </div>
  </div>
</div>

<!-- Preview overlay -->
<div id="preview-overlay">
  <div id="preview-box">
    <div id="preview-bar">
      <span>Preview</span>
      <button id="btn-close-preview">Close</button>
    </div>
    <iframe id="preview-iframe" sandbox="allow-same-origin allow-scripts"></iframe>
  </div>
</div>
`;

// ── ReportDesigner custom element ─────────────────────────────────────────────

class ReportDesigner extends HTMLElement {

  static get observedAttributes() {
    return ['preview-endpoint', 'save-endpoint', 'load-endpoint', 'rdl'];
  }

  constructor() {
    super();
    this._shadow  = this.attachShadow({ mode: 'open' });
    this._report  = new RdlReport();
    this._zoom    = 1.0;
    this._selName = null;
    this._pendingToolType = null;
    this._selDataSet = null; // name of selected dataset in data panel
  }

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  connectedCallback() {
    this._shadow.innerHTML = SHADOW_HTML;
    this._$ = id => this._shadow.getElementById(id);

    this._page      = this._$('page');
    this._canvasArea = this._$('canvas-area');

    this._bindToolbar();
    this._bindTabs();
    this._bindToolbox();
    this._bindCanvas();
    this._bindDataPanel();
    this._refreshCanvas();
    this._refreshDataPanel();

    const rdlAttr = this.getAttribute('rdl');
    if (rdlAttr) this._loadFromXml(rdlAttr);
  }

  attributeChangedCallback(name, _old, value) {
    if (name === 'rdl' && value) this._loadFromXml(value);
  }

  // ── Public API ──────────────────────────────────────────────────────────────

  loadRdl(xml) { this._loadFromXml(xml); }
  getRdl()     { return RdlSerializer.toXml(this._report); }

  // ── Toolbar ─────────────────────────────────────────────────────────────────

  _bindToolbar() {
    this._$('btn-new').onclick     = () => this._newReport();
    this._$('btn-load').onclick    = () => this._showLoadDialog();
    this._$('btn-save').onclick    = () => this._showSaveDialog();
    this._$('btn-preview').onclick = () => this._preview();
    this._$('btn-delete').onclick  = () => this._deleteSelected();
    this._$('zoom-sel').onchange   = e => this._setZoom(parseFloat(e.target.value));
    this._$('btn-close-preview').onclick = () => this._$('preview-overlay').classList.remove('open');
  }

  // ── Tab switching ────────────────────────────────────────────────────────────

  _bindTabs() {
    for (const btn of this._shadow.querySelectorAll('.tab-btn')) {
      btn.addEventListener('click', () => {
        const tab = btn.dataset.tab;
        for (const b of this._shadow.querySelectorAll('.tab-btn'))
          b.classList.toggle('active', b.dataset.tab === tab);
        for (const p of this._shadow.querySelectorAll('.tab-pane'))
          p.classList.toggle('active', p.id === 'tab-' + tab);
      });
    }
  }

  // ── Toolbox ──────────────────────────────────────────────────────────────────

  _bindToolbox() {
    for (const tool of this._shadow.querySelectorAll('.tool-item')) {
      tool.addEventListener('dragstart', e => {
        this._pendingToolType = tool.dataset.type;
        e.dataTransfer.setData('text/plain', tool.dataset.type);
        e.dataTransfer.effectAllowed = 'copy';
      });
      tool.addEventListener('dragend', () => { this._pendingToolType = null; });
    }
  }

  // ── Canvas ───────────────────────────────────────────────────────────────────

  _bindCanvas() {
    const page = this._page;

    page.addEventListener('dragenter', e => {
      if (this._pendingToolType || e.dataTransfer.types.includes('rdl/field')) {
        e.preventDefault();
        page.classList.add('drag-over');
      }
    });
    page.addEventListener('dragleave', () => page.classList.remove('drag-over'));
    page.addEventListener('dragover', e => {
      if (this._pendingToolType || e.dataTransfer.types.includes('rdl/field')) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
      }
    });
    page.addEventListener('drop', e => {
      page.classList.remove('drag-over');
      e.preventDefault();

      const rect  = page.getBoundingClientRect();
      const scale = DPI * this._zoom;
      const left  = snap(clamp((e.clientX - rect.left) / scale, 0, this._report.bodyWidth  - 0.25));
      const top   = snap(clamp((e.clientY - rect.top)  / scale, 0, this._report.bodyHeight - 0.1));

      // Field dragged from data panel → add to or create a Table data region
      const fieldRef    = e.dataTransfer.getData('rdl/field');
      const fieldDsName = e.dataTransfer.getData('rdl/fieldset');
      if (fieldRef) {
        const existing = fieldDsName
          ? this._report.items.find(i => i.type === 'Table' && i.dataSetName === fieldDsName)
          : null;
        if (existing) {
          // Add as a new column to the existing table
          const perCol = existing.width / Math.max(1, existing.columns.length);
          existing.columns.push({ header: fieldRef, fieldExpr: `=Fields!${fieldRef}.Value`, width: perCol });
          existing.width = perCol * existing.columns.length;
          this._refreshCanvas();
          this._select(existing.name);
        } else {
          // Create a new single-column Table
          const tbl = this._report.createItem('Table');
          tbl.left        = left;
          tbl.top         = top;
          tbl.dataSetName = fieldDsName || '';
          tbl.columns     = [{ header: fieldRef, fieldExpr: `=Fields!${fieldRef}.Value`, width: 1.5 }];
          tbl.width       = 1.5;
          tbl.height      = 0.6;
          this._refreshCanvas();
          this._select(tbl.name);
        }
        return;
      }

      const type = e.dataTransfer.getData('text/plain') || this._pendingToolType;
      if (!type) return;

      const item = this._report.createItem(type);
      item.left  = left;
      item.top   = top;
      this._refreshCanvas();
      this._select(item.name);
    });

    page.addEventListener('pointerdown', e => {
      if (e.target === page) this._deselect();
    });
  }

  // ── Canvas rendering ─────────────────────────────────────────────────────────

  _refreshCanvas() {
    const page  = this._page;
    const scale = DPI * this._zoom;
    page.style.width  = Math.round(this._report.pageWidth  * scale) + 'px';
    page.style.height = Math.round(this._report.pageHeight * scale) + 'px';

    for (const el of [...page.querySelectorAll('.r-item')]) el.remove();
    for (const item of this._report.items) page.appendChild(this._makeItemEl(item));

    this._applySel();
    this._updatePropsPanel();
  }

  _makeItemEl(item) {
    const el    = document.createElement('div');
    el.className = 'r-item';
    el.dataset.name = item.name;

    const inner = document.createElement('div');
    inner.className = 'item-inner';
    el.appendChild(inner);

    this._styleItemEl(item, el, inner);
    this._attachItemPointer(item, el);
    return el;
  }

  _styleItemEl(item, el, inner) {
    const scale = DPI * this._zoom;
    el.style.left   = (item.left   * scale) + 'px';
    el.style.top    = (item.top    * scale) + 'px';
    el.style.width  = (item.width  * scale) + 'px';
    el.style.height = Math.max(1, item.height * scale) + 'px';

    inner = inner || el.querySelector('.item-inner');
    const s = item.style;

    if (item.type === 'Textbox') {
      el.style.background = s.backgroundColor || 'transparent';
      inner.style.fontFamily     = s.fontFamily;
      inner.style.fontSize       = s.fontSize;
      inner.style.fontWeight     = s.fontWeight === 'Bold' ? 'bold' : 'normal';
      inner.style.fontStyle      = s.fontStyle  === 'Italic' ? 'italic' : 'normal';
      inner.style.color          = s.color;
      inner.style.alignItems     = s.verticalAlign === 'Bottom' ? 'flex-end'
                                 : s.verticalAlign === 'Middle' ? 'center' : 'flex-start';
      inner.style.justifyContent = s.textAlign === 'Right' ? 'flex-end'
                                 : s.textAlign === 'Center' ? 'center' : 'flex-start';
      inner.style.padding = '1px 2px';
      // Show field expressions with a tinted background in design view
      const val = item.value || '';
      inner.textContent = val;
      if (val.startsWith('=Fields!')) {
        el.style.background = el.style.background || '#fffbe6';
      }

    } else if (item.type === 'Rectangle') {
      el.style.background = s.backgroundColor || 'transparent';
      el.style.border = s.borderStyle && s.borderStyle !== 'None'
        ? `${s.borderWidth || '1pt'} ${s.borderStyle.toLowerCase()} ${s.borderColor || 'black'}`
        : '1px solid #bbb';

    } else if (item.type === 'Line') {
      el.style.overflow  = 'visible';
      el.style.borderTop = `${s.borderWidth || '1pt'} ${(s.borderStyle || 'solid').toLowerCase()} ${s.borderColor || 'black'}`;
      el.style.height    = '1px';

    } else if (item.type === 'Image') {
      el.style.border = '1px dashed #aaa';
      inner.style.alignItems     = 'center';
      inner.style.justifyContent = 'center';
      inner.style.color          = '#aaa';
      inner.style.fontSize       = '11px';
      inner.textContent          = item.value ? `🖼 ${item.value}` : '🖼 Image';

    } else if (item.type === 'Table') {
      el.style.border = '1px solid #bbb';
      el.style.background = '#fff';
      inner.style.flexDirection = 'column';
      inner.style.fontSize      = '10px';
      inner.style.overflow      = 'hidden';
      inner.innerHTML = '';
      const cols = item.columns;
      if (cols.length === 0) {
        inner.textContent = `⊞ ${item.dataSetName || 'Table (no columns)'}`;
        inner.style.alignItems = 'center';
        inner.style.justifyContent = 'center';
        inner.style.color = '#999';
      } else {
        const colPct = (100 / cols.length).toFixed(1) + '%';
        const hrow = document.createElement('div');
        hrow.style.cssText = 'display:flex;width:100%;background:#e8e8e8;border-bottom:1px solid #bbb;flex-shrink:0;';
        const drow = document.createElement('div');
        drow.style.cssText = 'display:flex;width:100%;flex-shrink:0;';
        for (const col of cols) {
          const hcell = document.createElement('div');
          hcell.style.cssText = `width:${colPct};overflow:hidden;text-overflow:ellipsis;white-space:nowrap;padding:1px 2px;border-right:1px solid #ccc;font-weight:bold;`;
          hcell.textContent = col.header;
          hrow.appendChild(hcell);
          const dcell = document.createElement('div');
          dcell.style.cssText = `width:${colPct};overflow:hidden;text-overflow:ellipsis;white-space:nowrap;padding:1px 2px;border-right:1px solid #ccc;color:#4285f4;`;
          dcell.textContent = col.fieldExpr.replace(/^=Fields!(.+)\.Value$/, '$1');
          drow.appendChild(dcell);
        }
        inner.appendChild(hrow);
        inner.appendChild(drow);
      }

    } else if (item.type === 'Chart') {
      el.style.background = item.style.backgroundColor || '#fff';
      el.style.border = item.style.borderStyle && item.style.borderStyle !== 'None'
        ? `1px solid ${item.style.borderColor || '#bbb'}`
        : '1px solid #bbb';
      inner.style.flexDirection  = 'column';
      inner.style.alignItems     = 'center';
      inner.style.justifyContent = 'center';
      inner.style.gap            = '4px';
      inner.style.padding        = '4px';
      const label = item.title || item.name;
      const ds    = item.dataSetName ? ` [${item.dataSetName}]` : '';
      const t     = item.chartType || 'Column';
      let svgContent;
      if (t === 'Pie' || t === 'Doughnut') {
        const r = t === 'Doughnut' ? 'M20,14 a6,6 0 1,1 0,0.01 Z' : '';
        svgContent =
          `<path d="M20,14 L20,2 A12,12 0 0,1 30,20 Z" fill="#4285f4"/>` +
          `<path d="M20,14 L30,20 A12,12 0 0,1 8,21 Z"  fill="#34a853"/>` +
          `<path d="M20,14 L8,21  A12,12 0 0,1 20,2  Z" fill="#fbbc05"/>` +
          (t === 'Doughnut' ? `<circle cx="20" cy="14" r="6" fill="${item.style.backgroundColor||'#fff'}"/>` : '');
      } else if (t === 'Line' || t === 'Area') {
        const poly = '6,22 14,12 22,16 30,6 38,10';
        svgContent = (t === 'Area' ? `<polygon points="${poly} 38,26 6,26" fill="#4285f450"/>` : '') +
          `<polyline points="${poly}" fill="none" stroke="#4285f4" stroke-width="2"/>` +
          `<circle cx="6" cy="22" r="2" fill="#4285f4"/><circle cx="14" cy="12" r="2" fill="#4285f4"/>` +
          `<circle cx="22" cy="16" r="2" fill="#4285f4"/><circle cx="30" cy="6"  r="2" fill="#4285f4"/>` +
          `<circle cx="38" cy="10" r="2" fill="#4285f4"/>`;
      } else if (t === 'Bar') {
        svgContent =
          `<rect y="2"  x="16" height="6" width="22" fill="#4285f4"/>` +
          `<rect y="10" x="16" height="6" width="14" fill="#34a853"/>` +
          `<rect y="18" x="16" height="6" width="18" fill="#fbbc05"/>` +
          `<rect y="26" x="16" height="6" width="10" fill="#ea4335"/>`;
      } else {
        svgContent =
          `<rect x="2"  y="16" width="6" height="12" fill="#4285f4"/>` +
          `<rect x="10" y="8"  width="6" height="20" fill="#34a853"/>` +
          `<rect x="18" y="12" width="6" height="16" fill="#fbbc05"/>` +
          `<rect x="26" y="4"  width="6" height="24" fill="#ea4335"/>` +
          `<rect x="34" y="10" width="6" height="18" fill="#4285f4"/>`;
      }
      inner.innerHTML =
        `<svg viewBox="0 0 44 32" width="44" height="32" style="flex-shrink:0">${svgContent}</svg>` +
        `<span style="font-size:10px;color:#555;text-align:center;overflow:hidden;` +
        `text-overflow:ellipsis;white-space:nowrap;width:100%">` +
        `${escHtml(t)} — ${escHtml(label)}${escHtml(ds)}</span>`;
    }
  }

  _syncItemEl(item) {
    const el    = this._page.querySelector(`[data-name="${CSS.escape(item.name)}"]`);
    const inner = el?.querySelector('.item-inner');
    if (el) this._styleItemEl(item, el, inner);
  }

  // ── Item pointer (move + resize) ──────────────────────────────────────────────

  _attachItemPointer(item, el) {
    el.addEventListener('pointerdown', e => {
      if (e.button !== 0) return;
      e.stopPropagation();
      this._select(item.name);

      const handle = e.target.closest('.r-handle');
      if (handle) {
        this._startResize(e, item, el, handle.dataset.dir);
      } else {
        this._startMove(e, item, el);
      }
    });
  }

  _startMove(e, item, el) {
    e.preventDefault();
    el.setPointerCapture(e.pointerId);
    const startX = e.clientX, startY = e.clientY;
    const startLeft = item.left, startTop = item.top;

    const onMove = ev => {
      const s = DPI * this._zoom;
      item.left = snap(Math.max(0, startLeft + (ev.clientX - startX) / s));
      item.top  = snap(Math.max(0, startTop  + (ev.clientY - startY) / s));
      this._syncItemEl(item);
      this._updatePropsInputs(item);
      this._statusPos(item);
    };
    const onUp = () => {
      el.removeEventListener('pointermove', onMove);
      el.removeEventListener('pointerup',   onUp);
    };
    el.addEventListener('pointermove', onMove);
    el.addEventListener('pointerup',   onUp);
  }

  _startResize(e, item, el, dir) {
    e.preventDefault();
    e.stopPropagation();
    el.setPointerCapture(e.pointerId);
    const startX = e.clientX, startY = e.clientY;
    const oL = item.left, oT = item.top, oW = item.width, oH = item.height;
    const minW = 0.1, minH = 0.01;

    const onMove = ev => {
      const s = DPI * this._zoom;
      const dx = (ev.clientX - startX) / s, dy = (ev.clientY - startY) / s;
      if (dir.includes('e')) item.width  = Math.max(minW, snap(oW + dx));
      if (dir.includes('s')) item.height = Math.max(minH, snap(oH + dy));
      if (dir.includes('w')) { const nW = Math.max(minW, snap(oW - dx)); item.left = snap(oL + oW - nW); item.width  = nW; }
      if (dir.includes('n')) { const nH = Math.max(minH, snap(oH - dy)); item.top  = snap(oT + oH - nH); item.height = nH; }
      this._syncItemEl(item);
      this._updatePropsInputs(item);
      this._statusPos(item);
    };
    const onUp = () => {
      el.removeEventListener('pointermove', onMove);
      el.removeEventListener('pointerup',   onUp);
    };
    el.addEventListener('pointermove', onMove);
    el.addEventListener('pointerup',   onUp);
  }

  // ── Selection ────────────────────────────────────────────────────────────────

  _select(name) {
    this._selName = name;
    this._applySel();
    this._updatePropsPanel();
    this._$('btn-delete').disabled = false;
    this._$('sb-item').textContent = name;
    const item = this._report.items.find(i => i.name === name);
    if (item) this._statusPos(item);
  }

  _deselect() {
    this._selName = null;
    this._applySel();
    this._updatePropsPanel();
    this._$('btn-delete').disabled = true;
    this._$('sb-item').textContent = 'No selection';
    this._$('sb-pos').textContent  = '';
  }

  _applySel() {
    for (const el of this._page.querySelectorAll('.r-item')) {
      const selected = el.dataset.name === this._selName;
      el.classList.toggle('selected', selected);
      for (const h of el.querySelectorAll('.r-handle')) h.remove();
      if (selected) {
        for (const dir of ['nw','n','ne','e','se','s','sw','w']) {
          const h = document.createElement('div');
          h.className = 'r-handle';
          h.dataset.dir = dir;
          el.appendChild(h);
        }
      }
    }
  }

  _selItem() {
    return this._report.items.find(i => i.name === this._selName) ?? null;
  }

  // ── Properties panel ──────────────────────────────────────────────────────────

  _updatePropsPanel() {
    const noSel  = this._$('no-sel');
    const content = this._$('prop-content');
    const item   = this._selItem();

    if (!item) {
      noSel.style.display   = '';
      content.style.display = 'none';
      content.innerHTML     = '';
      return;
    }

    noSel.style.display   = 'none';
    content.style.display = '';

    const row = (label, html) =>
      `<div class="prow"><label>${label}</label>${html}</div>`;
    const num  = (id, v, step = 0.05) =>
      `<input type="number" id="${id}" value="${(+v).toFixed(3)}" step="${step}" min="0">`;
    const txt  = (id, v) =>
      `<input type="text" id="${id}" value="${escHtml(v)}">`;
    const sel  = (id, v, opts) =>
      `<select id="${id}">${opts.map(o => `<option${o===v?' selected':''}>${o}</option>`).join('')}</select>`;
    const clr  = (id, v) =>
      `<input type="color" id="${id}" value="${toHex(v)}">`;
    const ta   = (id, v) =>
      `<textarea id="${id}" rows="2">${escHtml(v)}</textarea>`;

    const s      = item.style;
    const groups = [];

    groups.push(`<div class="pgroup">
      <div class="pgroup-title">Layout</div>
      ${row('Name', `<span style="font-size:11px;color:#555;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escHtml(item.name)}</span>`)}
      ${row('Left (in)',    num('p-left',   item.left))}
      ${row('Top (in)',     num('p-top',    item.top))}
      ${row('Width (in)',   num('p-width',  item.width))}
      ${row('Height (in)',  num('p-height', item.height))}
    </div>`);

    if (item.type === 'Textbox') {
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Content</div>
        ${row('Value', ta('p-value', item.value))}
        ${row('Can Grow', sel('p-cangrow', item.canGrow ? 'true' : 'false', ['true','false']))}
      </div>`);
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Font</div>
        ${row('Family',  txt('p-fontfamily', s.fontFamily))}
        ${row('Size',    txt('p-fontsize',   s.fontSize))}
        ${row('Weight',  sel('p-fontweight', s.fontWeight, ['Normal','Bold','Lighter','Bolder']))}
        ${row('Style',   sel('p-fontstyle',  s.fontStyle,  ['Normal','Italic']))}
      </div>`);
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Appearance</div>
        ${row('Color',      clr('p-color',   s.color))}
        ${row('Background', clr('p-bgcolor', s.backgroundColor || '#ffffff'))}
        ${row('Align',      sel('p-align',   s.textAlign,    ['Left','Center','Right']))}
        ${row('V-Align',    sel('p-valign',  s.verticalAlign, ['Top','Middle','Bottom']))}
      </div>`);
    }

    if (item.type === 'Rectangle') {
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Appearance</div>
        ${row('Background', clr('p-bgcolor',      s.backgroundColor || '#ffffff'))}
        ${row('Border',     sel('p-border',       s.borderStyle, ['None','Solid','Dashed','Dotted','Double']))}
        ${row('Bdr Color',  clr('p-bordercolor',  s.borderColor || '#000000'))}
        ${row('Bdr Width',  txt('p-borderwidth',  s.borderWidth))}
      </div>`);
    }

    if (item.type === 'Line') {
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Appearance</div>
        ${row('Line Style', sel('p-border',      s.borderStyle, ['Solid','Dashed','Dotted','Double']))}
        ${row('Line Color', clr('p-bordercolor', s.borderColor || '#000000'))}
        ${row('Line Width', txt('p-borderwidth', s.borderWidth))}
      </div>`);
    }

    if (item.type === 'Image') {
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Image</div>
        ${row('Source', sel('p-source',   item.source, ['External','Embedded','Database']))}
        ${row('Value',  txt('p-imgvalue', item.value))}
      </div>`);
    }

    if (item.type === 'Chart') {
      const dsetOpts = ['', ...this._report.dataSets.map(d => d.name)];
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Chart</div>
        ${row('Type',     sel('p-charttype',    item.chartType,    ['Column','Bar','Line','Pie','Area','Doughnut','Scatter']))}
        ${row('Subtype',  sel('p-chartsubtype', item.chartSubtype, ['Plain','Stacked','PercentStacked']))}
        ${row('DataSet',  sel('p-chartds',      item.dataSetName,  dsetOpts))}
        ${row('Category', txt('p-chartcat',     item.categoryField))}
        ${row('Series',   txt('p-chartser',     item.seriesField))}
        ${row('Value',    txt('p-chartval',     item.valueExpr))}
        ${row('Title',    txt('p-charttitle',   item.title))}
        ${row('No Rows',  txt('p-chartnorows',  item.noRows))}
        ${row('Palette',  sel('p-chartpalette', item.palette, ['Default','EarthTones','Excel','GrayScale','Light','Pastel','SemiTransparent','Pacific','Fire']))}
      </div>`);
    }

    if (item.type === 'Table') {
      const dsetOpts = ['', ...this._report.dataSets.map(d => d.name)];
      const last = item.columns.length - 1;
      const colRows = item.columns.map((col, i) =>
        `<div class="tbl-col-row">
          <span class="tbl-col-name" title="${escHtml(col.fieldExpr)}">${escHtml(col.header)}</span>
          <button class="tbl-col-btn" data-act="up"  data-ci="${i}"${i === 0    ? ' disabled' : ''}>▲</button>
          <button class="tbl-col-btn" data-act="dn"  data-ci="${i}"${i === last ? ' disabled' : ''}>▼</button>
          <button class="tbl-col-btn" data-act="del" data-ci="${i}">✕</button>
        </div>`
      ).join('');
      groups.push(`<div class="pgroup">
        <div class="pgroup-title">Table</div>
        ${row('DataSet', sel('p-tablds', item.dataSetName, dsetOpts))}
        ${row('No Rows', txt('p-tablnorows', item.noRows))}
        <div class="pgroup-title" style="margin-top:6px">Columns</div>
        <div id="p-tbl-cols">${colRows || '<span style="font-size:11px;color:#aaa">No columns</span>'}</div>
      </div>`);
    }

    content.innerHTML = groups.join('');
    this._bindPropsInputs(item);
  }

  _bindPropsInputs(item) {
    const s       = item.style;
    const content = this._$('prop-content');
    const on      = (id, fn) => {
      const el  = content.querySelector('#' + id);
      if (!el) return;
      const evt = (el.tagName === 'SELECT' || el.type === 'color') ? 'input' : 'change';
      el.addEventListener(evt, fn);
    };

    on('p-left',   e => { item.left   = parseFloat(e.target.value) || 0;    this._syncItemEl(item); this._statusPos(item); });
    on('p-top',    e => { item.top    = parseFloat(e.target.value) || 0;    this._syncItemEl(item); this._statusPos(item); });
    on('p-width',  e => { item.width  = parseFloat(e.target.value) || 0.1;  this._syncItemEl(item); });
    on('p-height', e => { item.height = parseFloat(e.target.value) || 0.01; this._syncItemEl(item); });

    if (item.type === 'Textbox') {
      on('p-value',      e => { item.value     = e.target.value; this._syncItemEl(item); });
      on('p-cangrow',    e => { item.canGrow    = e.target.value === 'true'; });
      on('p-fontfamily', e => { s.fontFamily    = e.target.value; this._syncItemEl(item); });
      on('p-fontsize',   e => { s.fontSize      = e.target.value; this._syncItemEl(item); });
      on('p-fontweight', e => { s.fontWeight    = e.target.value; this._syncItemEl(item); });
      on('p-fontstyle',  e => { s.fontStyle     = e.target.value; this._syncItemEl(item); });
      on('p-color',      e => { s.color         = e.target.value; this._syncItemEl(item); });
      on('p-bgcolor',    e => { s.backgroundColor = e.target.value; this._syncItemEl(item); });
      on('p-align',      e => { s.textAlign     = e.target.value; this._syncItemEl(item); });
      on('p-valign',     e => { s.verticalAlign = e.target.value; this._syncItemEl(item); });
    }
    if (item.type === 'Rectangle') {
      on('p-bgcolor',     e => { s.backgroundColor = e.target.value; this._syncItemEl(item); });
      on('p-border',      e => { s.borderStyle = e.target.value; this._syncItemEl(item); });
      on('p-bordercolor', e => { s.borderColor = e.target.value; this._syncItemEl(item); });
      on('p-borderwidth', e => { s.borderWidth = e.target.value; this._syncItemEl(item); });
    }
    if (item.type === 'Line') {
      on('p-border',      e => { s.borderStyle = e.target.value; this._syncItemEl(item); });
      on('p-bordercolor', e => { s.borderColor = e.target.value; this._syncItemEl(item); });
      on('p-borderwidth', e => { s.borderWidth = e.target.value; this._syncItemEl(item); });
    }
    if (item.type === 'Image') {
      on('p-source',   e => { item.source = e.target.value; this._syncItemEl(item); });
      on('p-imgvalue', e => { item.value  = e.target.value; this._syncItemEl(item); });
    }
    if (item.type === 'Chart') {
      on('p-charttype',    e => { item.chartType     = e.target.value; this._syncItemEl(item); });
      on('p-chartsubtype', e => { item.chartSubtype  = e.target.value; });
      on('p-chartds',      e => { item.dataSetName   = e.target.value; this._syncItemEl(item); });
      on('p-chartcat',     e => { item.categoryField = e.target.value; });
      on('p-chartser',     e => { item.seriesField   = e.target.value; });
      on('p-chartval',     e => { item.valueExpr     = e.target.value; });
      on('p-charttitle',   e => { item.title         = e.target.value; this._syncItemEl(item); });
      on('p-chartnorows',  e => { item.noRows        = e.target.value; });
      on('p-chartpalette', e => { item.palette       = e.target.value; });
    }
    if (item.type === 'Table') {
      on('p-tablds',     e => { item.dataSetName = e.target.value; this._syncItemEl(item); });
      on('p-tablnorows', e => { item.noRows      = e.target.value; });

      const colsEl = content.querySelector('#p-tbl-cols');
      if (colsEl) {
        colsEl.addEventListener('click', e => {
          const btn = e.target.closest('.tbl-col-btn');
          if (!btn) return;
          const i   = parseInt(btn.dataset.ci, 10);
          const act = btn.dataset.act;
          if (act === 'up' && i > 0) {
            [item.columns[i - 1], item.columns[i]] = [item.columns[i], item.columns[i - 1]];
          } else if (act === 'dn' && i < item.columns.length - 1) {
            [item.columns[i + 1], item.columns[i]] = [item.columns[i], item.columns[i + 1]];
          } else if (act === 'del') {
            item.columns.splice(i, 1);
            item.width = item.columns.reduce((s, c) => s + (c.width || 1.5), 0) || 1.5;
          }
          this._syncItemEl(item);
          this._updatePropsPanel();
        });
      }
    }
  }

  _updatePropsInputs(item) {
    const content = this._$('prop-content');
    const set = (id, v) => { const el = content.querySelector('#' + id); if (el) el.value = (+v).toFixed(3); };
    set('p-left', item.left); set('p-top', item.top);
    set('p-width', item.width); set('p-height', item.height);
  }

  // ── Data panel ────────────────────────────────────────────────────────────────

  _bindDataPanel() {
    this._$('btn-add-ds').onclick    = () => this._showAddDataSourceDialog();
    this._$('btn-add-dset').onclick  = () => this._showAddDataSetDialog();
    this._$('btn-add-field').onclick = () => {
      if (this._selDataSet) this._showAddFieldDialog(this._selDataSet);
    };
    this._$('btn-discover-fields').onclick = () => {
      if (this._selDataSet) this._discoverFields(this._selDataSet);
    };
    this._$('btn-insert-table').onclick = () => {
      if (this._selDataSet) this._insertTableFromDataset(this._selDataSet);
    };
  }

  _refreshDataPanel() {
    const r        = this._report;
    const dsList   = this._$('ds-list');
    const dsetList = this._$('dset-list');
    const fieldSec = this._$('fields-section');
    const fieldList = this._$('field-list');

    // DataSources
    dsList.innerHTML = '';
    for (const ds of r.dataSources) {
      const row = document.createElement('div');
      row.className = 'data-row';
      row.title = ds.connectString;
      row.innerHTML = `<span class="data-row-name">🗄 ${escHtml(ds.name)}</span>
        <button class="data-row-del" data-ds="${escHtml(ds.name)}" title="Remove">✕</button>`;
      row.querySelector('.data-row-del').addEventListener('click', e => {
        e.stopPropagation();
        this._removeDataSource(ds.name);
      });
      dsList.appendChild(row);
    }

    // DataSets
    dsetList.innerHTML = '';
    for (const ds of r.dataSets) {
      const row = document.createElement('div');
      row.className = 'data-row' + (ds.name === this._selDataSet ? ' selected' : '');
      row.title = ds.commandText;
      row.innerHTML = `<span class="data-row-name">📋 ${escHtml(ds.name)}</span>
        <button class="data-row-del" title="Remove">✕</button>`;
      row.addEventListener('click', e => {
        if (e.target.classList.contains('data-row-del')) return;
        this._selDataSet = ds.name;
        this._refreshDataPanel();
      });
      row.querySelector('.data-row-del').addEventListener('click', e => {
        e.stopPropagation();
        this._removeDataSet(ds.name);
      });
      dsetList.appendChild(row);
    }

    // Fields for selected DataSet
    const dset = r.dataSets.find(d => d.name === this._selDataSet);
    if (dset) {
      fieldSec.style.display = '';
      this._$('fields-title').textContent = `Fields — ${dset.name}`;
      fieldList.innerHTML = '';
      for (const f of dset.fields) {
        const row = document.createElement('div');
        row.className = 'field-row';
        row.draggable = true;
        row.title = `Drag to canvas  •  Type: ${f.typeName}`;
        row.innerHTML = `<span><span class="field-icon">▸</span>${escHtml(f.name)}</span>
          <button class="data-row-del" title="Remove">✕</button>`;

        row.addEventListener('dragstart', e => {
          e.dataTransfer.setData('rdl/field',    f.name);
          e.dataTransfer.setData('rdl/fieldset', dset.name);
          e.dataTransfer.effectAllowed = 'copy';
        });
        row.querySelector('.data-row-del').addEventListener('click', () => {
          this._removeField(dset.name, f.name);
        });
        fieldList.appendChild(row);
      }
    } else {
      fieldSec.style.display = 'none';
      this._selDataSet = null;
    }
  }

  // ── Data CRUD ─────────────────────────────────────────────────────────────────

  // Connection-string placeholder text keyed by provider name (case-insensitive lookup below).
  static _CONN_HINTS = {
    SQL:        'Server=myserver;Database=mydb;User Id=myuser;Password=mypass',
    SQLite:     'Data Source=/path/to/database.db',
    PostgreSQL: 'Host=myserver;Database=mydb;Username=myuser;Password=mypass',
    MySQL:      'Server=myserver;Database=mydb;Uid=myuser;Pwd=mypass',
    Oracle:     'Data Source=myserver/mydb;User Id=myuser;Password=mypass',
    ODBC:       'DSN=mydsn;Uid=myuser;Pwd=mypass',
    OleDb:      'Provider=SQLOLEDB;Data Source=myserver;Initial Catalog=mydb;User Id=myuser;Password=mypass',
    Json:       'file=/path/to/data.json\n— or —\nurl=https://example.com/data.json\n— or —\nurl=https://example.com/data.json;auth=Bearer: <token>',
  };

  _showAddDataSourceDialog() {
    const dsNames = this._report.dataSources.map(d => d.name);
    const hints   = ReportDesigner._CONN_HINTS;
    this._dialog('Add Data Source', [
      { label: 'Name',              id: 'f-dsname',  type: 'text',     value: `DataSource${dsNames.length + 1}` },
      { label: 'Data Provider',     id: 'f-dsprov',  type: 'select',   options: ['SQL','ODBC','OleDb','SQLite','PostgreSQL','MySQL','Oracle','Json'], value: 'SQL' },
      { label: 'Connection String', id: 'f-dsconn',  type: 'textarea', value: '', placeholder: hints['SQL'] },
    ], (bodyEl) => {
      const provSel = bodyEl.querySelector('#f-dsprov');
      const connTa  = bodyEl.querySelector('#f-dsconn');
      const updateHint = () => {
        connTa.placeholder = hints[provSel.value] || '';
      };
      provSel.addEventListener('change', updateHint);
      updateHint();
    }).then(vals => {
      if (!vals) return;
      if (!vals['f-dsname']) return;
      const ds = new RdlDataSource(vals['f-dsname']);
      ds.dataProvider  = vals['f-dsprov']  || 'SQL';
      ds.connectString = vals['f-dsconn']  || '';
      this._report.dataSources.push(ds);
      this._refreshDataPanel();
    });
  }

  _removeDataSource(name) {
    this._report.dataSources = this._report.dataSources.filter(d => d.name !== name);
    this._refreshDataPanel();
  }

  _showAddDataSetDialog() {
    const dsNames   = this._report.dataSources.map(d => d.name);
    const dsetNames = this._report.dataSets.map(d => d.name);
    const dsOpts    = dsNames.length ? dsNames : ['(none)'];
    const sources   = this._report.dataSources; // for provider lookup in onShown

    this._dialog('Add Data Set', [
      { label: 'Name',         id: 'f-dsetname',  type: 'text',     value: `DataSet${dsetNames.length + 1}` },
      { label: 'Data Source',  id: 'f-dsetsrc',   type: 'select',   options: dsOpts, value: dsOpts[0] },
      { label: 'Command Type', id: 'f-dsetctype', type: 'select',   options: ['Text','StoredProcedure','TableDirect'], value: 'Text' },
      { label: 'Query',        id: 'f-dsetq',     type: 'textarea', value: 'SELECT * FROM ',
        placeholder: 'SQL query  — or for Json:  columns=Field1,Field2' },
    ], (bodyEl) => {
      const srcSel    = bodyEl.querySelector('#f-dsetsrc');
      const ctypeSel  = bodyEl.querySelector('#f-dsetctype');
      const queryTa   = bodyEl.querySelector('#f-dsetq');
      const SQL_DEFAULT  = 'SELECT * FROM ';
      const JSON_DEFAULT = 'columns=';
      const JSON_HINT    = 'columns=Field1,Field2\n— or with a named array —\ntable=ArrayName;columns=Field1,Field2';

      const updateForSource = () => {
        const src = sources.find(d => d.name === srcSel.value);
        const isJson = src?.dataProvider?.toLowerCase() === 'json';
        if (isJson) {
          queryTa.placeholder = JSON_HINT;
          ctypeSel.disabled   = true;
          // Only replace the value if it still holds the SQL default.
          if (!queryTa.value || queryTa.value === SQL_DEFAULT)
            queryTa.value = JSON_DEFAULT;
        } else {
          queryTa.placeholder = 'SQL query  — or for Json:  columns=Field1,Field2';
          ctypeSel.disabled   = false;
          if (!queryTa.value || queryTa.value === JSON_DEFAULT)
            queryTa.value = SQL_DEFAULT;
        }
      };

      srcSel.addEventListener('change', updateForSource);
      updateForSource();
    }).then(vals => {
      if (!vals) return;
      if (!vals['f-dsetname']) return;
      const ds = new RdlDataSet(vals['f-dsetname']);
      ds.dataSourceName = vals['f-dsetsrc']   || '';
      ds.commandType    = vals['f-dsetctype'] || 'Text';
      ds.commandText    = vals['f-dsetq']     || '';
      this._report.dataSets.push(ds);
      this._selDataSet = ds.name;
      this._refreshDataPanel();
      // Auto-discover fields for file-based providers that support schema discovery.
      const srcForDs = this._report.dataSources.find(d => d.name === ds.dataSourceName);
      if (srcForDs && FILE_PROVIDERS.has(srcForDs.dataProvider.toLowerCase())) {
        this._discoverFields(ds.name);
      }
    });
  }

  _removeDataSet(name) {
    this._report.dataSets = this._report.dataSets.filter(d => d.name !== name);
    if (this._selDataSet === name) this._selDataSet = null;
    this._refreshDataPanel();
  }

  _showAddFieldDialog(dataSetName) {
    this._dialog('Add Field', [
      { label: 'Field Name',  id: 'f-fname',  type: 'text', value: '' },
      { label: 'Data Field',  id: 'f-fdata',  type: 'text', value: '' },
      { label: 'Type',        id: 'f-ftype',  type: 'select',
        options: ['System.String','System.Int32','System.Int64','System.Decimal','System.Double','System.DateTime','System.Boolean'],
        value: 'System.String' },
    ]).then(vals => {
      if (!vals) return;
      if (!vals['f-fname']) return;
      const ds = this._report.dataSets.find(d => d.name === dataSetName);
      if (!ds) return;
      const f       = new RdlField(vals['f-fname'], vals['f-fdata'] || vals['f-fname']);
      f.typeName    = vals['f-ftype'] || 'System.String';
      ds.fields.push(f);
      this._refreshDataPanel();
    });
  }

  _removeField(dataSetName, fieldName) {
    const ds = this._report.dataSets.find(d => d.name === dataSetName);
    if (ds) ds.fields = ds.fields.filter(f => f.name !== fieldName);
    this._refreshDataPanel();
  }

  async _discoverFields(dataSetName) {
    const previewEp = this.getAttribute('preview-endpoint');
    if (!previewEp) { alert('preview-endpoint attribute not set.'); return; }
    const schemaEp = previewEp.replace(/\/[^/]+$/, '/schema');

    const dset = this._report.dataSets.find(d => d.name === dataSetName);
    if (!dset) return;
    const src  = this._report.dataSources.find(d => d.name === dset.dataSourceName);
    if (!src)  { alert(`Data source '${dset.dataSourceName}' not found.`); return; }

    const discoverBtn = this._$('btn-discover-fields');
    const origText = discoverBtn.textContent;
    discoverBtn.textContent = '…';
    discoverBtn.disabled = true;

    try {
      const resp = await fetch(schemaEp, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({
          connectionName:   src.name, // resolved to a server-configured connection
          dataProvider:     src.dataProvider,
          connectionString: src.connectString,
          commandText:      dset.commandText,
        }),
      });
      if (!resp.ok) {
        const msg = await resp.text();
        alert(`Field discovery failed:\n${msg}`);
        return;
      }
      const { fields } = await resp.json();
      if (!fields?.length) { alert('No fields returned — check your connection string and query.'); return; }
      dset.fields = fields.map(f => {
        const fld = new RdlField(f.name, f.name);
        fld.typeName = f.typeName || 'System.String';
        return fld;
      });
      this._refreshDataPanel();
    } catch (err) {
      alert(`Field discovery error:\n${err.message}`);
    } finally {
      discoverBtn.textContent = origText;
      discoverBtn.disabled = false;
    }
  }

  _insertTableFromDataset(dataSetName) {
    const dset = this._report.dataSets.find(d => d.name === dataSetName);
    if (!dset) return;
    if (dset.fields.length === 0) {
      alert('No fields defined for this dataset. Use ↺ to discover fields or + to add them manually first.');
      return;
    }
    const tbl = this._report.createItem('Table');
    tbl.dataSetName = dataSetName;
    const colW = Math.max(0.8, Math.min(2.0, (this._report.bodyWidth * 0.9) / dset.fields.length));
    tbl.columns = dset.fields.map(f => ({
      header:    f.name,
      fieldExpr: `=Fields!${f.name}.Value`,
      width:     colW,
    }));
    tbl.width  = colW * dset.fields.length;
    tbl.height = 0.6;
    tbl.left   = 0;
    tbl.top    = snap(0.5);
    this._refreshCanvas();
    this._select(tbl.name);
  }

  // ── Toolbar actions ──────────────────────────────────────────────────────────

  _newReport() {
    this._report     = new RdlReport();
    this._selName    = null;
    this._selDataSet = null;
    this._refreshCanvas();
    this._deselect();
    this._refreshDataPanel();
  }

  _deleteSelected() {
    const item = this._selItem();
    if (!item) return;
    this._report.remove(item);
    this._deselect();
    this._refreshCanvas();
  }

  _setZoom(z) {
    this._zoom = z;
    this._refreshCanvas();
  }

  // ── Load / Save ───────────────────────────────────────────────────────────────

  _showLoadDialog() {
    const ep = this.getAttribute('load-endpoint');
    if (!ep) { alert('load-endpoint attribute not set.'); return; }

    this._dialog('Load Report', [
      { label: 'Report name (without .rdl)', id: 'f-name', type: 'text', value: '' }
    ]).then(vals => {
      if (!vals) return;
      const name = vals['f-name'];
      if (!name) return;
      fetch(`${ep}?name=${encodeURIComponent(name)}`)
        .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.text(); })
        .then(xml => this._loadFromXml(xml))
        .catch(err => alert('Load failed: ' + err.message));
    });
  }

  _showSaveDialog() {
    const ep = this.getAttribute('save-endpoint');
    if (!ep) { alert('save-endpoint attribute not set.'); return; }

    this._dialog('Save Report', [
      { label: 'Report name (without .rdl)', id: 'f-name', type: 'text', value: '' }
    ]).then(vals => {
      if (!vals) return;
      const name = vals['f-name'];
      if (!name) return;
      const rdl = RdlSerializer.toXml(this._report);
      fetch(ep, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, rdl }),
      })
        .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json(); })
        .then(j => alert(`Saved as ${j.saved}`))
        .catch(err => alert('Save failed: ' + err.message));
    });
  }

  // ── Preview ───────────────────────────────────────────────────────────────────

  _preview() {
    const ep = this.getAttribute('preview-endpoint');
    if (!ep) { alert('preview-endpoint attribute not set.'); return; }

    const overlay = this._$('preview-overlay');
    const iframe  = this._$('preview-iframe');
    overlay.classList.add('open');
    iframe.srcdoc = '<html><body style="display:flex;align-items:center;justify-content:center;height:100%;color:#888;font-family:system-ui">Loading…</body></html>';

    fetch(ep, {
      method: 'POST',
      headers: { 'Content-Type': 'text/xml' },
      body: RdlSerializer.toXml(this._report),
    })
      .then(r => r.text())
      .then(html => { iframe.srcdoc = html; })
      .catch(err => {
        iframe.srcdoc = `<html><body style="padding:16px;color:#c00;font-family:system-ui">Error: ${err.message}</body></html>`;
      });
  }

  // ── Internals ─────────────────────────────────────────────────────────────────

  _loadFromXml(xml) {
    try {
      this._report     = RdlSerializer.fromXml(xml);
      this._selName    = null;
      this._selDataSet = null;
      if (this._page) {
        this._refreshCanvas();
        this._deselect();
        this._refreshDataPanel();
      }
    } catch (err) {
      console.error('[report-designer] fromXml error:', err);
      alert('Failed to load RDL: ' + err.message);
    }
  }

  _statusPos(item) {
    this._$('sb-pos').textContent =
      `L:${item.left.toFixed(2)}" T:${item.top.toFixed(2)}" W:${item.width.toFixed(2)}" H:${item.height.toFixed(2)}"`;
  }

  /**
   * Show a dialog with multiple labelled inputs.
   * fields: Array of {label, id, type, value, options?, placeholder?}
   * onShown: optional (bodyEl) => void — called after the dialog is rendered,
   *   useful for wiring inter-field behaviour (e.g. provider-dependent hints).
   * Resolves with {id: value, ...} or null if cancelled.
   */
  _dialog(title, fields, onShown) {
    return new Promise(resolve => {
      const overlay = this._$('dlg-overlay');
      this._$('dlg-title').textContent = title;

      const bodyEl = this._$('dlg-body');
      bodyEl.innerHTML = fields.map(f => {
        let input;
        const ph = f.placeholder ? ` placeholder="${escHtml(f.placeholder)}"` : '';
        if (f.type === 'select') {
          input = `<select id="${f.id}">${(f.options || []).map(o =>
            `<option${o === f.value ? ' selected' : ''}>${escHtml(o)}</option>`
          ).join('')}</select>`;
        } else if (f.type === 'textarea') {
          input = `<textarea id="${f.id}" rows="3"${ph}>${escHtml(f.value || '')}</textarea>`;
        } else {
          input = `<input type="text" id="${f.id}" value="${escHtml(f.value || '')}"${ph}>`;
        }
        return `<label>${escHtml(f.label)}${input}</label>`;
      }).join('');

      overlay.classList.add('open');
      const firstInput = bodyEl.querySelector('input, select, textarea');
      if (firstInput) firstInput.focus();
      if (typeof onShown === 'function') onShown(bodyEl);

      const okBtn     = this._$('dlg-ok');
      const cancelBtn = this._$('dlg-cancel');

      const collect = () => {
        const vals = {};
        for (const f of fields) {
          const el = bodyEl.querySelector('#' + f.id);
          if (el) vals[f.id] = el.value;
        }
        return vals;
      };

      const close = (ok) => {
        overlay.classList.remove('open');
        // Re-clone to clear stale handlers
        okBtn.replaceWith(okBtn.cloneNode(true));
        cancelBtn.replaceWith(cancelBtn.cloneNode(true));
        resolve(ok ? collect() : null);
      };

      this._$('dlg-ok').onclick     = () => close(true);
      this._$('dlg-cancel').onclick = () => close(false);

      const firstText = bodyEl.querySelector('input[type=text], textarea');
      if (firstText) {
        firstText.addEventListener('keydown', e => {
          if (e.key === 'Enter') close(true);
          if (e.key === 'Escape') close(false);
        });
      }
    });
  }
}

customElements.define('report-designer', ReportDesigner);
