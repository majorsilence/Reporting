using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlViewer;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Globalization;
using System.Xml;
using Majorsilence.Forms.Printing;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class RdlUserControl : UserControl
    {


        private Rdl.NeedPassword _GetPassword;
        private bool bGotPassword = false;
        private string _DataSourceReferencePassword = null;
        public delegate void RdlChangeHandler(object sender, EventArgs e);

        private ToolStripButton ctlInsertCurrent = null;
        private ToolStripMenuItem ctlMenuInsertCurrent = null;
        bool bSuppressChange = false;
        private bool _ShowProperties = true;

        public event RdlDesign.RdlDesignerSavedFileEventHandler SavedFileEvent;

        /// <summary>
        /// Raised when the user changes the report. Loading a report (OpenFile, SourceFile, SourceRdl)
        /// does not raise it; check Modified to see whether there are unsaved changes.
        /// </summary>
        public event RdlChangeHandler RdlChanged;

        /// <summary>
        /// Raised by <see cref="ShowDataSourcesDialog"/> before the built-in "Manage Data Sources" dialog
        /// opens (issue #469). A host that sets <see cref="DesignerCommandEventArgs.Handled"/> to <c>true</c>
        /// can show its own UI instead; the native dialog is skipped entirely. No subscribers means identical
        /// behavior to before this event existed.
        /// </summary>
        public event EventHandler<DesignerCommandEventArgs> DataSourcesPropertiesRequested;

        /// <summary>
        /// Raised by <see cref="ShowDataSetDialog"/> before the built-in dataset-properties dialog opens
        /// (issue #469), naming the dataset involved. A host that sets
        /// <see cref="DesignerCommandEventArgs.Handled"/> to <c>true</c> can show its own UI instead; the
        /// native dialog, and the undo/XML bookkeeping around it, is skipped entirely. No subscribers means
        /// identical behavior to before this event existed.
        /// </summary>
        public event EventHandler<DesignerDataSetCommandEventArgs> DataSetPropertiesRequested;

        /// <summary>
        /// Gives this designer a theme of its own -- a built-in theme name (Default, Light, Dark, Classic,
        /// Aero) or <see cref="DesignerTheme.Custom"/> with a CSS file -- leaving the rest of the host
        /// application on its theme. Creates the control's <see cref="Control.ThemeScope"/> when it has none.
        /// Never throws for a bad theme; see <see cref="DesignerTheme.Apply"/>.
        /// </summary>
        public DesignerThemeResult UseTheme(string theme, string cssPath = null)
            => DesignerTheme.ApplyTo(EnsureThemeScope(), theme, cssPath);

        /// <summary>As <see cref="UseTheme"/>, from CSS theme text (Majorsilence.Forms docs/theming.md, plus <c>--rdl-*</c> variables).</summary>
        public DesignerThemeResult UseThemeCss(string css)
            => DesignerTheme.ApplyCssTo(EnsureThemeScope(), css);

        private ThemeScope EnsureThemeScope() => ThemeScope ??= new ThemeScope();

        public RdlUserControl()
        {
            InitializeComponent();


            _GetPassword = new Rdl.NeedPassword(this.GetPassword);


            rdlEditPreview1.OnSelectionChanged += SelectionChanged;
            rdlEditPreview1.OnReportItemInserted += ReportItemInserted;
            rdlEditPreview1.OnInsertCancelled += ReportItemInserted;
            rdlEditPreview1.OnOpenSubreport += OpenSubReportEvent;
            rdlEditPreview1.OnSelectionMoved += SelectionMoved;
            rdlEditPreview1.OnDesignTabChanged += DesignTabChanged;
            rdlEditPreview1.OnRdlChanged += (sender, e) => RdlChanged?.Invoke(this, e);
            mainProperties.HidePropertiesClicked += delegate(object sender, EventArgs e)
            {
                ShowProperties(!_ShowProperties);
            };
        }


        /// <summary>
        /// Open a file programmatically when the designer is already open.
        /// </summary>
        /// <param name="filePath">The full path to the rdl report.</param>
        /// <example>
        /// An example of opening a designer form and loading one report.
        /// <code lang="cs">
        /// Majorsilence.Reporting.RdlDesign.RdlDesigner designer = new Majorsilence.Reporting.RdlDesign.RdlDesigner("myFyiChannel");
        /// designer.Show();
        /// designer.OpenFile(@"Path\to\a\report.rdl");
        /// </code>
        /// <code lang="vb">
        /// Dim designer As New Majorsilence.Reporting.RdlDesign.RdlDesigner("myFyiChannel")
        /// designer.Show() 
        /// designer.OpenFile("Path\to\a\report.rdl")
        /// </code>
        /// </example>
        /// <remarks>
        /// You can open as many reports as you want by calling this function. The only limitation is that
        /// the designer must already be running by having called the Show() function first.
        /// </remarks>
        public void OpenFile(string filePath)
        {
            this.OpenFile(new Uri(filePath));
        }
        public void OpenFile(Uri filePath)
        {
            OpenReport(filePath, null);
        }

        public Majorsilence.Reporting.RdlViewer.RdlViewer Viewer
        {
            get { return rdlEditPreview1.Viewer; }
        }

        internal void ResetPassword()
        {
            bGotPassword = false;
            _DataSourceReferencePassword = null;
        }

        Uri _SourceFile;
        /// <summary>
        /// The RDL file that should be displayed.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Uri SourceFile
        {
            get { return _SourceFile; }
            set
            {
                _SourceFile = value;
                string rdl = GetRdlSource();
                this.rdlEditPreview1.SetRdlText(rdl == null ? "" : rdl);
            }
        }

        private string GetRdlSource()
        {
            StreamReader fs = null;
            string prog = null;
            try
            {
   

                fs = new StreamReader(_SourceFile.LocalPath);
                prog = fs.ReadToEnd();
            }
            catch
            { }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            return prog;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SourceRdl
        {
            get { return this.rdlEditPreview1.GetRdlText(); }
            set { this.rdlEditPreview1.SetRdlText(value); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentInsert
        {
            get { return rdlEditPreview1.CurrentInsert; }
            set
            {
                rdlEditPreview1.CurrentInsert = value;
            }
        }

        private IDesignerIconProvider _IconProvider;

        /// <summary>
        /// Lets a host application override individual toolbar/menu icons on this designer instance,
        /// keeping the built-in defaults for any icon it doesn't override (issue #468). No provider (the
        /// default) means identical behavior to before this property existed.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDesignerIconProvider IconProvider
        {
            get { return _IconProvider; }
            set
            {
                _IconProvider = value;
                DesignerIconResolver.Apply(value, mainTB, toolStrip1);
            }
        }

        internal string GetPassword()
        {
            if (bGotPassword)
            {
                return _DataSourceReferencePassword;
            }

            using (DataSourcePassword dlg = new DataSourcePassword())
            {
                DialogResult rc = dlg.ShowDialog();
                bGotPassword = true;
                if (rc == DialogResult.OK)
                {
                    _DataSourceReferencePassword = dlg.PassPhrase;
                }

                return _DataSourceReferencePassword;
            }
        }

        internal Rdl.NeedPassword SharedDatasetPassword
        {
            get { return _GetPassword; }
        }

        /// <summary>
        /// Parameters preset in every report created with the New Report wizard.
        /// </summary>
        public List<DefaultReportParameter> NewReportParameters { get; } = new List<DefaultReportParameter>();

        /// <summary>
        /// Optional connection preset in the New Report wizard.
        /// </summary>
        public string NewReportConnectionString { get; set; }
        public bool NewReportHideConnectionTab { get; set; }
        public DialogDatabase.ConnectionType NewReportConnectionType { get; set; } = DialogDatabase.ConnectionType.MSSQL;

        private void newToolStripButton1_Click(object sender, EventArgs e)
        {
            using (DialogDatabase dlgDB = new DialogDatabase(this))
            {
                dlgDB.StartPosition = FormStartPosition.CenterParent;
                if (!string.IsNullOrEmpty(NewReportConnectionString))
                    dlgDB.SetConnection(NewReportConnectionString, NewReportHideConnectionTab, NewReportConnectionType);
                dlgDB.SetParameters(NewReportParameters);
                dlgDB.FormBorderStyle = FormBorderStyle.SizableToolWindow;

                // show modally
                dlgDB.ShowDialog();
                if (dlgDB.DialogResult == DialogResult.Cancel)
                    return;
                string rdl = dlgDB.ResultReport;

                // Create the MDI child using the RDL syntax the wizard generates
                LoadReport(null, rdl, false);
         

            }
        }

        public bool FileSave()
        {
            Uri file = SourceFile;
            if (file == null || file.LocalPath == "")		// if no file name then do SaveAs
            {
                return FileSaveAs();
            }
            string rdl = GetRdlText();

            return FileSave(file, rdl);
        }


        private bool FileSave(Uri file, string rdl)
        {
            StreamWriter writer = null;
            bool bOK = true;
            try
            {
                writer = new StreamWriter(file.LocalPath);
                writer.Write(rdl);
                //				editRDL.ClearUndo();
                //				editRDL.Modified = false;
                //				SetTitle();
                //				statusBar.Text = "Saved " + curFileName;
            }
            catch (Exception ae)
            {
                bOK = false;
                MessageBox.Show(this, ae.Message + "\r\n" + ae.StackTrace);
                //				statusBar.Text = "Save of file '" + curFileName + "' failed";
            }
            finally
            {
                writer.Close();
            }
            if (bOK)
                this.Modified = false;
            return bOK;
        }

        public string GetRdlText()
        {
            return this.rdlEditPreview1.GetRdlText();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Modified
        {
            get { return rdlEditPreview1.Modified; }
            set
            {
                rdlEditPreview1.Modified = value;
                SetTitle();
            }
        }

        private void SetTitle()
        {
            string title = this.Text;
            if (title.Length < 1)
                return;
            char m = title[title.Length - 1];
            if (this.Modified)
            {
                if (m != '*')
                    title = title + "*";
            }
            else if (m == '*')
                title = title.Substring(0, title.Length - 1);

            if (title != this.Text)
                this.Text = title;
            return;
        }


        // Create an MDI child.   Only creates it if not already open.
        private void LoadReport(Uri file, string rdl, bool bMenuUpdate)
        {

     
            try
            {



                Viewer.GetDataSourceReferencePassword = _GetPassword;
                if (file != null)
                {
                    Viewer.Folder = System.IO.Path.GetDirectoryName(file.LocalPath);
                    SourceFile = file;
                    Text = System.IO.Path.GetFileName(file.LocalPath);
                    Viewer.Folder = System.IO.Path.GetDirectoryName(file.LocalPath);
                    Viewer.ReportName = System.IO.Path.GetFileNameWithoutExtension(file.LocalPath);
                }
                else
                {
                    SourceRdl = rdl;
                    Viewer.ReportName = Text = Strings.RdlDesigner_CreateMDIChild_Untitled;
                }

                ShowEditLines(true);
                ShowReportItemOutline = false;
                ShowPreviewWaitDialog(true);



            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }

       
 
        }

        public void ShowEditLines(bool bShow)
        {
            rdlEditPreview1.ShowEditLines(bShow);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool ShowReportItemOutline
        {
            get { return rdlEditPreview1.ShowReportItemOutline; }
            set { rdlEditPreview1.ShowReportItemOutline = value; }
        }

        internal void ShowPreviewWaitDialog(bool bShow)
        {
            rdlEditPreview1.ShowPreviewWaitDialog(bShow);
        }


        /// <summary>
        /// Zoom 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return rdlEditPreview1.Zoom; }
            set { rdlEditPreview1.Zoom = value; }
        }

        /// <summary>
        /// ZoomMode 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ZoomEnum ZoomMode
        {
            get { return rdlEditPreview1.ZoomMode; }
            set { rdlEditPreview1.ZoomMode = value; }
        }

        private void ReportItemInserted(object sender, System.EventArgs e)
        {

            // turn off the current selection after an item is inserted
            if (ctlInsertCurrent != null)
            {
                ctlInsertCurrent.Checked = false;
                CurrentInsert = null;
                ctlInsertCurrent = null;
            }
            if (ctlMenuInsertCurrent != null)
            {
                ctlMenuInsertCurrent.Checked = false;
                CurrentInsert = null;
                ctlMenuInsertCurrent = null;
            }
        }

        private void OpenSubReportEvent(object sender, SubReportEventArgs e)
        {
  
            Uri file = new Uri(this.Viewer.Folder);
            if (e.SubReportName[0] == Path.DirectorySeparatorChar)
            {
                file = new Uri(file.LocalPath + e.SubReportName);
            }
            else
            {
                file = new Uri(file.LocalPath + Path.DirectorySeparatorChar + e.SubReportName + ".rdl");
            }

            LoadReport(file, null, true);
        }

        private void SelectionMoved(object sender, System.EventArgs e)
        {
            SetStatusNameAndPosition();
        }

        private async void SelectionChanged(object sender, System.EventArgs e)
        {
            // handle edit tab first
            if (rdlEditPreview1.DesignTab == DesignTabs.Edit)
            {
                SetStatusNameAndPosition();
                return;
            }

            bSuppressChange = true;			// don't process changes in status bar

            SetStatusNameAndPosition();
            this.EnableEditTextBox();	// handling enabling/disabling of textbox

            StyleInfo si = await rdlEditPreview1.GetSelectedStyleAsync();
            if (si == null)
               {
                bSuppressChange = false;   // the early exit used to leave it set
                return;
               }

            if (centerAlignToolStripButton2 != null)
                centerAlignToolStripButton2.Checked = si.TextAlign == TextAlignEnum.Center ? true : false;
            if (leftAlignToolStripButton2 != null)
                leftAlignToolStripButton2.Checked = si.TextAlign == TextAlignEnum.Left ? true : false;
            if (rightAlignToolStripButton3 != null)
                rightAlignToolStripButton3.Checked = si.TextAlign == TextAlignEnum.Right ? true : false;
            if (boldToolStripButton1 != null)
                boldToolStripButton1.Checked = si.IsFontBold() ? true : false;
            if (italiacToolStripButton1 != null)
                italiacToolStripButton1.Checked = si.FontStyle == FontStyleEnum.Italic ? true : false;
            if (underlineToolStripButton2 != null)
                underlineToolStripButton2.Checked = si.TextDecoration == TextDecorationEnum.Underline ? true : false;
            if (fontToolStripComboBox1 != null)
                fontToolStripComboBox1.Text = si.FontFamily;
            if (fontSizeToolStripComboBox1 != null)
            {
                string rs = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.#}", si.FontSize);
                fontSizeToolStripComboBox1.Text = rs;
            }
            if (foreColorPicker1 != null)
            {
                foreColorPicker1.Text = si.Color.IsEmpty ? si.ColorText : Majorsilence.Forms.ColorTranslator.ToHtml(si.Color);
            }
            if (backColorPicker1 != null)
            {
                backColorPicker1.Text = si.BackgroundColor.IsEmpty ? si.BackgroundColorText : Majorsilence.Forms.ColorTranslator.ToHtml(si.BackgroundColor);
            }

            bSuppressChange = false;
        }

        private void DesignTabChanged(object sender, System.EventArgs e)
        {
            bool bEnableEdit = false;
            bool bEnableDesign = false;
            bool bEnablePreview = false;
            bool bShowProp = _ShowProperties;
            switch (rdlEditPreview1.DesignTab)
            {
                case DesignTabs.Edit:
                    bEnableEdit = true;
                    break;
                case DesignTabs.Design:
                    bEnableDesign = true;
                    break;
                case DesignTabs.Preview:
                    bEnablePreview = true;
                    break;
            }
 
            ShowProperties(bShowProp);
            if (leftAlignToolStripButton2 != null)
            {
                leftAlignToolStripButton2.Enabled = bEnableDesign;
            }
            if (centerAlignToolStripButton2 != null)
            {
                centerAlignToolStripButton2.Enabled = bEnableDesign;
            }
            if (rightAlignToolStripButton3 != null)
            {
                rightAlignToolStripButton3.Enabled = bEnableDesign;
            }
            if (boldToolStripButton1 != null)
            {
                boldToolStripButton1.Enabled = bEnableDesign;
            }
            if (italiacToolStripButton1 != null)
            {
                italiacToolStripButton1.Enabled = bEnableDesign;
            }
            if (underlineToolStripButton2 != null)
            {
                underlineToolStripButton2.Enabled = bEnableDesign;
            }
            if (fontToolStripComboBox1 != null)
            {
                fontToolStripComboBox1.Enabled = bEnableDesign;
            }
            if (fontSizeToolStripComboBox1 != null)
            {
                fontSizeToolStripComboBox1.Enabled = bEnableDesign;
            }
            if (foreColorPicker1 != null)
            {
                foreColorPicker1.Enabled = bEnableDesign;
            }
            if (backColorPicker1 != null)
            {
                backColorPicker1.Enabled = bEnableDesign;
            }
            if (cutToolStripButton1 != null)
            {
                cutToolStripButton1.Enabled = bEnableDesign | bEnableEdit;
            }
            if (copyToolStripButton1 != null)
            {
                copyToolStripButton1.Enabled = bEnableDesign | bEnableEdit | bEnablePreview;
            }
            if (undoToolStripButton1 != null)
            {
                undoToolStripButton1.Enabled = bEnableDesign | bEnableEdit;
            }
            if (pasteToolStripButton1 != null)
            {
                pasteToolStripButton1.Enabled = bEnableDesign | bEnableEdit;
            }
            if (printToolStripButton2 != null)
            {
                printToolStripButton2.Enabled = bEnablePreview;
            }
            if (textboxToolStripButton1 != null)
            {
                textboxToolStripButton1.Enabled = bEnableDesign;
            }
            if (selectToolStripButton2 != null)
            {
                selectToolStripButton2.Enabled = bEnablePreview;
                selectToolStripButton2.Checked = SelectionTool;
            }
            if (chartToolStripButton1 != null)
            {
                chartToolStripButton1.Enabled = bEnableDesign;
            }
            if (rectangleToolStripButton1 != null)
            {
                rectangleToolStripButton1.Enabled = bEnableDesign;
            }
            if (tableToolStripButton1 != null)
            {
                tableToolStripButton1.Enabled = bEnableDesign;
            }
            if (matrixToolStripButton1 != null)
            {
                matrixToolStripButton1.Enabled = bEnableDesign;
            }
            if (listToolStripButton1 != null)
            {
                listToolStripButton1.Enabled = bEnableDesign;
            }
            if (lineToolStripButton1 != null)
            {
                lineToolStripButton1.Enabled = bEnableDesign;
            }
            if (imageToolStripButton1 != null)
            {
                imageToolStripButton1.Enabled = bEnableDesign;
            }
            if (subreportToolStripButton1 != null)
            {
                subreportToolStripButton1.Enabled = bEnableDesign;
            }
            if (pdfToolStripButton2 != null)
            {
                pdfToolStripButton2.Enabled = bEnablePreview;
            }
            if (TifToolStripButton2 != null)
            {
                TifToolStripButton2.Enabled = bEnablePreview;
            }
            if (XmlToolStripButton2 != null)
            {
                XmlToolStripButton2.Enabled = bEnablePreview;
            }
            if (htmlToolStripButton2 != null)
            {
                htmlToolStripButton2.Enabled = bEnablePreview;
            }
            if (MhtToolStripButton2 != null)
            {
                MhtToolStripButton2.Enabled = bEnablePreview;
            }
            if (CsvToolStripButton2 != null)
            {
                CsvToolStripButton2.Enabled = bEnablePreview;
            }
            if (excelToolStripButton2 != null)
            {
                excelToolStripButton2.Enabled = bEnablePreview;
            }
            if (RtfToolStripButton2 != null)
            {
                RtfToolStripButton2.Enabled = bEnablePreview;
            }
            
            this.EnableEditTextBox();

            if (zoomToolStripComboBox1 != null)
            {
                zoomToolStripComboBox1.Enabled = bEnablePreview;
                string zText = "Actual Size";

                switch (ZoomMode)
                {
                    case ZoomEnum.FitWidth:
                        zText = "Fit Width";
                        break;
                    case ZoomEnum.FitPage:
                        zText = "Fit Page";
                        break;
                    case ZoomEnum.UseZoom:
                        if (Zoom == 1)
                            zText = "Actual Size";
                        else
                            zText = string.Format("{0:0}", Zoom * 100f);
                        break;
                }
                zoomToolStripComboBox1.Text = zText;
                
            }
            // when no active sheet
            if (this.saveToolStripButton1 != null)
                this.saveToolStripButton1.Enabled = true;

            // Update the status and position information
            SetStatusNameAndPosition();
        }

        private void EnableEditTextBox()
        {

            bool bEnable = false;

            if (this.ctlEditTextbox == null ||
              rdlEditPreview1.DesignTab != DesignTabs.Design || rdlEditPreview1.DrawCtl.SelectedCount != 1)
            { }
            else
            {
                XmlNode tn = rdlEditPreview1.DrawCtl.SelectedList[0] as XmlNode;
                if (tn != null && tn.Name == "Textbox")
                {
                    ctlEditTextbox.Text = rdlEditPreview1.DrawCtl.GetElementValue(tn, "Value", "");
                    bEnable = true;
                }
            }
            if (ctlEditTextbox != null)
            {
                if (!bEnable)
                    ctlEditTextbox.Text = "";
                ctlEditTextbox.Enabled = bEnable;
                fxToolStripLabel1.Enabled = bEnable;
            }
        }

        private void SetStatusNameAndPosition()
        {

            SetProperties();


            return;
        }


        private void SetProperties()
        {
            if (!_ShowProperties || rdlEditPreview1.DesignTab != DesignTabs.Design)
                mainProperties.ResetSelection(null, null);
            else
                mainProperties.ResetSelection(rdlEditPreview1.DrawCtl, rdlEditPreview1.DesignCtl);
        }

        internal void ShowProperties(bool bShow)
        {
            _ShowProperties = bShow;
            if (!_ShowProperties || rdlEditPreview1.DesignTab != DesignTabs.Design)
                mainProperties.ResetSelection(null, null);
            else
                mainProperties.ResetSelection(rdlEditPreview1.DrawCtl, rdlEditPreview1.DesignCtl);

            mainProperties.Visible = splitContainer1.Panel2.Visible = _ShowProperties;
            if (splitContainer1.Panel2.Visible == false)
            {
                splitContainer1.Panel2Collapsed = true;
                splitContainer1.Panel2.Hide();
                ButtonShowProperties.Visible = true;
            }
            else 
            {
                ButtonShowProperties.Visible = false;
                splitContainer1.Panel2Collapsed = false;
                splitContainer1.Panel2.Show();
            }

        }

        public void ApplyStyleToSelected(string name, string v)
        {

            rdlEditPreview1.ApplyStyleToSelected(name, v);
        }

        /// <summary>
        /// Turns the Selection Tool on in report preview
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SelectionTool
        {
            get
            {
                return rdlEditPreview1.SelectionTool;
            }
            set
            {
                rdlEditPreview1.SelectionTool = value;
            }
        }

        public async Task<bool> ExportAsync(Rdl.OutputPresentationType type)
        {
            SaveFileDialog sfd = new SaveFileDialog();
			sfd.Title = string.Format(Strings.MDIChild_Export_ExportTitleFormat, type.ToString().ToUpper());
			switch (type)
            {
				case OutputPresentationType.CSV:
					sfd.Filter = Strings.MDIChild_Export_CSV;
					break;
				case OutputPresentationType.XML:
					sfd.Filter = Strings.MDIChild_Export_XML;
					break;
				case OutputPresentationType.PDF:
				case OutputPresentationType.PDFOldStyle:
					sfd.Filter = Strings.MDIChild_Export_PDF;
					break;
				case OutputPresentationType.TIF:
					sfd.Filter = Strings.MDIChild_Export_TIF;
					break;
				case OutputPresentationType.RTF:
					sfd.Filter = Strings.MDIChild_Export_RTF;
					break;
				case OutputPresentationType.Word:
					sfd.Filter = Strings.MDIChild_Export_DOC;
					break;
				case OutputPresentationType.ExcelTableOnly:
				case OutputPresentationType.Excel2007:
					sfd.Filter = Strings.MDIChild_Export_Excel;
					break;
				case OutputPresentationType.HTML:
					sfd.Filter = Strings.MDIChild_Export_Web_Page;
					break;
				case OutputPresentationType.MHTML:
					sfd.Filter = Strings.MDIChild_Export_MHT;
					break;
                default:
                    throw new Exception(Strings.MDIChild_Error_AllowedExportTypes);
            }
            sfd.FilterIndex = 1;

            if (SourceFile != null)
            {
                sfd.FileName = Path.GetFileNameWithoutExtension(SourceFile.LocalPath) + "." + type.ToString().ToLower();
            }
            else
            {
                sfd.FileName = "*." + type.ToString().ToLower();
            }

            try
            {
                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return false;

                // save the report in the requested rendered format 
                bool rc = true;
                // tif can be either in color or black and white; ask user what they want
                if (type == OutputPresentationType.TIF)
                {
                    DialogResult dr = MessageBox.Show(this, Strings.MDIChild_ShowF_WantDisplayColorsInTIF, Strings.MDIChild_ShowF_Export, MessageBoxButtons.YesNoCancel);
                    if (dr == DialogResult.No)
                        type = OutputPresentationType.TIFBW;
                    else if (dr == DialogResult.Cancel)
                        return false;
                }
                try { await SaveAsAsync(sfd.FileName, type); }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        ex.Message, Strings.MDIChild_ShowG_ExportError,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    rc = false;
                }
                return rc;
            }
            finally
            {
                sfd.Dispose();
            }
        }

        public async Task SaveAsAsync(string filename, OutputPresentationType type)
        {
            await rdlEditPreview1.SaveAs(filename, type);
        }

        public int PageCount
        {
            get { return rdlEditPreview1.PageCount; }
        }


        /// <summary>
        /// Page height of the report.
        /// </summary>
        public float PageHeight
        {
            get { return rdlEditPreview1.PageHeight; }
        }


        /// <summary>
        /// Page width of the report.
        /// </summary>
        public float PageWidth
        {
            get { return rdlEditPreview1.PageWidth; }
        }

        /// <summary>
        /// Current page in view on report
        /// </summary>
        public int PageCurrent
        {
            get { return rdlEditPreview1.PageCurrent; }
        }

        // Print(PrintDocument) removed -- rdlEditPreview1.Print(PrintDocument) no longer exists
        // per D2/D4's printing redesign (see MIGRATION-NOTES.md). Use SaveAsAsync(path,
        // OutputPresentationType.PDF) instead; see printToolStripButton2_Click for the caller-side
        // change.


        private void openToolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            try
            {
                ofd.InitialDirectory = Path.GetDirectoryName(SourceFile.LocalPath);
            }
            catch
            {
            }

            ofd.DefaultExt = "rdl";
            ofd.Filter = Strings.RdlUserControl_openToolStripButton1_Click_ReportFilesFilter;
            ofd.FilterIndex = 1;
            ofd.CheckFileExists = true;
            ofd.Multiselect = true;
            try
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
   
                    OpenReport(new Uri(ofd.FileName), null);

                }
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private void OpenReport(Uri file, string rdl)
        {

            try
            {
              
  
                Viewer.GetDataSourceReferencePassword = _GetPassword;
                if (file != null)
                {
                    Viewer.Folder = Path.GetDirectoryName(file.LocalPath);
                    SourceFile = file;
                    Text = Path.GetFileName(file.LocalPath);
                    Viewer.Folder = Path.GetDirectoryName(file.LocalPath);
                    Viewer.ReportName = Path.GetFileNameWithoutExtension(file.LocalPath);
                }
                else
                {
                    SourceRdl = rdl;
                    Viewer.ReportName = Text = "Untitled";
                }
                ShowEditLines(true);
                ShowReportItemOutline = this.ShowReportItemOutline;
                ShowPreviewWaitDialog(false);


            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }


        }

        public bool FileSaveAs()
        {
            SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = Strings.MDIChild_FileSaveAs_RDLFilter;
            sfd.FilterIndex = 1;

            Uri file = SourceFile;

            sfd.FileName = file == null ? "*.rdl" : file.LocalPath;
            try
            {
                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return false;

                // User wants to save!
                string rdl = GetRdlText();
                if (FileSave(new Uri(sfd.FileName), rdl))
                {	// Save was successful
                    Text = sfd.FileName;
                    _SourceFile = new Uri(sfd.FileName);
                    return true;
                }
            }
            finally
            {
                sfd.Dispose();
            }
            return false;
        }

        /// <summary>
        /// Opens the built-in "Manage Data Sources" dialog for the loaded report (issue #469/#470). Unlike
        /// the stand-alone <c>RdlDesigner</c> shell, <see cref="RdlUserControl"/> has no menu or toolbar
        /// button of its own for this -- a host wires its own UI to call this method.
        /// Raises <see cref="DataSourcesPropertiesRequested"/> first; if the host sets
        /// <c>Handled = true</c> there, the native dialog is skipped.
        /// </summary>
        public void ShowDataSourcesDialog()
        {
            var cmdArgs = new DesignerCommandEventArgs();
            DataSourcesPropertiesRequested?.Invoke(this, cmdArgs);
            if (cmdArgs.Handled)
                return;

            rdlEditPreview1.StartUndoGroup(Strings.RdlDesigner_Undo_DataSourcesDialog);
            using (DialogDataSources dlgDS = new DialogDataSources(SourceFile, rdlEditPreview1.DrawCtl))
            {
                dlgDS.StartPosition = FormStartPosition.CenterParent;
                DialogResult dr = dlgDS.ShowDialog(this);
                rdlEditPreview1.EndUndoGroup(dr == DialogResult.OK);
                if (dr == DialogResult.OK)
                    Modified = true;
            }
        }

        /// <summary>
        /// The names of every DataSet currently defined on the loaded report, in document order -- lets a
        /// host build its own "manage dataset" menu/list to call <see cref="ShowDataSetDialog"/> with
        /// (mirroring what <c>RdlDesigner</c>'s own Data &gt; Datasets submenu lists).
        /// </summary>
        public string[] GetDataSetNames()
        {
            DesignXmlDraw draw = rdlEditPreview1.DrawCtl;
            if (draw == null || rdlEditPreview1.ReportDocument == null)
                return Array.Empty<string>();

            XmlNode rNode = draw.GetReportNode();
            XmlNode dsNode = draw.GetNamedChildNode(rNode, "DataSets");
            if (dsNode == null)
                return Array.Empty<string>();

            List<string> names = new List<string>();
            foreach (XmlNode dNode in dsNode)
            {
                if (dNode.Name != "DataSet")
                    continue;
                XmlAttribute nAttr = dNode.Attributes["Name"];
                if (nAttr == null)
                    continue;
                names.Add(nAttr.Value);
            }
            return names.ToArray();
        }

        /// <summary>
        /// Opens the built-in dataset-properties dialog for the loaded report (issue #469/#470), the same
        /// dialog <c>RdlDesigner</c>'s Data &gt; Datasets submenu opens. Pass the name of an existing
        /// DataSet (see <see cref="GetDataSetNames"/>) to edit it, or <c>null</c>/empty to create a new one.
        /// Raises <see cref="DataSetPropertiesRequested"/> first, naming the dataset involved; if the host
        /// sets <c>Handled = true</c> there, the native dialog (and the undo/XML bookkeeping around it) is
        /// skipped entirely.
        /// </summary>
        public void ShowDataSetDialog(string dataSetName = null)
        {
            DesignXmlDraw draw = rdlEditPreview1.DrawCtl;
            if (draw == null || rdlEditPreview1.ReportDocument == null)
                return;

            string label = string.IsNullOrEmpty(dataSetName) ? Strings.RdlDesigner_menuData_Popup_New : dataSetName;

            var cmdArgs = new DesignerDataSetCommandEventArgs(label);
            DataSetPropertiesRequested?.Invoke(this, cmdArgs);
            if (cmdArgs.Handled)
                return;

            rdlEditPreview1.StartUndoGroup(Strings.RdlDesigner_Undo_DataSetDialog);

            List<XmlNode> ds = new List<XmlNode>();
            XmlNode rNode = draw.GetReportNode();
            XmlNode dsNode = draw.GetCreateNamedChildNode(rNode, "DataSets");
            XmlNode dataset = null;

            // find the requested dataset: its name equals the label passed in
            int dsCount = 0;        // count of the datasets
            string onlyOneDsname = null;
            foreach (XmlNode dNode in dsNode)
            {
                if (dNode.Name != "DataSet")
                    continue;
                XmlAttribute nAttr = dNode.Attributes["Name"];
                if (nAttr == null)  // shouldn't really happen
                    continue;
                if (dsCount == 0)
                    onlyOneDsname = nAttr.Value;        // we keep track of 1st name;

                dsCount++;
                if (nAttr.Value == label)
                    dataset = dNode;
            }

            bool bNew = false;
            if (dataset == null)    // no match -- this must be a new dataset
            {
                dataset = draw.CreateElement(dsNode, "DataSet", null);  // create empty node
                bNew = true;
            }
            ds.Add(dataset);

            using (PropertyDialog pd = new PropertyDialog(draw, ds, PropertyTypeEnum.DataSets))
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    if (dsCount == 1)
                    // if we used to just have one DataSet we may need to fix up DataRegions
                    //	that were defaulting to that name
                    {
                        dsCount = 0;
                        bool bUseName = false;
                        foreach (XmlNode dNode in dsNode)
                        {
                            if (dNode.Name != "DataSet")
                                continue;
                            XmlAttribute nAttr = dNode.Attributes["Name"];
                            if (nAttr == null)  // shouldn't really happen
                                continue;

                            dsCount++;
                            if (onlyOneDsname == nAttr.Value)
                                bUseName = true;
                        }
                        if (bUseName && dsCount > 1)
                        {
                            foreach (XmlNode drNode in draw.ReportNames.ReportItems)
                            {
                                switch (drNode.Name)
                                {
                                    // If a DataRegion doesn't have a dataset name specified use previous one
                                    case "Table":
                                    case "List":
                                    case "Matrix":
                                    case "Chart":
                                        XmlNode aNode = draw.GetNamedChildNode(drNode, "DataSetName");
                                        if (aNode == null)
                                            draw.CreateElement(drNode, "DataSetName", onlyOneDsname);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    Modified = true;
                }
                else if (bNew)  // if canceled and new DataSet get rid of temp node
                {
                    dsNode.RemoveChild(dataset);
                }
                if (pd.Delete)  // user must have hit a delete button for this to get set
                    dsNode.RemoveChild(dataset);

                if (!dsNode.HasChildNodes)      // If no dataset exists we remove DataSets
                    draw.RemoveElement(rNode, "DataSets");

                rdlEditPreview1.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
            }
        }

        private void saveToolStripButton1_Click(object sender, EventArgs e)
        {

            if (!FileSave())
                return;

            if (this.SavedFileEvent != null)
            {
                this.SavedFileEvent(this, new RdlDesignerSavedFileEvent(SourceFile));
            }

            if (rdlEditPreview1.CanEdit)
            {
                rdlEditPreview1.ClearUndo();
            }


            return;
        }

        private void cutToolStripButton1_Click(object sender, EventArgs e)
        {
            if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
            {
                ctlEditTextbox.Cut();
                return;
            }

            RdlEditPreview e1 = rdlEditPreview1;
            if (e1 == null)
                return;

            if (e1.SelectionLength > 0)
                e1.Cut();
        }


        private void copyToolStripButton1_Click(object sender, EventArgs e)
        {
            if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
            {
                ctlEditTextbox.Copy();
                return;
            }

            RdlEditPreview e1 = rdlEditPreview1;
            if (e1 == null)
                return;

            if (e1.SelectionLength > 0)
                e1.Copy();
        }

        private void pasteToolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void undoToolStripButton1_Click(object sender, EventArgs e)
        {

        }

        
        private void lineToolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void ctlEditTextbox_Validated(object sender, EventArgs e)
        {

        }

        private void ctlEditTextbox_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void mainTB_ItemClicked(object sender, EventArgs e)
        {

        }


        private void ToolStripButtons_Clicked(object sender, EventArgs e)
        {
            if (ctlInsertCurrent != null)
                ctlInsertCurrent.Checked = false;

            ToolStripButton ctl = (ToolStripButton)sender;
            ctlInsertCurrent = ctl.Checked ? ctl : null;

            CurrentInsert = ctlInsertCurrent == null ? null : (string)ctlInsertCurrent.Tag;
        }

        private void boldToolStripButton1_Click(object sender, EventArgs e)
        {

            ApplyStyleToSelected("FontWeight", boldToolStripButton1.Checked ? "Bold" : "Normal");
            SetProperties();

        }

        private void italiacToolStripButton1_Click(object sender, EventArgs e)
        {

            ApplyStyleToSelected("FontStyle", italiacToolStripButton1.Checked ? "Italic" : "Normal");
            SetProperties();

        }

        private void underlineToolStripButton2_Click(object sender, EventArgs e)
        {

            ApplyStyleToSelected("TextDecoration", underlineToolStripButton2.Checked ? "Underline" : "None");
            SetProperties();
        }

        private void leftAlignToolStripButton2_Click(object sender, EventArgs e)
        {
            TextAlignEnum ta = TextAlignEnum.General;

            if (sender == leftAlignToolStripButton2)
            {
                ta = TextAlignEnum.Left;
                leftAlignToolStripButton2.Checked = true;
                rightAlignToolStripButton3.Checked = centerAlignToolStripButton2.Checked = false;
            }
            else if (sender == rightAlignToolStripButton3)
            {
                ta = TextAlignEnum.Right;
                rightAlignToolStripButton3.Checked = true;
                leftAlignToolStripButton2.Checked = centerAlignToolStripButton2.Checked = false;
            }
            else if (sender == centerAlignToolStripButton2)
            {
                ta = TextAlignEnum.Center;
                centerAlignToolStripButton2.Checked = true;
                rightAlignToolStripButton3.Checked = leftAlignToolStripButton2.Checked = false;
            }

            ApplyStyleToSelected("TextAlign", ta.ToString());
            SetProperties();

        }

        private void fontToolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("FontFamily", fontToolStripComboBox1.Text);
                SetProperties();
            }
        }

        private void fontToolStripComboBox1_Validated(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("FontFamily", fontToolStripComboBox1.Text);
                SetProperties();
            }
        }

        private void fontSizeToolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("FontSize", fontSizeToolStripComboBox1.Text + "pt");
                SetProperties();
            }
        }

        private void fontSizeToolStripComboBox1_Validated(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("FontSize", fontSizeToolStripComboBox1.Text + "pt");
                SetProperties();
            }
        }


        // No real print-spooler integration under Majorsilence.Forms -- see MIGRATION-NOTES.md
        // (D2) for the full rationale. Majorsilence.Forms.PrintDialog is a no-op stub with no
        // real UI, so go straight to "export as PDF" instead of pretending to show one.
        private bool isPrinting = false;
        private async void printToolStripButton2_Click(object sender, EventArgs e)
        {

            if (isPrinting == true)			// already printing
            {
                MessageBox.Show(this, Strings.RdlUserControl_Show_PrintOneFile, Strings.RdlUserControl_Show_RDLDesign);
                return;
            }

            isPrinting = true;
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = Path.GetFileNameWithoutExtension(SourceFile.LocalPath) + ".pdf",
                };
                if (await sfd.ShowDialogAsync(this.FindForm()) == DialogResult.OK)
                {
                    await SaveAsAsync(sfd.FileName, OutputPresentationType.PDF);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Strings.RdlUserControl_Show_PrintError + ex.Message, Strings.RdlUserControl_Show_RDLDesign);
            }
            finally
            {
                isPrinting = false;
            }
        }

        private void zoomToolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            switch (zoomToolStripComboBox1.Text)
            {
                case "Actual Size":
                    Zoom = 1;
                    break;
                case "Fit Page":
                    ZoomMode = ZoomEnum.FitPage;
                    break;
                case "Fit Width":
                    ZoomMode = ZoomEnum.FitWidth;
                    break;
                default:
                    string s = zoomToolStripComboBox1.Text.Substring(0, zoomToolStripComboBox1.Text.Length - 1);
                    float z;
                    try
                    {
                        z = Convert.ToSingle(s) / 100f;
                        Zoom = z;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, Strings.RdlUserControl_Show_ZoomValueInvalid);
                    }
                    break;
            }
        }

        private void selectToolStripButton2_Click(object sender, EventArgs e)
        {
            SelectionTool = selectToolStripButton2.Checked;

        }

        private async void pdfToolStripButton2_Click(object sender, EventArgs e)
        {

            await ExportAsync(Rdl.OutputPresentationType.PDF);

        }

        private async void htmlToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.HTML);
        }

        private async void excelToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.Excel2007);
        }

        private async void XmlToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.XML);
        }

        private async void MhtToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.MHTML);
        }

        private async void CsvToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.CSV);
        }

        private async void RtfToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.RTF);
        }

        private async void TifToolStripButton2_Click(object sender, EventArgs e)
        {
            await ExportAsync(Rdl.OutputPresentationType.TIF);
        }

        private void foreColorPicker1_Validated(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("Color", foreColorPicker1.Text);
                SetProperties();
            }
        }

        private void foreColorPicker1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("Color", foreColorPicker1.Text);
                SetProperties();
            }
        }

        private void backColorPicker1_Validated(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("BackgroundColor", backColorPicker1.Text);
                SetProperties();
            }
        }

        private void backColorPicker1_Click(object sender, EventArgs e)
        {
            if (!bSuppressChange)
            {
                ApplyStyleToSelected("BackgroundColor", backColorPicker1.Text);
                SetProperties();
            }
        }

        private void ButtonShowProperties_Click(object sender, EventArgs e)
        {
            ShowProperties(!_ShowProperties);
        }

    }
}
