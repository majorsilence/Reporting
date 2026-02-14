using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Text;
using EncryptionProvider;
using EncryptionProvider.String;
using Majorsilence.Reporting.RdlViewer.Resources;
using Majorsilence.Reporting.Rdl;
using Majorsilence.WinformUtils;
using System.ComponentModel;

namespace Majorsilence.Reporting.RdlViewer
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// RdlViewer displays RDL files or syntax. 
    /// </summary>
    public partial class RdlViewer : System.Windows.Forms.UserControl
    {
        public delegate void HyperlinkEventHandler(object source, HyperlinkEventArgs e);

        public delegate void PageNavigationEventHandler(object sender, PageNavigationEventArgs e);

        /// <summary>
        /// Hyperlink invoked when report item with hyperlink is clicked on.
        /// </summary>
        public event HyperlinkEventHandler Hyperlink;
        public event EventHandler<SubreportDataRetrievalEventArgs> SubreportDataRetrieval;
        public event PageNavigationEventHandler PageNavigation;

        /// <summary>
        /// EBN 31/03/2014
        /// Delegate is used to provide the content of a sub report from an alternate source (server, database, memory, ...)
        /// </summary>
        /// <param name="SubReportName">Name and path of the subreport</param>
        /// <returns>The content of the sub report (XML content)</returns>
        private CrossDelegate SubReportGetContent = new CrossDelegate();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CrossDelegate.GetContent dSubReportGetContent
        {
            get { return SubReportGetContent.SubReportGetContent; }
            set { SubReportGetContent.SubReportGetContent = value; }
        }

        public NeedPassword GetDataSourceReferencePassword = null;
        /// <summary>
        /// File name to use
        /// </summary>
        private Uri _SourceFileName;
        /// <summary>
        /// Source Rdl; if provided overrides filename
        /// </summary>
        private string _SourceRdl;
        /// <summary>
        /// Parameters to run the report
        /// </summary>
        private IDictionary _Parameters = new Dictionary<string, string>();
        /// <summary>
        /// The Report
        /// </summary>
        private Report _Report;
        /// <summary>
        /// Folder for DataSourceReference (if file name not provided)
        /// </summary>
        private string _Folder;
        /// <summary>
        /// The pages of the report to view
        /// </summary>
        private Pages _pgs;
        /// <summary>
        /// Last load of report failed
        /// </summary>
        private bool _loadFailed;
        /// <summary>
        /// Left margin; calculated based on size of window & scroll style
        /// </summary>
        private float _leftMargin;

        #region report information

        /// <summary>
        /// Width of page
        /// </summary>
        private float _PageWidth;
        /// <summary>
        /// Height of page
        /// </summary>
        private float _PageHeight;
        private string _ReportDescription;
        private string _ReportAuthor;
        private string _ReportName;
        private IList _errorMsgs;

        #endregion report information

        //private PageDrawing _pd;			// draws the pages of a report

        #region Zoom

        /// <summary>
        /// Zoom factor
        /// </summary>
        private float _zoom;
        private float DpiX;
        private float DpiY;
        private ZoomEnum _zoomMode = ZoomEnum.FitWidth;
        /// <summary>
        /// Right margin: 10 points
        /// </summary>
        private float _leftGap = 10;
        /// <summary>
        /// Left margin: 10 points
        /// </summary>
        private float _rightGap = 10;
        /// <summary>
        /// gap between pages: 10 points
        /// </summary>
        private float _pageGap = 10;

        #endregion

        #region Printing

        /// <summary>
        /// Compensate for non-printable region
        /// </summary>
        private bool _UseTrueMargins = true;
        /// <summary>
        /// End page
        /// </summary>
        private int printEndPage;
        /// <summary>
        /// Current page to print
        /// </summary>
        private int printCurrentPage;

        #endregion Printing

        // Scrollbars
        private ScrollModeEnum _ScrollMode;

        // the main drawing panel


        // panel for specifying parameters
        private int _ParametersMaxHeight;
        // max height of controls in _ParameterPanel

        private string _HighlightText = null;
        // text that should be highlighted when drawn
        private bool _HighlightCaseSensitive = false;
        // highlight text is case insensitive
        private bool _HighlightAll = false;
        // highlight all instances of Highlight text
        private PageItem _HighlightItem = null;
        // page item to highlight

        private bool _ShowParameters = true;
        private bool _ShowWaitDialog = true;
        // show wait dialog when running report

        public RdlViewer()
        {
            // CustomReportItem init 
            RdlEngineConfig.GetCustomReportTypes();

            this.InitializeComponent();
            _DrawPanel.BorderStyle = BorderStyle.None;
            _SourceFileName = null;
            _SourceRdl = null;
            _Parameters = null;             // parameters to run the report
            _pgs = null;                        // the pages of the report to view
            _loadFailed = false;
            _PageWidth = 0;
            _PageHeight = 0;
            _ReportDescription = null;
            _ReportAuthor = null;
            _ReportName = null;
            _zoom = -1;                     // force zoom to be calculated

            // Get our graphics DPI					   
            Graphics g = null;
            try
            {
                g = this.CreateGraphics();
                DpiX = g.DpiX;
                DpiY = g.DpiY;
            }
            catch
            {
                DpiX = DpiY = 96;
            }
            finally
            {
                if (g != null)
                    g.Dispose();
            }

            _ScrollMode = ScrollModeEnum.Continuous;

            // tooltip 
            _vScrollToolTip = new ToolTip();
            _vScrollToolTip.AutomaticDelay = 100;   // .1 seconds
            _vScrollToolTip.AutoPopDelay = 1000;    // 1 second
            _vScrollToolTip.ReshowDelay = 100;      // .1 seconds
            _vScrollToolTip.InitialDelay = 10;      // .01 seconds
            _vScrollToolTip.ShowAlways = false;
            _vScrollToolTip.SetToolTip(_vScroll, "");

            _DrawPanel.Parent = this;

            _DrawPanel.MouseWheel += new MouseEventHandler(DrawPanelMouseWheel);

            _WarningButton = new PictureBox();
            _WarningButton.Parent = this;
            _WarningButton.Width = 15;
            _WarningButton.Height = 15;
            _WarningButton.Paint += new PaintEventHandler(_WarningButton_Paint);
            _WarningButton.Click += new System.EventHandler(WarningClick);
            ToolTip tip = new ToolTip();
            tip.AutomaticDelay = 500;
            tip.ShowAlways = true;
            tip.SetToolTip(_WarningButton, "Click to see Report Warnings");

            _ParameterPanel = new ScrollableControl();

            _FindCtl = new RdlViewerFind();
            _FindCtl.Height = 27;
            _FindCtl.Parent = this;
            _FindCtl.Viewer = this;
            _FindCtl.Visible = false;

            this.Layout += new LayoutEventHandler(RdlViewer_Layout);
            this.SuspendLayout();

            // Must be added in this order for DockStyle to work correctly
            this.Controls.Add(_FindCtl);
            this.Controls.Add(_ParameterPanel);

            this.ResumeLayout(false);
        }

        public new bool Focus()
        {
            return (this._DrawPanel.Focus());
        }

        /// <summary>
        /// When true printing will compensate for non-printable area of paper
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseTrueMargins
        {
            get { return _UseTrueMargins; }
            set { _UseTrueMargins = value; }
        }

        /// <summary>
        /// Show the Wait Dialog when retrieving and rendering report when true.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowWaitDialog
        {
            get { return _ShowWaitDialog; }
            set { _ShowWaitDialog = value; }
        }

        /// <summary>
        /// True if Parameter panel should be shown. 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowParameterPanel
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                return _ShowParameters;
            }
            set
            {
                _ShowParameters = value;
                RdlViewer_Layout(this, null);               // re layout based on new report
            }
        }

        /// <summary>
        /// True when find panel is visible
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowFindPanel
        {
            get
            {
                return _FindCtl.Visible;
            }
            set
            {
                _FindCtl.Visible = value;
                RdlViewer_Layout(this, null);               // re layout based on new report
            }
        }

        /// <summary>
        /// Causes the find panel to find the next item
        /// </summary>
        public async Task FindNext()
        {
            await _FindCtl.FindNext();
        }

        /// <summary>
        /// The color to use when highlighting the current found item
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HighlightItemColor
        {
            get { return _DrawPanel.HighlightItemColor; }
            set { _DrawPanel.HighlightItemColor = value; }
        }

        /// <summary>
        /// The color to use when highlighting all
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HighlightAllColor
        {
            get { return _DrawPanel.HighlightAllColor; }
            set { _DrawPanel.HighlightAllColor = value; }
        }

        /// <summary>
        /// The text to highlight when either HighLightAll is on or the HighLightItem is on.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string HighlightText
        {
            get { return _HighlightText; }
            set
            {
                _HighlightText = value;
                _DrawPanel.Invalidate();    // force redraw
            }
        }

        /// <summary>
        /// When HighlightText has a value; HighlightAll controls whether
        /// all page items with that text will be highlighted
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HighlightAll
        {
            get { return _HighlightAll; }
            set
            {
                _HighlightAll = value;
                if (_HighlightText != null && _HighlightText.Length > 0)
                    _DrawPanel.Invalidate();    // force redraw when need to
            }
        }

        /// <summary>
        /// When HighlightText has a value; HighlightCaseSensitive controls whether
        /// the comparison is case sensitive.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HighlightCaseSensitive
        {
            get { return _HighlightCaseSensitive; }
            set
            {
                _HighlightCaseSensitive = value;
                if (_HighlightText != null && _HighlightText.Length > 0)
                    _DrawPanel.Invalidate();    // force redraw when need to
            }
        }

        /// <summary>
        /// When used with HighlightText; HighlightPageItem will only highlight the selected item.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PageItem HighlightPageItem
        {
            get { return _HighlightItem; }
            set
            {
                _HighlightItem = value;
                _DrawPanel.Invalidate();    // force redraw
            }
        }

        /// <summary>
        /// Returns the number of pages in the report.  0 is returned if no report has been loaded.
        /// </summary>
        public int PageCount
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                if (_pgs == null)
                    return 0;
                else
                    return _pgs.PageCount;
            }
        }

        /// <summary>
        /// Sets/Returns the page currently showing
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PageCurrent
        {
            get
            {
                if (_pgs == null)
                    return 0;
                int pc = (int)(_pgs.PageCount * ((long)_vScroll.Value + PageHeight) / (double)_vScroll.Maximum) + 1;
                if (pc > _pgs.PageCount)
                    pc = _pgs.PageCount;
                return pc;
            }
            set
            {
                if (_pgs == null)
                    return;
                // Contributed by Henrique (h2a) 07/14/2006
                if (value <= _pgs.PageCount && value >= 1)
                {
                    //					_vScroll.Value = (int)((double)_vScroll.Maximum / _pgs.PageCount * (value -1)); 

                    double scrollValue = ((double)_vScroll.Maximum * (value - 1)) / _pgs.PageCount;
                    _vScroll.Value = Math.Min((int)Math.Round(scrollValue), _vScroll.Maximum);

                    string tt = string.Format("Page {0} of {1}",
                                    (int)(_pgs.PageCount * (long)_vScroll.Value / (double)_vScroll.Maximum) + 1,
                                    _pgs.PageCount);

                    _vScrollToolTip.SetToolTip(_vScroll, tt);

                    _DrawPanel.Invalidate();
                    _DrawPanel.Refresh();
                    Refresh();
                    ChangePageEvent();
                }
                else
                    throw new ArgumentOutOfRangeException("PageCurrent", value, String.Format("Value must be between 1 and {0}.", _pgs.PageCount));
            }
        }

        /// <summary>
        /// Gets the report definition.
        /// </summary>
        public async Task<Report> Report()
        {
            await LoadPageIfNeeded();
            return _Report;    
        }

        /// <summary>
        /// Forces the report to get rebuilt especially after changing parameters or data.
        /// </summary>
        public async Task Rebuild()
        {
            // Aulofee customization - start. Code added (2 lines) to avoid to execute twice GetPages and so the SQL query (custo end). 
            if (_pgs == null)
            {
                await LoadPageIfNeeded();

                if (_Report == null)
                    throw new Exception(Strings.RdlViewer_Error_Report_must_be_loaded_prior_to_Rebuild_being_called);
                // Aulofee customization - start. Code added (2 lines) to avoid to execute twice GetPages and so the SQL query (custo end). 
            }
            else
            {
                _pgs = await GetPages(_Report);
            }
            _DrawPanel.Pgs = _pgs;
            _vScroll.Value = 0;
            CalcZoom();
            _DrawPanel.Invalidate();
        }

        /// <summary>
        /// Gets/Sets the ScrollMode.  
        ///		SinglePage: Shows a single page shows in pane.
        ///		Continuous: Shows pages as a continuous vertical column.
        ///		Facing: Shows first page on right side of pane, then alternating
        ///				with single page scrolling.
        ///		ContinuousFacing: Shows 1st page on right side of pane, then alternating 
        ///				with continuous scrolling.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ScrollModeEnum ScrollMode
        {
            get { return _ScrollMode; }
            set
            {
                _ScrollMode = value;
                CalcZoom();
                this._DrawPanel.Invalidate();
            }
        }

        /// <summary>
        /// Enables/Disables the selection tool.  The selection tool allows the user
        /// to select text and images on the display and copy it to the clipboard.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SelectTool
        {
            get { return _DrawPanel.SelectTool; }
            set { _DrawPanel.SelectTool = value; }
        }

        /// <summary>
        /// Returns true when one or more PageItems are selected.
        /// </summary>
        public bool CanCopy
        {
            get { return _DrawPanel.CanCopy; }
        }

        /// <summary>
        /// Copies the current selection (if any) to the clipboard.
        /// </summary>
        public void Copy()
        {
            if (!CanCopy)
                return;

            Image im = _DrawPanel.SelectImage;
            if (im == null)
                Clipboard.SetDataObject(SelectText, true);
            else
            {
                Clipboard.SetImage(im);
                im.Dispose();
            }
        }

        /// <summary>
        /// The contents of the selected text.  Tab separate items on same y coordinate;
        /// newline separate items when y coordinate changes.   Order is based on user
        /// selection order.
        /// </summary>
        public string SelectText
        {
            get
            {
                return _DrawPanel.SelectText;
            }
        }

        /// <summary>
        /// Holds a file name that contains the RDL (Report Specification Language).  Setting
        /// this field will cause a new report to be loaded into the viewer.
        /// SourceFile is mutually exclusive with SourceRdl.  Setting SourceFile will nullify SourceRdl.
        /// </summary>
        public Uri SourceFile
        {
            get
            {
                return _SourceFileName;
            }
        }

        public async Task SetSourceFile(Uri value)
        {
            _SourceFileName = value;
            if (value != null)
                _SourceRdl = null;
            _vScroll.Value = _hScroll.Value = 0;
            _pgs = null;                // reset pages, only if SourceRdl is also unavailable
            _DrawPanel.Pgs = null;
            _loadFailed = false;            // attempt to load the report
            if (this.Visible)
            {
                await LoadPageIfNeeded();         // force load of report
                this._DrawPanel.Invalidate();
            }
        }

        /// <summary>
        /// Holds the XML source of the report in RDL (Report Specification Language).
        /// SourceRdl is mutually exclusive with SourceFile.  Setting SourceRdl will nullify SourceFile.
        /// </summary>
        public string SourceRdl
        {
            get { return _SourceRdl; }
        }

        public async Task SetSourceRdl(string value)
        {
            _SourceRdl = value;
            if (value != null)
                _SourceFileName = null;
            _pgs = null;                // reset pages
            _DrawPanel.Pgs = null;
            _loadFailed = false;            // attempt to load the report	
            _vScroll.Value = _hScroll.Value = 0;
            if (this.Visible)
            {
                await LoadPageIfNeeded();
                this._DrawPanel.Invalidate();
            }
        }

        /// <summary>
        /// Do not force report loading 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SourceRdlNoLoad
        {
            get { return _SourceRdl; }
            set
            {
                _SourceRdl = value;
                if (value != null)
                    _SourceFileName = null;
                _pgs = null;                // reset pages
                _DrawPanel.Pgs = null;
                _loadFailed = false;            // attempt to load the report	
                _vScroll.Value = _hScroll.Value = 0;
                //if (this.Visible)
                //{
                //    LoadPageIfNeeded();         // force load of report
                //    this._DrawPanel.Invalidate();
                //}
            }
        }

        /// <summary>
        /// Holds the folder to data source reference files when SourceFileName not available.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Folder
        {
            get { return _Folder; }
            set { _Folder = value; }
        }

        /// <summary>
        /// Parameters passed to report when run.  Parameters are separated by '&'.  For example,
        /// OrderID=10023&OrderDate=10/14/2002
        /// Note: these parameters will override the user specified ones.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Parameters
        {
            get
            {
                string result = "";

                // Check for zero to working form designer
                if (_Parameters != null)
                {
                    foreach (DictionaryEntry kvp in _Parameters)
                    {
                        result += String.Format("{0:s}={1:s};", kvp.Key, kvp.Value);
                    }
                }

                return result.TrimEnd(';');
            }
            set
            {
                SetReportParametersAmpersandSeparated(value);
            }
        }

        #region Parameter setting methods
        public void SetReportParametersAmpersandSeparated(string parameterString)
        {
            _Parameters = new Dictionary<string, string>();
            if (String.IsNullOrEmpty(parameterString))
                return;
            String[] prms = parameterString.TrimEnd(';').Split('&');
            foreach (String p in prms)
            {
                int iEq = p.IndexOf("=");
                if (iEq > 0)
                {
                    string name = p.Substring(0, iEq);
                    string val = p.Substring(iEq + 1);
                    _Parameters.Add(name, val);
                }
            }
        }

        public void SetReportParametersAsJson(string jsonParameterString)
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonParameterString);
            SetReportParameters(dict);
        }
        
        public void SetReportParameters(IDictionary<string, string> parameters)
        {
            _Parameters = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                _Parameters.Add(parameter.Key, parameter.Value);
            }
        }
        #endregion

        /// <summary>
        /// The height of the report page (in points) as defined within the report.
        /// </summary>
        public float PageHeight
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                return _PageHeight;
            }
        }

        /// <summary>
        /// The width of the report page (in points) as defined within the report.
        /// </summary>
        public float PageWidth
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                return _PageWidth;
            }
        }

        /// <summary>
        /// Description of the report.
        /// </summary>
        public string ReportDescription
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                return _ReportDescription;
            }
        }

        /// <summary>
        /// Author of the report.
        /// </summary>
        public string ReportAuthor
        {
            get
            {
                // HACK: async
                Task.Run(async () => await LoadPageIfNeeded()).GetAwaiter().GetResult();
                return _ReportAuthor;
            }
        }

        /// <summary>
        /// Name of the report.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReportName
        {
            get
            {
                return _ReportName;
            }
            set { _ReportName = value; }
        }

        /// <summary>
        /// Zoom factor.  For example, .5 is a 50% reduction, 2 is 200% increase.
        /// Setting this value will force ZoomMode to UseZoom.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                this._zoomMode = ZoomEnum.UseZoom;
                CalcZoom();         // this adjust any scrolling issues
                this._DrawPanel.Invalidate();
            }
        }

        /// <summary>
        /// ZoomMode.  Optionally, allows zoom to dynamically change depending on pane size.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ZoomEnum ZoomMode
        {
            get { return _zoomMode; }
            set
            {
                _zoomMode = value;
                CalcZoom();             // force zoom calculation
                this._DrawPanel.Invalidate();
            }
        }

        /// <summary>
        /// Print the report.
        /// </summary>
        public async Task Print(PrintDocument pd)
        {
            await LoadPageIfNeeded();

            pd.PrintPage += new PrintPageEventHandler(PrintPage);

            // This is a work around where many printers do not support printing
            // more then one copy.  This will cause a prompt for each copy if using XPS
            //
            // http://msdn.microsoft.com/en-us/library/system.drawing.printing.printersettings.copies.aspx
            // "Not all printers support printing multiple copes. You can use the MaximumCopies property 
            // to determine the maximum number of copies the printer supports. If the number of 
            // copies is set higher than the maximum copies supported by the printer, only the 
            // maximum number of copies will be printed, and no exception will occur."
            // 
            if (pd.PrinterSettings.MaximumCopies == 1 && pd.PrinterSettings.Copies > 1)
            {
                for (int i = 0; i < pd.PrinterSettings.Copies; i++)
                {
                    _Print(pd);
                }
            }
            else
            {
                _Print(pd);
            }
        }

        private void _Print(PrintDocument pd)
        {
            printCurrentPage = -1;
            switch (pd.PrinterSettings.PrintRange)
            {
                case PrintRange.AllPages:
                    printCurrentPage = 0;
                    printEndPage = _pgs.PageCount - 1;
                    break;
                case PrintRange.Selection:
                    printCurrentPage = pd.PrinterSettings.FromPage - 1;
                    printEndPage = pd.PrinterSettings.FromPage - 1;
                    break;
                case PrintRange.SomePages:
                    printCurrentPage = pd.PrinterSettings.FromPage - 1;
                    if (printCurrentPage < 0)
                        printCurrentPage = 0;
                    printEndPage = pd.PrinterSettings.ToPage - 1;
                    if (printEndPage >= _pgs.PageCount)
                        printEndPage = _pgs.PageCount - 1;
                    break;
            }
            pd.Print();
        }

        private async void PrintPage(object sender, PrintPageEventArgs e)
        {
            System.Drawing.Rectangle r = new System.Drawing.Rectangle(0, 0, int.MaxValue, int.MaxValue);
            // account for the non-printable area of the paper
            PointF pageOffset;
            if (this.UseTrueMargins && this._Report != null)
            {
                // The page offset is set in pixels as the Draw method changes the graphics object to use pixels
                // (the origin transform does not get changed by the change in units.  PrintableArea returns
                // numbers in the hundredths of an inch.

                float x = ((e.PageSettings.PrintableArea.X * e.Graphics.DpiX) / 100.0F) - e.Graphics.Transform.OffsetX;
                float y = ((e.PageSettings.PrintableArea.Y * e.Graphics.DpiY) / 100.0F) - e.Graphics.Transform.OffsetY;

                // Get the margins in printer pixels (don't use the function!)
                // Points to pixels conversion ((double)x * DpiX / POINTSIZEF)
                float lm = (float)((double)_Report.LeftMarginPoints * e.Graphics.DpiX / POINTSIZEF);
                float tm = (float)((double)_Report.TopMarginPoints * e.Graphics.DpiY / POINTSIZEF);
                // Correct based on the report margin
                if (x > lm)      // left margin is less than the minimum left margin
                    x = 0;
                if (y > tm)      // top margin is less than the minimum top margin
                    y = 0;
                pageOffset = new PointF(-x, -y);
            }
            else
            {
                pageOffset = PointF.Empty;
            }

            await _DrawPanel.Draw(e.Graphics, printCurrentPage, r, false, pageOffset);

            printCurrentPage++;
            if (printCurrentPage > printEndPage)
                e.HasMorePages = false;
            else
                e.HasMorePages = true;
        }


        /// <summary>
        /// Save the file.  The extension determines the type of file to save.
        /// </summary>
        /// <param name="FileName">Name of the file to be saved to.</param>
        /// <param name="type">Type of file to save.  Should be "pdf", "xml", "html", "mhtml", "csv", "rtf", "excel", "tif".</param>
        public async Task SaveAs(string FileName, Majorsilence.Reporting.Rdl.OutputPresentationType type)
        {
            await LoadPageIfNeeded();


            OneFileStreamGen sg = new OneFileStreamGen(FileName, true); // overwrite with this name
            if (!(type == OutputPresentationType.PDF || type == OutputPresentationType.PDFOldStyle ||
                type == OutputPresentationType.TIF || type == OutputPresentationType.TIFBW))
            {
                var ld = GetParameters();        // split parms into dictionary
                await _Report.RunGetData(ld);                     // obtain the data (again)
            }
            try
            {
                switch (type)
                {
                    case OutputPresentationType.PDF:
                        _Report.ItextPDF = true;
                        await _Report.RunRenderPdf(sg, _pgs);
                        break;
                    case OutputPresentationType.PDFOldStyle:
                        _Report.ItextPDF = false;
                        await _Report.RunRenderPdf(sg, _pgs);
                        break;
                    case OutputPresentationType.TIF:
                        await _Report.RunRenderTif(sg, _pgs, true);
                        break;
                    case OutputPresentationType.TIFBW:
                        await _Report.RunRenderTif(sg, _pgs, false);
                        break;
                    case OutputPresentationType.CSV:
                        await _Report.RunRender(sg, OutputPresentationType.CSV);
                        break;
                    case OutputPresentationType.Word:
                    case OutputPresentationType.RTF:
                        await _Report.RunRender(sg, OutputPresentationType.RTF);
                        break;
                    case OutputPresentationType.ExcelTableOnly:
                        await _Report.RunRender(sg, OutputPresentationType.ExcelTableOnly);
                        break;
                    case OutputPresentationType.Excel2007:
                        await _Report.RunRender(sg, OutputPresentationType.Excel2007);
                        break;
                    case OutputPresentationType.XML:
                        await _Report.RunRender(sg, OutputPresentationType.XML);
                        break;
                    case OutputPresentationType.HTML:
                        await _Report.RunRender(sg, OutputPresentationType.HTML);
                        break;
                    case OutputPresentationType.MHTML:
                        await _Report.RunRender(sg, OutputPresentationType.MHTML);
                        break;
                    default:
                        throw new Exception(Strings.RdlViewer_Error_UnsupportedExtension);
                }
            }
            finally
            {
                if (sg != null)
                {
                    sg.CloseMainStream();
                }


            }
            return;
        }

        /// <summary>
        /// Finds the first instance of the search string.
        /// </summary>
        /// <param name="search"></param>
        /// <returns>null if not found</returns>
        public async Task<PageItem> Find(string search)
        {
            return await Find(search, null, RdlViewerFinds.None);
        }

        /// <summary>
        /// Find locates the next string after the passed location.  Use ScrollToPageItem to then
        /// reposition the Viewer on that item
        /// </summary>
        /// <param name="search">Text to search for</param>
        /// <param name="position">PageItem after which to start search.  null starts at beginning</param>
        /// <param name="options">Multiple options can be or'ed together.</param>
        /// <returns>null if not found</returns>
        public async Task<PageItem> Find(string search, PageItem position, RdlViewerFinds options)
        {
            await LoadPageIfNeeded();

            if (_pgs == null || _pgs.Count == 0)       // no report nothing to find
                return null;

            // initialize the loop direction and starting point
            int increment;
            int sPage;
            int sItem;
            if (((options & RdlViewerFinds.Backward) == RdlViewerFinds.Backward))
            {   // set to backward direction
                increment = -1;                 // go backwards
                sPage = _pgs.PageCount - 1;     // start at last page
                sItem = _pgs[sPage].Count - 1;  // start at bottom of last page
            }
            else
            {   // set to forward direction
                increment = 1;
                sPage = 0;
                sItem = 0;
            }

            bool bFirst = true;
            if (position != null)
            {
                sPage = position.Page.PageNumber - 1;   // start on same page as current
                sItem = position.ItemNumber + increment;  //   but on the item after/before the current one
            }

            if (!((options & RdlViewerFinds.MatchCase) == RdlViewerFinds.MatchCase))
                search = search.ToLower();          // should use Culture!!! todo

            PageItem found = null;
            for (int pi = sPage; pi < _pgs.Count && found == null && pi >= 0; pi = pi + increment)
            {
                Page p = _pgs[pi];
                if (bFirst)         // The first time sItem is already set
                    bFirst = false;
                else
                {
                    if (increment < 0)  // we're going backwards?
                        sItem = p.Count - 1;    // yes, start at bottom of page
                    else
                        sItem = 0;              // no, start at top of page
                }
                for (int pii = sItem; pii < p.Count && found == null && pii >= 0; pii = pii + increment)
                {
                    PageText pt = p[pii] as PageText;
                    if (pt == null)
                        continue;

                    if ((options & RdlViewerFinds.MatchCase) == RdlViewerFinds.MatchCase)
                    {
                        if (pt.Text.Contains(search))
                            found = pt;
                    }
                    else
                    {
                        if (pt.Text.ToLower().Contains(search))
                            found = pt;
                    }
                }
            }

            return found;
        }

        public async Task ScrollToPageItem(PageItem pi)
        {
            await LoadPageIfNeeded();
            if (_pgs == null || _pgs.PageCount <= 0)    // nothing to scroll to
                return;

            int sPage = 0;
            int sItem = 0;
            int itemVerticalOffset = 0;
            int itemHorzOffset = 0;
            int height = 0;
            int width = 0;
            if (pi != null)
            {
                sPage = pi.Page.PageNumber - 1;
                sItem = pi.ItemNumber;
                RectangleF rect = new RectangleF(PixelsX(pi.X + _leftMargin),
                                      PixelsY(pi.Y),
                                      PixelsX(pi.W),
                                      PixelsY(pi.H));
                itemVerticalOffset = (int)(rect.Top);
                itemHorzOffset = (int)rect.Left;
                width = (int)rect.Width;
                height = (int)(rect.Height);
            }

            // set the vertical scroll
            int scroll = (int)((double)_vScroll.Maximum * sPage / _pgs.PageCount) + itemVerticalOffset;

            // do we need to scroll vertically?
            if (!(_vScroll.Value <= scroll && _vScroll.Value + _DrawPanel.Height / this.Zoom >= scroll + height))
            {   // item isn't on visible part of window; force scroll
                int maxScroll = Math.Max(_vScroll.Minimum, _vScroll.Maximum - _DrawPanel.Height);
                _vScroll.Value = Math.Min(Math.Max(scroll, _vScroll.Minimum), Math.Min(maxScroll, _vScroll.Maximum));
                SetScrollControlsV();
                ScrollEventArgs sa = new ScrollEventArgs(ScrollEventType.ThumbPosition, _vScroll.Maximum + 1); // position is intentionally wrong
                VerticalScroll(_vScroll, sa);
            }

            // set the horizontal scroll
            scroll = itemHorzOffset;

            // do we need to scroll horizontally?
            if (!(_hScroll.Value <= scroll && _hScroll.Value + _DrawPanel.Width / this.Zoom >= scroll + width))
            {   // item isn't on visible part of window; force scroll
                int maxScroll = Math.Max(_hScroll.Minimum, _hScroll.Maximum - _DrawPanel.Width);
                _hScroll.Value = Math.Min(Math.Max(scroll, _hScroll.Minimum), Math.Min(maxScroll, _hScroll.Maximum));
                SetScrollControlsH();
                ScrollEventArgs sa = new ScrollEventArgs(ScrollEventType.ThumbPosition, _hScroll.Maximum + 1); // position is intentionally wrong
                HorizontalScroll(_hScroll, sa);
            }
        }

        private Bitmap _buffer;
        // HACK: async shenanigans
        bool doGraphicsDraw;
        private async void DrawPanelPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            try         // never want to die in here
            {
                if (doGraphicsDraw && _buffer != null)
                {            
                    e.Graphics.DrawImage(_buffer, 0, 0);
                    _buffer.Dispose();
                    _buffer = null;            
                }
                else
                {
                    await LoadPageIfNeeded();             // make sure we have something to show

                    if (_zoom < 0)
                        CalcZoom();             // new report or resize client requires new zoom factor

                    // Draw the page
                    _buffer = new Bitmap(Math.Max(1, _DrawPanel.Width), Math.Max(1, _DrawPanel.Height));
                    using (Graphics g = Graphics.FromImage(_buffer))
                    {
                        await _DrawPanel.Draw(g, _zoom, _leftMargin, _pageGap,
                                PointsX(_hScroll.Value), PointsY(_vScroll.Value),
                                e.ClipRectangle,
                                _HighlightItem, _HighlightText, _HighlightCaseSensitive, _HighlightAll);                         
                    }

                    doGraphicsDraw = true;
                    _DrawPanel.Invalidate();       // force a redraw
                }         
            }
            catch (Exception ex)
            {   // don't want to kill process if we die
                using (Font font = new Font("Arial", 8))
                    e.Graphics.DrawString(ex.Message + "\r\n" + ex.StackTrace, font, Brushes.Black, 0, 0);
            }
        }

        private void DrawPanelResize(object sender, EventArgs e)
        {
            CalcZoom();                         // calc zoom
            _DrawPanel.Invalidate();
        }

        private float POINTSIZEF = 72.27f;

        private float PointsX(float x)      // pixels to points
        {
            return x * POINTSIZEF / DpiX;
        }

        private float PointsY(float y)
        {
            return y * POINTSIZEF / DpiY;
        }

        private int PixelsX(float x)        // points to pixels
        {
            int r = (int)((double)x * DpiX / POINTSIZEF);
            if (r == 0 && x > .0001f)
                r = 1;
            return r;
        }

        private int PixelsY(float y)
        {
            int r = (int)((double)y * DpiY / POINTSIZEF);
            if (r == 0 && y > .0001f)
                r = 1;
            return r;
        }

        private void CalcZoom()
        {
            switch (_zoomMode)
            {
                case ZoomEnum.UseZoom:
                    if (_zoom <= 0)			// normalize invalid values
                        _zoom = 1;
                    break;                  // nothing to calculate
                case ZoomEnum.FitWidth:
                    CalcZoomFitWidth();
                    break;
                case ZoomEnum.FitPage:
                    CalcZoomFitPage();
                    break;
            }
            if (_zoom <= 0)
                _zoom = 1;
            float w = PointsX(_DrawPanel.Width);    // convert to points

            if (w > (this._PageWidth + _leftGap + _rightGap) * _zoom)
                _leftMargin = ((w - (this._PageWidth + _leftGap + _rightGap) * _zoom) / 2) / _zoom;
            else
                _leftMargin = _leftGap;
            if (_leftMargin < 0)
                _leftMargin = 0;
            SetScrollControls();            // zoom affects the scroll bars
            return;
        }

        private void CalcZoomFitPage()
        {
            try
            {
                float w = PointsX(_DrawPanel.Width);    // convert to points
                float h = PointsY(_DrawPanel.Height);
                float xratio = w / (this._PageWidth + _leftGap + _rightGap);
                float yratio = h / (this._PageHeight + this._pageGap + this._pageGap);
                _zoom = Math.Min(xratio, yratio);
            }
            catch
            {
                _zoom = 1;          // shouldn't ever happen but this routine must never throw exception
            }
        }

        private void CalcZoomFitWidth()
        {
            try
            {
                float w = PointsX(_DrawPanel.Width);    // convert to points
                float h = PointsY(_DrawPanel.Height);
                _zoom = w / (this._PageWidth + _leftGap + _rightGap);

            }
            catch
            {
                _zoom = 1;          // shouldn't ever happen but this routine must never throw exception
            }
        }

        // Obtain the Pages by running the report
        private async Task<Report> GetReport()
        {
            string prog;

            // Obtain the source
            if (_loadFailed)
                prog = GetReportErrorMsg();
            else if (_SourceRdl != null)
                prog = _SourceRdl;
            else if (_SourceFileName != null)
                prog = GetRdlSource();
            else
                prog = GetReportEmptyMsg();

            // Compile the report
            // Now parse the file
            RDLParser rdlp;
            Report r;
            try
            {
                _errorMsgs = null;
                rdlp = new RDLParser(prog);
                rdlp.DataSourceReferencePassword = GetDataSourceReferencePassword;
                if (_SourceFileName != null)
                    rdlp.Folder = Path.GetDirectoryName(_SourceFileName.LocalPath);
                else
                    rdlp.Folder = this.Folder;

                // EBN 31/03/2014
                // Cross objects
                rdlp.OnSubReportGetContent.SubReportGetContent = dSubReportGetContent;

                r = await rdlp.Parse();
                if (r.ErrorMaxSeverity > 0)
                {
                    _errorMsgs = r.ErrorItems;      // keep a copy of the errors

                    int severity = r.ErrorMaxSeverity;
                    r.ErrorReset();
                    if (severity > 4)
                    {
                        r = null;           // don't return when severe errors
                        _loadFailed = true;
                    }
                }
                // If we've loaded the report; we should tell it where it got loaded from
                if (r != null && !_loadFailed)
                {   // Don't care much if this fails; and don't want to null out report if it does
                    try
                    {
                        if (_SourceFileName != null)
                        {
                            r.Name = Path.GetFileNameWithoutExtension(_SourceFileName.LocalPath);
                            r.Folder = Path.GetDirectoryName(_SourceFileName.LocalPath);
                        }
                        else
                        {
                            r.Folder = this.Folder;
                            r.Name = this.ReportName;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                _errorMsgs = new List<string>();        // create new error list
                _errorMsgs.Add(ex.Message);         // put the message in it
                _errorMsgs.Add(ex.StackTrace);      //   and the stack trace
                r = null;
            }

            if (r != null)
            {
                _PageWidth = r.PageWidthPoints;
                _PageHeight = r.PageHeightPoints;
                _ReportDescription = r.Description;
                _ReportAuthor = r.Author;
                r.SubreportDataRetrieval += new EventHandler<SubreportDataRetrievalEventArgs>(r_SubreportDataRetrieval);
                ParametersBuild(r);
            }
            else
            {
                _PageWidth = 0;
                _PageHeight = 0;
                _ReportDescription = null;
                _ReportAuthor = null;
                _ReportName = null;
            }
            return r;
        }

        void r_SubreportDataRetrieval(object sender, SubreportDataRetrievalEventArgs e)
        {
            if (this.SubreportDataRetrieval != null)
                SubreportDataRetrieval(this, e);
        }

        private string GetReportEmptyMsg()
        {
            string prog = "<Report><Width>8.5in</Width><Body><Height>1in</Height><ReportItems><Textbox><Value></Value><Style><FontWeight>Bold</FontWeight></Style><Height>.3in</Height><Width>5 in</Width></Textbox></ReportItems></Body></Report>";
            return prog;
        }

        private string GetReportErrorMsg()
        {
            string data1 = @"<?xml version='1.0' encoding='UTF-8'?>
<Report> 
	<LeftMargin>.4in</LeftMargin><Width>8.5in</Width>
	<Author></Author>
	<DataSources>
		<DataSource Name='DS1'>
			<ConnectionProperties> 
				<DataProvider>xxx</DataProvider>
				<ConnectString></ConnectString>
			</ConnectionProperties>
		</DataSource>
	</DataSources>
	<DataSets>
		<DataSet Name='Data'>
			<Query>
				<DataSourceName>DS1</DataSourceName>
			</Query>
			<Fields>
				<Field Name='Error'> 
					<DataField>Error</DataField>
					<TypeName>String</TypeName>
				</Field>
			</Fields>";

            string data2 = @"
		</DataSet>
	</DataSets>
	<PageHeader>
		<Height>1 in</Height>
		<ReportItems>
			<Textbox><Top>.1in</Top><Value>Majorsilence Reporting</Value><Style><FontSize>18pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
			<Textbox><Top>.1in</Top><Left>4.25in</Left><Value>=Globals!ExecutionTime</Value><Style><Format>dddd, MMMM dd, yyyy hh:mm:ss tt</Format><FontSize>12pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
			<Textbox><Top>.5in</Top><Value>Errors processing report</Value><Style><FontSize>12pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
		</ReportItems>
	</PageHeader>
	<Body><Height>3 in</Height>
		<ReportItems>
			<Table>
				<Style><BorderStyle>Solid</BorderStyle></Style>
				<TableColumns>
					<TableColumn><Width>7 in</Width></TableColumn>
				</TableColumns>
				<Header>
					<TableRows>
						<TableRow>
							<Height>15 pt</Height>
							<TableCells>
								<TableCell>
									<ReportItems><Textbox><Value>Messages</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems>
								</TableCell>
							</TableCells>
						</TableRow>
					</TableRows>
					<RepeatOnNewPage>true</RepeatOnNewPage>
				</Header>
				<Details>
					<TableRows>
						<TableRow>
							<Height>12 pt</Height>
							<TableCells>
								<TableCell>
									<ReportItems><Textbox Name='ErrorMsg'><Value>=Fields!Error.Value</Value><CanGrow>true</CanGrow></Textbox></ReportItems>
								</TableCell>
							</TableCells>
						</TableRow>
					</TableRows>
				</Details>
			</Table>
		</ReportItems>
	</Body>
</Report>";

            StringBuilder sb = new StringBuilder(data1, data1.Length + data2.Length + 1000);
            // Build out the error messages
            sb.Append("<Rows>");
            foreach (string msg in _errorMsgs)
            {
                sb.Append("<Row><Error>");
                string newmsg = msg.Replace("&", @"&amp;");
                newmsg = newmsg.Replace("<", @"&lt;");
                sb.Append(newmsg);
                sb.Append("</Error></Row>");
            }
            sb.Append("</Rows>");
            sb.Append(data2);
            return sb.ToString();
        }

        private async Task<Pages> GetPages()
        {
            this._Report = await GetReport();
            if (_loadFailed)			// retry on failure; this will get error report
                this._Report = await GetReport();

            return await GetPages(this._Report);
        }

        private async Task<Pages> GetPages(Report report)
        {
            Pages pgs = null;

            var ld = GetParameters();        // split parms into dictionary

            try
            {
                await report.RunGetData(ld);

                pgs = await report.BuildPages();

                if (report.ErrorMaxSeverity > 0)
                {
                    if (_errorMsgs == null)
                    {
                        _errorMsgs = report.ErrorItems;     // keep a copy of the errors
                    }
                    else
                    {
                        foreach (string err in report.ErrorItems)
                        {
                            _errorMsgs.Add(err);
                        }
                    }

                    report.ErrorReset();
                }

            }
            catch (Exception e)
            {
                string msg = e.Message;
            }

            return pgs;
        }

        private IDictionary GetParameters()
        {
            // If we have a loaded report with user parameters, use those runtime values
            // instead of the _Parameters dictionary which may be stale
            if (_Report != null && _Report.UserReportParameters != null && _Report.UserReportParameters.Count > 0)
            {
                Dictionary<string, object> runtimeParams = new Dictionary<string, object>();
                foreach (UserReportParameter urp in _Report.UserReportParameters)
                {
                    // Skip null values as they would use the parameter's default value anyway
                    // and we want to let the report engine handle defaults properly
                    if (urp.Value != null)
                    {
                        runtimeParams[urp.Name] = urp.Value;
                    }
                }
                // Merge with any parameters from _Parameters that aren't in UserReportParameters
                if (_Parameters != null)
                {
                    foreach (DictionaryEntry kvp in _Parameters)
                    {
                        string key = kvp.Key.ToString();
                        if (!runtimeParams.ContainsKey(key))
                        {
                            runtimeParams[key] = kvp.Value;
                        }
                    }
                }
                return runtimeParams;
            }
            return _Parameters;
        }

        private void SetParameterValue(String key, String value)
        {
            if (_Parameters.Contains(key))
                _Parameters[key] = value;
            else
                _Parameters.Add(key, value);
        }

        private string GetRdlSource()
        {
            StreamReader fs = null;
            string prog = null;
            try
            {
                fs = new StreamReader(_SourceFileName.LocalPath);
                prog = fs.ReadToEnd();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            prog = doPossibleDecryption(prog);

            return prog;
        }

        private String doPossibleDecryption(String rdl)
        {

            if (Path.GetExtension(_SourceFileName.LocalPath).Equals(".encrypted"))
            {

                try
                {
                    StringEncryption enc = new StringEncryption(Prompt.ShowDialog("Please enter the passkey", "Passkey?"));
                    rdl = enc.Decrypt(rdl);
                }
                catch (Exception)
                {
                    MessageBox.Show(Strings.RdlViewer_doPossibleDecryption_Incorrect_Pass_key_entered_);
                }
            }

            return rdl;
        }

        // 15052008 AJM - Updating Render notification window - This could be improved to show current action in the future
        private void showWait()
        {
            try
            {
                this.ShowWaiter();
            }
            catch (ObjectDisposedException)
            {
                // Just let it go
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.RdlViewer_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Call LoadPageIfNeeded when a routine requires the report to be loaded in order
        /// to fulfill the request.
        /// </summary>
        private async Task LoadPageIfNeeded()
        {
            if (_pgs == null)
            {
                Cursor savec = null;
                try
                {
                    // 15052008 AJM - Updating Render notification window - This could be improved to show current action in the future
                    if (_ShowWaitDialog)
                    {
                        showWait();
                    }

                    savec = this.Cursor;                // this could take a while so put up wait cursor
                    this.Cursor = Cursors.WaitCursor;              
                    _pgs = await GetPages();
                    _DrawPanel.Pgs = _pgs;
                    CalcZoom();                         // this could affect zoom
                    _DrawPanel.Invalidate();
                }
                finally
                {
                    this.HideWaiter();
                    if (savec != null)
                        this.Cursor = savec;
                }
                RdlViewer_Layout(this, null);               // re layout based on new report
            }
        }

        private void ParametersBuild(Report r)
        {
            // Remove all previous controls
            _ParameterPanel.Controls.Clear();
            _ParameterPanel.AutoScroll = true;

            int yPos = 10;
            foreach (UserReportParameter rp in r.UserReportParameters)
            {
                if (rp.Prompt == null || rp.Prompt.Length == 0)		// skip parameters that don't have a prompt
                    continue;

                // Create a label
                Label label = new Label();
                label.Parent = _ParameterPanel;
                label.AutoSize = true;
                label.Text = rp.Prompt;
                label.Location = new Point(10, yPos);

                // Create a control
                Control v;
                int width = 90;
                if (rp.DisplayValues == null)
                {
                    TextBox tb = new TextBox();
                    v = tb;
                    tb.Height = tb.PreferredHeight;
                    tb.Validated += new System.EventHandler(ParametersTextValidated);
                }
                else
                {
                    ComboBox cb = new ComboBox();
                    // create a label to auto
                    Label l = new Label();
                    l.AutoSize = true;
                    l.Visible = false;

                    cb.Leave += new EventHandler(ParametersLeave);
                    v = cb;
                    width = 0;
                    foreach (string s in rp.DisplayValues)
                    {
                        l.Text = s;
                        if (width < l.Width)
                            width = l.Width;
                        cb.Items.Add(s);
                    }
                    if (width > 0)
                    {
                        l.Text = "XX";
                        width += l.Width;       // give some extra room for the drop down arrow
                    }
                    else
                        width = 90;             // just force the default
                }
                v.Parent = _ParameterPanel;
                v.Width = width;
                v.Location = new Point(label.Location.X + label.Width + 5, yPos);
                if (rp.DefaultValue != null)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < rp.DefaultValue.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");
                        sb.Append(rp.DefaultValue[i].ToString());
                    }
                    v.Text = sb.ToString();
                }
                v.Tag = rp;

                yPos += Math.Max(label.Height, v.Height) + 5;
            }

            this._ParametersMaxHeight = yPos;
        }

        private void ParametersLeave(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null)
                return;

            UserReportParameter rp = cb.Tag as UserReportParameter;
            if (rp == null)
                return;

            try
            {
                rp.Value = cb.Text;
            }
            catch (ArgumentException ae)
            {
                MessageBox.Show(ae.Message, Strings.RdlViewer_ParametersLeave_InvalidReportParameter);
            }
        }

        private void ParametersTextValidated(object sender, System.EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null)
                return;

            UserReportParameter rp = tb.Tag as UserReportParameter;
            if (rp == null)
                return;

            try
            {
                rp.Value = tb.Text;
            }
            catch (ArgumentException ae)
            {
                MessageBox.Show(ae.Message, Strings.RdlViewer_ParametersLeave_InvalidReportParameter);
            }
        }

        private async void ParametersViewClick(object sender, System.EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _RunButton.Enabled = false;
                _errorMsgs = null;          // reset the error message
                if (this._Report == null)
                {
                    return;
                }

                // Force parameters to get built
                foreach (Control ctl in _ParameterPanel.Controls)
                {
                    if (ctl.Tag is UserReportParameter)
                    {
                        if (ctl is TextBox)
                        {
                            this.ParametersTextValidated(ctl, new EventArgs());
                        }
                        else if (ctl is ComboBox)
                        {
                            this.ParametersLeave(ctl, new EventArgs());
                        }
                    }
                }

                bool bFail = false;
                foreach (UserReportParameter rp in _Report.UserReportParameters)
                {
                    if (rp.Prompt == null)
                        continue;
                    if (rp.Value == null && !rp.Nullable)
                    {
                        MessageBox.Show(string.Format(Strings.RdlViewer_ParametersViewClick_RequiredParameterNotProvided, String.IsNullOrEmpty(rp.Prompt) ? rp.Name : rp.Prompt), Strings.RdlViewer_ParametersViewClick_ReportParameterMissing);
                        bFail = true;
                    }
                }
                if (bFail)
                {
                    return;
                }
                
                showWait();
                  
                _pgs = await GetPages(this._Report);
                _DrawPanel.Pgs = _pgs;
                _vScroll.Value = 0;
                CalcZoom();
                _WarningButton.Visible = WarningVisible();
                _DrawPanel.Invalidate();
            }
            catch
            {
                // don't fail out;  occasionally get thread abort exception
            }
            finally
            {
                this.HideWaiter();
                _RunButton.Enabled = true;
                Cursor.Current = Cursors.Default;

               
            }
        }

        private void WarningClick(object sender, System.EventArgs e)
        {
            if (_errorMsgs == null)
                return;                     // shouldn't even be visible if no warnings

            DialogMessages dm = new DialogMessages(_errorMsgs);
            dm.ShowDialog();
            return;
        }

        private void SetScrollControls()
        {
            if (_pgs == null)       // nothing loaded; nothing to do
            {
                _vScroll.Enabled = _hScroll.Enabled = false;
                _vScroll.Value = _hScroll.Value = 0;
                return;
            }
            SetScrollControlsV();
            SetScrollControlsH();
        }

        private void SetScrollControlsV()
        {
            // calculate the vertical scroll needed
            float h = PointsY(_DrawPanel.Height);   // height of pane
            if (_zoom * ((this._PageHeight + this._pageGap) * _pgs.PageCount + this._pageGap) <= h)
            {
                _vScroll.Enabled = false;
                _vScroll.Value = 0;
                return;
            }
            _vScroll.Minimum = 0;
            _vScroll.Maximum = (int)(PixelsY((this._PageHeight + this._pageGap) * _pgs.PageCount + this._pageGap));
            _vScroll.Value = Math.Min(_vScroll.Value, _vScroll.Maximum);
            if (this._zoomMode == ZoomEnum.FitPage)
            {
                _vScroll.LargeChange = (int)(_vScroll.Maximum / _pgs.PageCount);
                _vScroll.SmallChange = _vScroll.LargeChange;
            }
            else
            {
                _vScroll.LargeChange = (int)(Math.Max(_DrawPanel.Height, 0) / _zoom);
                _vScroll.SmallChange = _vScroll.LargeChange / 5;
            }
            _vScroll.Enabled = true;
            string tt = string.Format("Page {0} of {1}",
                            (int)(_pgs.PageCount * (long)_vScroll.Value / (double)_vScroll.Maximum) + 1,
                            _pgs.PageCount);

            _vScrollToolTip.SetToolTip(_vScroll, tt);
            //			switch (_ScrollMode)
            //			{
            //				case ScrollModeEnum.SinglePage:
            //					break;
            //				case ScrollModeEnum.Continuous:
            //				case ScrollModeEnum.ContinuousFacing:
            //				case ScrollModeEnum.Facing:
            //					break;
            //			}
            return;
        }

        private void SetScrollControlsH()
        {
            // calculate the horizontal scroll needed
            float w = PointsX(_DrawPanel.Width);    // width of pane
            if (_zoomMode == ZoomEnum.FitPage ||
                _zoomMode == ZoomEnum.FitWidth ||
                _zoom * (this._PageWidth + this._leftGap + this._rightGap) <= w)
            {
                _hScroll.Enabled = false;
                _hScroll.Value = 0;
                return;
            }

            _hScroll.Minimum = 0;
            _hScroll.Maximum = (int)(PixelsX(this._PageWidth + this._leftGap + this._rightGap));
            _hScroll.Value = Math.Min(_hScroll.Value, _hScroll.Maximum);
            _hScroll.LargeChange = (int)(Math.Max(_DrawPanel.Width, 0) / _zoom);
            _hScroll.SmallChange = _hScroll.LargeChange / 5;
            _hScroll.Enabled = true;

            return;
        }

        private void HorizontalScroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (_hScroll.IsDisposed)
                return;

            if (e.NewValue == _hScroll.Value)	// don't need to scroll if already there
                return;

            _DrawPanel.Invalidate();
        }

        private void VerticalScroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (_vScroll.IsDisposed)
                return;

            if (e.NewValue == _vScroll.Value)	// don't need to scroll if already there
                return;

            string tt = string.Format("Page {0} of {1}",
                            (int)(_pgs.PageCount * (long)_vScroll.Value / (double)_vScroll.Maximum) + 1,
                            _pgs.PageCount);

            _vScrollToolTip.SetToolTip(_vScroll, tt);

            _DrawPanel.Invalidate();
            ChangePageEvent();
        }

        private int previousPage = 1;

        private void ChangePageEvent()
        {

            if (PageNavigation == null)
            {
                return;
            }

            int currentPage = PageCurrent;

            if (previousPage != currentPage)
            {
                PageNavigation(this, new PageNavigationEventArgs(currentPage));
            }

            previousPage = currentPage;

        }

        private void DrawPanelMouseWheel(object sender, MouseEventArgs e)
        {
            int wvalue;
            bool bCtrlOn = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            if (bCtrlOn)
            {   // when ctrl key on and wheel rotated we zoom in or out
                float zoom = Zoom;

                if (e.Delta < 0)
                {
                    zoom -= .1f;
                    if (zoom < .1f)
                        zoom = .1f;
                }
                else
                {
                    zoom += .1f;
                    if (zoom > 10)
                        zoom = 10;
                }
                Zoom = zoom;
                _DrawPanel.Refresh();
                return;
            }

            if (e.Delta < 0)
            {
                if (_vScroll.Value < _vScroll.Maximum)
                {
                    wvalue = _vScroll.Value + _vScroll.SmallChange;

                    //Changed from forum, User: robertopisati http://www.fyireporting.com/forum/viewtopic.php?t=863
                    int maxScroll = (int)Math.Max(_vScroll.Minimum, _vScroll.Maximum - (_DrawPanel.Height / _zoom));
                    _vScroll.Value = Math.Min(Math.Max(wvalue, _vScroll.Minimum), Math.Min(maxScroll, _vScroll.Maximum));
                    _DrawPanel.Refresh();
                }
            }
            else
            {
                if (_vScroll.Value > _vScroll.Minimum)
                {
                    wvalue = _vScroll.Value - _vScroll.SmallChange;

                    _vScroll.Value = Math.Max(_vScroll.Minimum, wvalue);
                    _DrawPanel.Refresh();
                }
            }

            ChangePageEvent();
        }

        private async void DrawPanelKeyDown(object sender, KeyEventArgs e)
        {
            // Force scroll up and down
            if (e.KeyCode == Keys.Down)
            {
                if (!_vScroll.Enabled)
                    return;
                int wvalue = _vScroll.Value + _vScroll.SmallChange;
                int maxScroll = (int)Math.Max(_vScroll.Minimum, _vScroll.Maximum - (_DrawPanel.Height / _zoom));
                _vScroll.Value = Math.Min(Math.Max(wvalue, _vScroll.Minimum), Math.Min(maxScroll, _vScroll.Maximum));
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = Math.Max(_vScroll.Value - _vScroll.SmallChange, _vScroll.Minimum);
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (!_vScroll.Enabled)
                    return;
                int maxScroll = Math.Max(_vScroll.Minimum, _vScroll.Maximum - _DrawPanel.Height);
                _vScroll.Value = Math.Min(Math.Max(_vScroll.Value + _vScroll.LargeChange, _vScroll.Minimum), Math.Min(maxScroll, _vScroll.Maximum));
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = Math.Max(_vScroll.Value - _vScroll.LargeChange, _vScroll.Minimum);
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = _vScroll.Minimum;
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                if (!_vScroll.Enabled)
                    return;
                if (_pgs != null && _pgs.Count > 0)
                {
                    Page last = _pgs[_pgs.Count - 1];
                    if (last.Count > 0)
                    {
                        PageItem lastItem = last[last.Count - 1];
                        await this.ScrollToPageItem(lastItem);
                        e.Handled = true;
                    }
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (!_hScroll.Enabled)
                    return;
                if (e.Control)
                    _hScroll.Value = _hScroll.Minimum;
                else
                    _hScroll.Value = Math.Max(_hScroll.Value - _hScroll.SmallChange, _hScroll.Minimum);
                _DrawPanel.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (!_hScroll.Enabled)
                    return;
                if (e.Control)
                {
                    int maxScroll = Math.Max(_hScroll.Minimum, _hScroll.Maximum - _DrawPanel.Width);
                    _hScroll.Value = Math.Min(maxScroll, _hScroll.Maximum);
                }
                else
                {
                    int maxScroll = Math.Max(_hScroll.Minimum, _hScroll.Maximum - _DrawPanel.Width);
                    _hScroll.Value = Math.Min(Math.Max(_hScroll.Value + _hScroll.SmallChange, _hScroll.Minimum), Math.Min(maxScroll, _hScroll.Maximum));
                }
                _DrawPanel.Refresh();
                e.Handled = true;
            }

        }

        private bool WarningVisible()
        {
            if (!_ShowParameters)
                return false;

            return _errorMsgs != null;
        }

        private void RdlViewer_Layout(object sender, LayoutEventArgs e)
        {
            int fHeight = _FindCtl.Visible ? _FindCtl.Height : 0;
            int pHeight;
            if (_ShowParameters)
            {   // Only the parameter panel is visible
                _ParameterPanel.Visible = true;
                _RunButton.Visible = true;

                _WarningButton.Visible = WarningVisible();

                _ParameterPanel.Location = new Point(0, 0);
                _ParameterPanel.Width = this.Width - _RunButton.Width - _WarningButton.Width - 5;
                pHeight = this.Height / 3;
                if (pHeight > _ParametersMaxHeight)
                    pHeight = _ParametersMaxHeight;
                if (pHeight < _RunButton.Height + 15)
                    pHeight = _RunButton.Height + 15;
                _ParameterPanel.Height = pHeight;
            }
            else
            {
                //				pHeight=_RunButton.Height + 15;
                pHeight = 0;
                _RunButton.Visible = false;
                _WarningButton.Visible = false;
                _ParameterPanel.Visible = false;
            }
            _DrawPanel.Location = new Point(0, pHeight);
            _DrawPanel.Width = this.Width - _vScroll.Width;
            _DrawPanel.Height = this.Height - _hScroll.Height - pHeight - fHeight;
            _hScroll.Location = new Point(0, this.Height - _hScroll.Height - fHeight);
            _hScroll.Width = _DrawPanel.Width;
            _vScroll.Location = new Point(this.Width - _vScroll.Width, _DrawPanel.Location.Y);
            _vScroll.Height = _DrawPanel.Height;

            if (_FindCtl.Visible)
            {
                _FindCtl.Location = new Point(0, this.Height - _FindCtl.Height);
                _FindCtl.Width = this.Width;
                _FindCtl.BringToFront();
            }

            _RunButton.Location = new Point(this.Width - _RunButton.Width - 2 - _WarningButton.Width, 10);
            _WarningButton.Location = new Point(_RunButton.Location.X + _RunButton.Width + 2, 13);
        }

        private void _WarningButton_Paint(object sender, PaintEventArgs e)
        {
            int midPoint = _WarningButton.Width / 2;
            Graphics g = e.Graphics;

            Point[] triangle = new Point[5];
            triangle[0] = triangle[4] = new Point(midPoint - 1, 0);
            triangle[1] = new Point(0, _WarningButton.Height - 1);
            triangle[2] = new Point(_WarningButton.Width, _WarningButton.Height - 1);
            triangle[3] = new Point(midPoint + 1, 0);
            g.FillPolygon(Brushes.Yellow, triangle);
            g.DrawPolygon(Pens.Black, triangle);
            g.FillRectangle(Brushes.Red, midPoint - 1, 5, 2, 5);
            g.FillRectangle(Brushes.Red, midPoint - 1, 11, 2, 2);
        }

        internal void InvokeHyperlink(HyperlinkEventArgs hlea)
        {
            if (Hyperlink != null)
                Hyperlink(this, hlea);
        }

        public void HideRunButton()
        {
            _RunButton.Visible = false;
        }

        public void ShowRunButton()
        {
            _RunButton.Visible = true;
        }
    }

    public enum RdlViewerFinds
    {
        None = 0,
        MatchCase = 1,
        Backward = 2
    }

    public enum ScrollModeEnum
    {
        SinglePage,
        Continuous,
        Facing,
        ContinuousFacing
    }

    public enum ZoomEnum
    {
        UseZoom,
        FitPage,
        FitWidth
    }

    /// <summary>
    /// HyperlinkEventArgs passed when a report item with a hyperlink defined is clicked on
    /// </summary>
    public class HyperlinkEventArgs : System.ComponentModel.CancelEventArgs
    {
        string _Hyperlink;
        // Hyperlink text
        public HyperlinkEventArgs(string hyperlink)
            : base()
        {
            _Hyperlink = hyperlink;
        }

        public string Hyperlink
        {
            get { return _Hyperlink; }
        }
    }

    public class PageNavigationEventArgs : EventArgs
    {
        public PageNavigationEventArgs(int newPage)
            : base()
        {
            _newPage = newPage;
        }

        private int _newPage;

        public int NewPage
        {
            get
            {
                return _newPage;
            }
        }

    }


}
