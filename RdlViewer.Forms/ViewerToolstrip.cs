using System;
using Majorsilence.Forms;
using Majorsilence.Forms.Printing;
using Majorsilence.Forms.Drawing;
using System.Resources;
using System.Reflection;
using System.IO;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlViewer.Resources;
using System.ComponentModel;

namespace Majorsilence.Reporting.RdlViewer
{
    public class ViewerToolstrip : ToolStrip
    {
        public ViewerToolstrip()
        {
            Init();
        }

        public ViewerToolstrip(RdlViewer viewer)
        {
            Init();
            this.Viewer = viewer;
        }

        private RdlViewer _viewer = null;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RdlViewer Viewer
        { 
            get{ return _viewer; }
            set
            { 
                _viewer = value;
                this.Viewer.PageNavigation += HandlePageNavigation;
            } 
        }

        private ToolStripTextBox currentPage = new ToolStripTextBox();
        private ToolStripLabel pageCount = new ToolStripLabel("");

        private void Init()
        {
            InitializeToolBar();
     
        }

        private async void OpenClicked(object sender, System.EventArgs e)
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            await Viewer.SetSourceFile(new Uri(dlg.FileName));
            await Viewer.Rebuild();

            currentPage.Text = Viewer.PageCurrent.ToString();
            pageCount.Text = "/" + Viewer.PageCount;
        }

        private async void PrintClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            // No real print-spooler integration -- see MIGRATION-NOTES.md and RdlViewer.cs's
            // comment where Print(PrintDocument) used to live. Majorsilence.Forms.PrintDialog is
            // a no-op stub with no real UI anyway, so don't pretend to show one; go straight to
            // "export as PDF" (the same path the Save As toolbar button already uses) and let the
            // user print from their OS's own PDF viewer.
            var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = System.IO.Path.GetFileNameWithoutExtension(Viewer.SourceFile.LocalPath) + ".pdf",
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                await Viewer.SaveAs(dlg.FileName, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
            }
        }

        private async void SaveAsClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            var dlg = new SaveFileDialog();
            dlg.Filter = Strings.RdlViewer_menuFileSaveAs_Click_FilesFilter;
            dlg.FileName = ".pdf";
            var result = dlg.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            // save the report in a rendered format
            string ext = null;
            int i = dlg.FileName.LastIndexOf('.');
            if (i < 1)
            {
                ext = "";
            }
            else
            {
                ext = dlg.FileName.Substring(i + 1).ToLower();
            }
            Majorsilence.Reporting.Rdl.OutputPresentationType type = Majorsilence.Reporting.Rdl.OutputPresentationType.Internal;
            switch (ext)
            {
                case "pdf":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.PDF;
                    break;
                case "xml":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.XML;
                    break;
                case "html":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.HTML;
                    break;
                case "htm":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.HTML;
                    break;
                case "csv":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.CSV;
                    break;
                case "rtf":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.RTF;
                    break;
                case "mht":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.MHTML;
                    break;
                case "mhtml":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.MHTML;
                    break;
                case "xlsx":
                    type = dlg.FilterIndex == 7 ? OutputPresentationType.ExcelTableOnly : OutputPresentationType.Excel2007;
                    break;
                case "tif":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.TIF;
                    break;
                case "tiff":
                    type = Majorsilence.Reporting.Rdl.OutputPresentationType.TIF;
                    break;
                default:
                    MessageBox.Show(String.Format("{0} is not a valid file type. File extension must be PDF, XML, HTML, CSV, MHT, RTF, TIF, XLSX.", dlg.FileName),
                        "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }

            await Viewer.SaveAs(dlg.FileName, type);
        }

        private void FirstPageClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            Viewer.PageCurrent = 1;
        }

        private void PreviousPageClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            if (Viewer.PageCurrent == 1)
            {
                return;
            }

            Viewer.PageCurrent -= 1;

        }

        private void NextPageClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            if (Viewer.PageCurrent == Viewer.PageCount)
            {
                return;
            }
            Viewer.PageCurrent += 1;

        }

        private void LastPageClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            Viewer.PageCurrent = Viewer.PageCount;
        }

        private void ZoomInClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            Viewer.Zoom += 0.5f;
        }

        private void ZoomOutClicked(object sender, System.EventArgs e)
        {
            if (Viewer == null)
            {
                return;
            }

            Viewer.Zoom -= 0.5f;
        }

        private void InitializeToolBar()
        {

           

            this.Items.Add(new ToolStripButton("Open", GetImage("fyiReporting.RdlViewer.Resources.document-open.png"), OpenClicked));
            this.Items.Add(new ToolStripButton("Save As", GetImage("fyiReporting.RdlViewer.Resources.document-save.png"), SaveAsClicked));
            this.Items.Add(new ToolStripButton("Print", GetImage("fyiReporting.RdlViewer.Resources.document-print.png"), PrintClicked));
            this.Items.Add(new ToolStripButton("<<", (Image)null, FirstPageClicked));
            this.Items.Add(new ToolStripButton("<", (Image)null, PreviousPageClicked));
            this.Items.Add(new ToolStripButton(">", (Image)null, NextPageClicked));
            this.Items.Add(new ToolStripButton(">>", (Image)null, LastPageClicked));
            this.Items.Add(this.currentPage);
            this.Items.Add(this.pageCount);
            this.Items.Add(new ToolStripButton("Zoom In", (Image)null, ZoomInClicked));
            this.Items.Add(new ToolStripButton("Zoom Out", (Image)null, ZoomOutClicked));
        }


        void HandlePageNavigation(object sender, PageNavigationEventArgs e)
        {
            currentPage.Text = e.NewPage.ToString();
        }

        private Bitmap GetImage(string resourceName)
        {

            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                return new Bitmap(stream);
            }

        }

    }
}

