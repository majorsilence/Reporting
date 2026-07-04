
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Data;
using Majorsilence.Forms.Printing;
using System.IO;
using System.Xml;
using EncryptionProvider;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlViewer;
using EncryptionProvider.String;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// RdlReader is a application for displaying reports based on RDL.
    /// </summary>
    internal partial class MDIChild 
    {


        public delegate void RdlChangeHandler(object sender, EventArgs e);
        public event RdlChangeHandler OnSelectionChanged;
        public event RdlChangeHandler OnSelectionMoved;
        public event RdlChangeHandler OnReportItemInserted;
        public event RdlChangeHandler OnDesignTabChanged;
        public event DesignCtl.OpenSubreportEventHandler OnOpenSubreport;
        public event DesignCtl.HeightEventHandler OnHeightChanged;

        Uri _SourceFile;
        // TabPage for this MDI Child

        private MDIChild() { }
        public MDIChild(int width, int height)
        {
            this.InitializeComponent();

            this.SuspendLayout();
            // 
            // rdlDesigner
            // 
            this.rdlDesigner.Name = "rdlDesigner";
            this.rdlDesigner.Size = new System.Drawing.Size(width, height);

            // 
            // MDIChild
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(width, height);

            this.Name = "";
            this.Text = "";
            this.FormClosing += this.MDIChild_Closing;

            this.ResumeLayout(false);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal TabPage Tab
        {
            get { return _Tab; }
            set { _Tab = value; }
        }

        public RdlEditPreview Editor
        {
            get
            {
                return rdlDesigner.CanEdit ? rdlDesigner : null;	// only return when it can edit
            }
        }

        public RdlEditPreview RdlEditor
        {
            get
            {
                return rdlDesigner;			// always return
            }
        }

        public void ShowEditLines(bool bShow)
        {
            rdlDesigner.ShowEditLines(bShow);
        }

        internal void ShowPreviewWaitDialog(bool bShow)
        {
            rdlDesigner.ShowPreviewWaitDialog(bShow);
        }
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool ShowReportItemOutline
        {
            get { return rdlDesigner.ShowReportItemOutline; }
            set { rdlDesigner.ShowReportItemOutline = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentInsert
        {
            get { return rdlDesigner.CurrentInsert; }
            set
            {
                rdlDesigner.CurrentInsert = value;
            }
        }

        public int CurrentLine
        {
            get { return rdlDesigner.CurrentLine; }
        }

        public int CurrentCh
        {
            get { return rdlDesigner.CurrentCh; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal DesignTabs DesignTab
        {
			get
			{
				return rdlDesigner.DesignTab; 
			}
            set { rdlDesigner.DesignTab = value; }
        }

        internal DesignXmlDraw DrawCtl
        {
            get { return rdlDesigner.DrawCtl; }
        }

        public XmlDocument ReportDocument
        {
            get { return rdlDesigner.ReportDocument; }
        }

        internal void SetFocus()
        {
            rdlDesigner.SetFocus();
        }

        public StyleInfo SelectedStyle
        {
            get { return rdlDesigner.SelectedStyle; }
        }

        public string SelectionName
        {
            get { return rdlDesigner.SelectionName; }
        }

        public PointF SelectionPosition
        {
            get { return rdlDesigner.SelectionPosition; }
        }

        public SizeF SelectionSize
        {
            get { return rdlDesigner.SelectionSize; }
        }

        public void ApplyStyleToSelected(string name, string v)
        {
            rdlDesigner.ApplyStyleToSelected(name, v);
        }

        public bool FileSave()
        {
            Uri file = SourceFile;
            if (file == null || file.LocalPath == "")		// if no file name then do SaveAs
            {
                // FileSaveAs is async (FileDialog.ShowDialog(Form) is async in Majorsilence.Forms
                // -- see MIGRATION-NOTES.md), but FileSave() itself has many synchronous callers
                // up through OkToClose()/MDIChild_Closing that aren't worth threading async
                // through. Sync-over-async bridge, matching the same pattern already used
                // elsewhere in this codebase (e.g. RdlViewer.cs's ShowParameters getter).
                return Task.Run(async () => await FileSaveAs()).GetAwaiter().GetResult();
            }
            string rdl = GetRdlText();

            return FileSave(file, rdl);
        }

        // EncryptionProvider.Prompt.ShowDialog is compiled only under `#if WINDOWS || NET48`
        // (a raw System.Windows.Forms.Form, never migrated) -- unavailable to this project's
        // plain net8.0/net10.0 TFM. Same replacement as RdlViewer.Forms/RdlViewer.cs's
        // PromptForPasskey: a small local dialog built directly on Majorsilence.Forms, using the
        // default CenterScreen StartPosition instead of Prompt's manual Screen.FromControl/
        // WorkingArea centering math.
        private static string PromptForPasskey(string text, string caption)
        {
            using var prompt = new Majorsilence.Forms.Form
            {
                Size = new System.Drawing.Size(500, 200),
                FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog,
                Text = caption,
            };
            var textLabel = new Majorsilence.Forms.Label { Left = 50, Top = 20, Width = 400, Height = 60, Text = text };
            var textBox = new Majorsilence.Forms.TextBox { Left = 50, Top = 100, Width = 400 };
            var confirmation = new Majorsilence.Forms.Button { Text = "OK", Left = 350, Width = 100, Top = 120 };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.ShowDialog();
            return textBox.Text;
        }

        private String doPossibleEncryption(Uri file, String rdl)
        {
            String extension = Path.GetExtension(file.LocalPath);
            if (extension.Equals(".encrypted"))
            {
                StringEncryption enc = new StringEncryption(PromptForPasskey("Please enter passkey", "Passkey?"));
                try
                {
                    rdl = enc.Encrypt(rdl);
                }
                catch (Exception)
                {
                    MessageBox.Show(Properties.Resources.MDIChild_doPossibleEncryption_Unable_to_encrypt_file_);
                }
                
            }
            return rdl;
        }


        private bool FileSave(Uri file, string rdl)
        {
            bool bOK = true;
            try
            {
                rdl = doPossibleEncryption(file, rdl);
                using (StreamWriter writer = new StreamWriter(file.LocalPath))
                {
                    writer.Write(rdl);
                    //				editRDL.ClearUndo();
                    //				editRDL.Modified = false;
                    //				SetTitle();
                    //				statusBar.Text = "Saved " + curFileName;
                }
            }
            catch (Exception ae)
            {
                bOK = false;
                MessageBox.Show(ae.Message + "\r\n" + ae.StackTrace);
                //				statusBar.Text = "Save of file '" + curFileName + "' failed";
            }
            if (bOK)
                this.Modified = false;
            return bOK;
        }

        public async Task<bool> ExportAsync(Rdl.OutputPresentationType type)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = string.Format(Strings.MDIChild_Export_ExportTitleFormat, type.ToString().ToUpper());
            switch (type)
            {
                case  OutputPresentationType.CSV:
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
                if (await sfd.ShowDialog(this) != DialogResult.OK)
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
                try { await SaveAs(sfd.FileName, type); }
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

        public async Task<bool> FileSaveAs()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Strings.MDIChild_FileSaveAs_RDLFilter;
            sfd.FilterIndex = 1;

            Uri file = SourceFile;

            sfd.FileName = file == null ? "*.rdl" : file.LocalPath;
            try
            {
                if (await sfd.ShowDialog(this) != DialogResult.OK)
                    return false;

                // User wants to save!
                string rdl = GetRdlText();
                if (FileSave(new Uri(sfd.FileName), rdl))
                {	// Save was successful
                    Text = sfd.FileName;
                    Tab.Text = Path.GetFileName(sfd.FileName);
                    _SourceFile = new Uri(sfd.FileName);
					DrawCtl.Folder = Path.GetDirectoryName(sfd.FileName);
                    Tab.ToolTipText = sfd.FileName;
                    return true;
                }
            }
            finally
            {
                sfd.Dispose();
            }
            return false;
        }

        public string GetRdlText()
        {
            return this.rdlDesigner.GetRdlText();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Modified
        {
            get { return rdlDesigner.Modified; }
            set
            {
                rdlDesigner.Modified = value;
                SetTitle();
            }
        }

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
                this.rdlDesigner.SetRdlText(rdl == null ? "" : rdl);
            }
        }
        
        public async Task SetSourceFileAsync(Uri file)
        {
            _SourceFile = file;

            string rdl = await GetRdlSourceAsync();
             this.rdlDesigner.SetRdlText(rdl == null ? "" : rdl);
        }

        private String doPossibleDecryption(String rdl)
        {

            if (Path.GetExtension(_SourceFile.LocalPath).Equals(".encrypted"))
            {
                
                try
                {
                    StringEncryption enc = new StringEncryption(PromptForPasskey("Please enter the passkey", "Passkey?"));
                    rdl = enc.Decrypt(rdl);
                }
                catch (Exception)
                {
                    MessageBox.Show(Properties.Resources.MDIChild_doPossibleDecryption_Incorrect_passkey_entered_);
                }
            }

            return rdl;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SourceRdl
        {
            get { return this.rdlDesigner.GetRdlText(); }
            set { this.rdlDesigner.SetRdlText(value); }
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
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            prog = doPossibleDecryption(prog);

            return prog;
        }
        
        private async Task<string> GetRdlSourceAsync()
        {
            StreamReader fs = null;
            string prog = null;
            try
            {
                fs = new StreamReader(_SourceFile.LocalPath);
                prog = await fs.ReadToEndAsync();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            prog = doPossibleDecryption(prog);

            return prog;
        }

        /// <summary>
        /// Number of pages in the report.
        /// </summary>
        public int PageCount
        {
            get { return this.rdlDesigner.PageCount; }
        }

        /// <summary>
        /// Current page in view on report
        /// </summary>
        public int PageCurrent
        {
            get { return this.rdlDesigner.PageCurrent; }
        }

        /// <summary>
        /// Page height of the report.
        /// </summary>
        public float PageHeight
        {
            get { return this.rdlDesigner.PageHeight; }
        }
        /// <summary>
        /// Turns the Selection Tool on in report preview
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SelectionTool
        {
            get
            {
                return this.rdlDesigner.SelectionTool;
            }
            set
            {
                this.rdlDesigner.SelectionTool = value;
            }
        }

        /// <summary>
        /// Page width of the report.
        /// </summary>
        public float PageWidth
        {
            get { return this.rdlDesigner.PageWidth; }
        }

        /// <summary>
        /// Zoom 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return this.rdlDesigner.Zoom; }
            set { this.rdlDesigner.Zoom = value; }
        }

        /// <summary>
        /// ZoomMode 
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ZoomEnum ZoomMode
        {
            get { return this.rdlDesigner.ZoomMode; }
            set { this.rdlDesigner.ZoomMode = value; }
        }

        // Print(PrintDocument) removed -- see RdlEditPreview.cs and MIGRATION-NOTES.md's D2
        // printing-redesign writeup. Use SaveAs(path, OutputPresentationType.PDF) instead.

        public async Task SaveAs(string filename, OutputPresentationType type)
        {
            await rdlDesigner.SaveAs(filename, type);
        }

        private void MDIChild_Closing(object sender, Majorsilence.Forms.FormClosingEventArgs e)
        {
            if (!OkToClose())
            {
                e.Cancel = true;
                return;
            }

            if (Tab == null)
                return;

            Control ctl = Tab.Parent;
            ctl.Controls.Remove(Tab);
            Tab.Tag = null;             // this is the Tab reference to this
            Tab = null;
        }

        public bool OkToClose()
        {
            if (!Modified)
                return true;

            DialogResult r =
                    MessageBox.Show(this, String.Format(Strings.MDIChild_ShowH_WantSaveChanges,
                    _SourceFile == null ? Strings.MDIChild_ShowH_Untitled : Path.GetFileName(_SourceFile.LocalPath)),
                    Strings.MDIChild_ShowH_fyiReportingDesigner,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3);

            bool bOK = true;
            if (r == DialogResult.Cancel)
                bOK = false;
            else if (r == DialogResult.Yes)
            {
                if (!FileSave())
                    bOK = false;
            }
            return bOK;
        }

        private void rdlDesigner_RdlChanged(object sender, System.EventArgs e)
        {
            SetTitle();
        }

        private void rdlDesigner_HeightChanged(object sender, HeightEventArgs e)
        {
            if (OnHeightChanged != null)
                OnHeightChanged(this, e);
        }

        private void rdlDesigner_SelectionChanged(object sender, System.EventArgs e)
        {
            if (OnSelectionChanged != null)
                OnSelectionChanged(this, e);
        }

        private void rdlDesigner_DesignTabChanged(object sender, System.EventArgs e)
        {
            if (OnDesignTabChanged != null)
                OnDesignTabChanged(this, e);
        }

        private void rdlDesigner_ReportItemInserted(object sender, System.EventArgs e)
        {
            if (OnReportItemInserted != null)
                OnReportItemInserted(this, e);
        }

        private void rdlDesigner_SelectionMoved(object sender, System.EventArgs e)
        {
            if (OnSelectionMoved != null)
                OnSelectionMoved(this, e);
        }

        private void rdlDesigner_OpenSubreport(object sender, SubReportEventArgs e)
        {
            if (OnOpenSubreport != null)
            {
                OnOpenSubreport(this, e);
            }
        }

        private void rdlDesigner_SaveRequested(object sender, System.EventArgs e)
        {
            FileSave();
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

        public Majorsilence.Reporting.RdlViewer.RdlViewer Viewer
        {
            get { return rdlDesigner.Viewer; }
        }

        private void MDIChild_Load(object sender, EventArgs e)
        {

        }

    }
}
