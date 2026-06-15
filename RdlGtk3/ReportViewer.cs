// 
//  ReportViewer.cs
//  
//  Author:
//       Krzysztof Marecki
// 
//  Copyright (c) 2010 Krzysztof Marecki
//  Copyright (c) 2012 Peter Gill
// 
// This file is part of the NReports project
// This file is part of the Majorsilence Reporting project 
//	
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Cairo;
using Gtk;
using Majorsilence.Reporting.Rdl;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = Gtk.Action;
using Strings = Majorsilence.Reporting.RdlEngine.Resources.Strings;

namespace Majorsilence.Reporting.RdlGtk3
{
    [ToolboxItem(true)]
    public class ReportViewer : Bin
    {
        private string _connectionString;
        private Action<Pages> _customPrintAction;
        private ToggleAction _errorsAction;
        private Box _hbox;
        private bool _overwriteSubreportConnection;
        private Pages _pages;
        private Action _printAction;
        private PrintOperation _printing;
        private Action _refreshAction;
        private Report _report;
        private OutputPresentationType[] _restrictedOutputPresentationTypes;
        private Action _saveAsAction;
        private Box _vbox;
        private Action _zoomInAction;
        private Action _zoomOutAction;

        public NeedPassword DataSourceReferencePassword = null;
        private Paned _hpanedReport;

        private int _hpanedWidth;
        private ScrolledWindow _scrolledwindowErrors;
        private ScrolledWindow _scrolledwindowPages;

        private bool _showErrors;

        private bool _showParams;

        private Uri _sourceFile;
        private TextView _textviewErrors;
        private Box _vboxPages;
        private Box _vboxParameters;

        public ReportViewer()
        {
            Build();
            Parameters = new ListDictionary();

            _errorsAction.Toggled += OnErrorsActionToggled;
            DisableActions();
            ShowErrors = false;
        }

        private Toolbar ActionGroup { get; set; }

        public ListDictionary Parameters { get; private set; }

        public bool ShowErrors
        {
            get => _showErrors;
            set
            {
                _showErrors = value;
                CheckVisibility();
            }
        }

        public string DefaultExportFileName { get; set; } = "report";

        public bool ShowParameters
        {
            get => _showParams;
            set
            {
                _showParams = value;
                CheckVisibility();
            }
        }

        public string WorkingDirectory { get; set; }

        public Uri SourceFile
        {
            get => _sourceFile;
            private set
            {
                _sourceFile = value;
                WorkingDirectory = System.IO.Path.GetDirectoryName(_sourceFile.LocalPath);
            }
        }

        public event EventHandler ReportPrinted;

        protected virtual void Build()
        {
            ActionGroup = new Toolbar();

#pragma warning disable CS0612 // Type or member is obsolete
            _refreshAction = new Action("refresh", "Refresh", "gtk-refresh", Stock.Refresh);
            _refreshAction.IsImportant = true;
            _refreshAction.Tooltip = "Refresh the report";
            ActionGroup.Add(_refreshAction.CreateToolItem());

            _saveAsAction = new Action("export", "Export", "gtk-save-as", Stock.SaveAs);
            _saveAsAction.Tooltip = "Export as PDF, CSV, ASP, HTML, MHTML, XML, Excel";
            ActionGroup.Add(_saveAsAction.CreateToolItem());

            _printAction = new Action("print", "Print", "gtk-print", Stock.Print);
            _printAction.Tooltip = "Print the report";
            ActionGroup.Add(_printAction.CreateToolItem());

            _zoomOutAction = new Action("zoom-out", "Zoom Out", "gtk-zoom-out", Stock.ZoomOut);

            _zoomOutAction.IsImportant = true;

            _zoomOutAction.Tooltip = "Zoom out the report";
            ActionGroup.Add(_zoomOutAction.CreateToolItem());

            _zoomInAction = new Action("zoom-in", "Zoom In", "gtk-zoom-in", Stock.ZoomIn);
            _zoomInAction.IsImportant = true;
            _zoomInAction.Tooltip = "Zoom in the report";
            ActionGroup.Add(_zoomInAction.CreateToolItem());

            _errorsAction = new ToggleAction("errors", "Errors", "gtk-dialog-warning", Stock.DialogWarning);
            _errorsAction.IsImportant = true;
            _errorsAction.DrawAsRadio = true;
            ActionGroup.Add(_errorsAction.CreateToolItem());

            _vbox = new Box(Orientation.Vertical, 6);
            _vbox.Homogeneous = false;
            _vbox.Expand = true;


            _hbox = new Box(Orientation.Horizontal, 6);
            _hbox.Homogeneous = false;
            _hbox.Expand = true;
            _vboxParameters = new Box(Orientation.Horizontal, 6);
            _vboxParameters.Homogeneous = false;
            _hbox.PackStart(_vboxParameters, false, false, 0);
            _vbox.Add(ActionGroup);

            _hpanedReport = new Paned(Orientation.Horizontal);
            _hpanedReport.Expand = true;
            _scrolledwindowPages = new ScrolledWindow();
            _scrolledwindowPages.Expand = true;

            Viewport gtkViewport = new();
            gtkViewport.Expand = true;
            _vboxPages = new Box(Orientation.Vertical, 6);
            _vboxPages.Homogeneous = false;
            _vboxPages.Expand = true;
            gtkViewport.Add(_vboxPages);
            _scrolledwindowPages.Add(gtkViewport);
            _hpanedReport.Pack1(_scrolledwindowPages, true, true);

            _scrolledwindowErrors = new ScrolledWindow();
            _textviewErrors = new TextView();
            _textviewErrors.WidthRequest = 200;
            _textviewErrors.Editable = false;
            _textviewErrors.WrapMode = WrapMode.WordChar;
            _scrolledwindowErrors.Add(_textviewErrors);
            _hpanedReport.Pack2(_scrolledwindowErrors, true, true);

            _hbox.PackEnd(_hpanedReport, true, true, 0);
            _vbox.PackEnd(_hbox, true, true, 0);

            Add(_vbox);
            _vbox.ShowAll();

            _refreshAction.Activated += OnRefreshActionActivated;
            _saveAsAction.Activated += OnPdfActionActivated;
            _printAction.Activated += OnPrintActionActivated;
            _zoomOutAction.Activated += OnZoomOutActionActivated;
            _zoomInAction.Activated += OnZoomInActionActivated;
            _hpanedReport.SizeAllocated += OnHpanedReportSizeAllocated;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        ///     Loads the report.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="connectionString">Relace all Connection string in report.</param>
        /// <param name="overwriteSubreportConnection">If true connection string in subreport also will be overwrite</param>
        /// <param name="restrictedOutputPresentationTypes">
        ///     Restricts <see cref="OutputPresentationType" /> to chose from in export
        ///     dialog
        /// </param>
        /// <param name="customPrintAction">>For use a custom print action</param>
        public async Task LoadReport(Uri filename, string parameters, string connectionString,
            bool overwriteSubreportConnection = false,
            OutputPresentationType[] restrictedOutputPresentationTypes = null, Action<Pages> customPrintAction = null)
        {
            SourceFile = filename;

            _connectionString = connectionString;
            _overwriteSubreportConnection = overwriteSubreportConnection;
            _customPrintAction = customPrintAction;

            await LoadReport(filename, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        ///     Loads the report.
        /// </summary>
        /// <param name="source">Xml source of report</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="connectionString">Relace all Connection string in report.</param>
        /// <param name="overwriteSubreportConnection">If true connection string in subreport also will be overwrite</param>
        /// <param name="restrictedOutputPresentationTypes">
        ///     Restricts <see cref="OutputPresentationType" /> to chose from in export
        ///     dialog
        /// </param>
        public async Task LoadReport(string source, string parameters, string connectionString,
            bool overwriteSubreportConnection = false,
            OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            _connectionString = connectionString;
            _overwriteSubreportConnection = overwriteSubreportConnection;

            await LoadReport(source, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        ///     Loads the report.
        /// </summary>
        /// <param name='filename'>Filename.</param>
        /// <param name="restrictedOutputPresentationTypes">
        ///     Restricts <see cref="OutputPresentationType" /> to chose from in export
        ///     dialog
        /// </param>
        public async Task LoadReport(Uri filename, OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            await LoadReport(filename, "", restrictedOutputPresentationTypes);
        }

        /// <summary>
        ///     Loads the report.
        /// </summary>
        /// <param name='sourcefile'>Filename.</param>
        /// <param name='parameters'>Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="restrictedOutputPresentationTypes">
        ///     Restricts <see cref="OutputPresentationType" /> to chose from in export
        ///     dialog
        /// </param>
        public async Task LoadReport(Uri sourcefile, string parameters,
            OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            SourceFile = sourcefile;

            string source = await File.ReadAllTextAsync(sourcefile.LocalPath);
            await LoadReport(source, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        ///     Loads the report.
        /// </summary>
        /// <param name="source">Xml source of report</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="restrictedOutputPresentationTypes">
        ///     Restricts <see cref="OutputPresentationType" /> to chose from in export
        ///     dialog
        /// </param>
        public async Task LoadReport(string source, string parameters,
            OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            _restrictedOutputPresentationTypes = restrictedOutputPresentationTypes ?? new OutputPresentationType[0];

            // Any parameters?  e.g.  file1.rdl?orderid=5 
            if (parameters.Trim() != "")
            {
                Parameters = GetParmeters(parameters);
            }
            else
            {
                Parameters = new ListDictionary();
            }

            // Compile the report 
            _report = await GetReport(source);
            if (_report == null)
            {
                return;
            }

            AddParameterControls();

            await RefreshReport();
        }

        private async Task RefreshReport()
        {
            if (ShowParameters)
            {
                SetParametersFromControls();
            }

            await _report.RunGetData(Parameters);
            _pages = await _report.BuildPages();

            // All GTK widget updates must run on the main thread. Use Application.Invoke to marshal.
            Application.Invoke(delegate
            {
                try
                {
                    foreach (Widget w in _vboxPages.AllChildren.Cast<Widget>().ToList())
                    {
                        _vboxPages.Remove(w);
                        try
                        {
                            w.Destroy();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Warning destroying widget: " + ex.Message);
                        }
                    }

                    for (int pageCount = 0; pageCount < _pages.Count; pageCount++)
                    {
                        ReportArea area = new();
                        area.SetReport(_report, _pages[pageCount]);
                        //area.Scale
                        _vboxPages.Add(area);
                    }

                    ShowAll();

                    SetErrorMessages(_report.ErrorItems);

                    if (_report.ErrorMaxSeverity == 0)
                    {
                        _showErrors = false;
                    }

                    //			Title = string.Format ("RDL report viewer - {0}", report.Name);
                    EnableActions();
                    CheckVisibility();
                }
                catch (Exception ex)
                {
                    // Don't allow UI update failures to crash the app. Log and carry on.
                    Console.WriteLine("UI update failed during RefreshReport: " + ex);
                }
            });
        }

        protected void OnZoomOutActionActivated(object sender, EventArgs e)
        {
            foreach (Widget w in _vboxPages.AllChildren)
            {
                if (w is ReportArea)
                {
                    ((ReportArea)w).Scale -= 0.1f;
                }
            }
            //reportarea.Scale -= 0.1f;
        }

        protected void OnZoomInActionActivated(object sender, EventArgs e)
        {
            foreach (Widget w in _vboxPages.AllChildren)
            {
                if (w is ReportArea)
                {
                    ((ReportArea)w).Scale += 0.1f;
                }
            }
            //reportarea.Scale += 0.1f;
        }

        // GetParameters creates a list dictionary
        // consisting of a report parameter name and a value.
        private ListDictionary GetParmeters(string parms)
        {
            ListDictionary ld = new();
            if (parms == null)
            {
                return ld; // dictionary will be empty in this case
            }

            // parms are separated by & 

            char[] breakChars = new[] { '&' };
            string[] ps = parms.Split(breakChars);

            foreach (string p in ps)
            {
                int iEq = p.IndexOf("=");
                if (iEq > 0)
                {
                    string name = p.Substring(0, iEq);
                    string val = p.Substring(iEq + 1);
                    ld.Add(name, val);
                }
            }

            return ld;
        }

        private async Task<Report> GetReport(string reportSource)
        {
            // Now parse the file 

            RDLParser rdlp;
            Report r;

            rdlp = new RDLParser(reportSource);
            rdlp.Folder = WorkingDirectory;
            rdlp.OverwriteConnectionString = _connectionString;
            rdlp.OverwriteInSubreport = _overwriteSubreportConnection;
            // RDLParser takes RDL XML and Parse compiles the report

            r = await rdlp.Parse();
            if (r.ErrorMaxSeverity > 0)
            {
                foreach (string emsg in r.ErrorItems)
                {
                    Console.WriteLine(emsg);
                }

                SetErrorMessages(r.ErrorItems);

                int severity = r.ErrorMaxSeverity;
                r.ErrorReset();
                if (severity > 4)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    _errorsAction.Active = true;
#pragma warning restore CS0612 // Type or member is obsolete
                    return null; // don't return when severe errors
                }
            }

            return r;
        }


        private void OnErrorsActionToggled(object sender, EventArgs e)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            ShowErrors = _errorsAction.Active;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        private void CheckVisibility()
        {
            if (ShowErrors)
            {
                _scrolledwindowErrors.ShowAll();
            }
            else
            {
                _scrolledwindowErrors.Hide();
            }

            //scrolledwindowErrors.HideAll();
            if (ShowParameters)
            {
                _vboxParameters.ShowAll();
            }
            //vboxParameters.HideAll();
        }

        protected override void OnShown()
        {
            base.OnShown();
            CheckVisibility();
        }

        private void DisableActions()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            _saveAsAction.Sensitive = false;
            _refreshAction.Sensitive = false;
            _printAction.Sensitive = false;
            _zoomInAction.Sensitive = false;
            _zoomOutAction.Sensitive = false;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        private void EnableActions()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            _saveAsAction.Sensitive = true;
            _refreshAction.Sensitive = true;
            _printAction.Sensitive = true;
            _zoomInAction.Sensitive = true;
            _zoomOutAction.Sensitive = true;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        private void AddParameterControls()
        {
            foreach (Widget child in _vboxParameters.Children.Cast<Widget>().ToList())
            {
                _vboxParameters.Remove(child);
                child.Destroy();
            }

            foreach (UserReportParameter rp in _report.UserReportParameters)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                HBox hbox = new();
#pragma warning restore CS0612 // Type or member is obsolete
                Label labelPrompt = new();
#pragma warning disable CS0612 // Type or member is obsolete
                labelPrompt.SetAlignment(0, 0.5f);
#pragma warning restore CS0612 // Type or member is obsolete
                labelPrompt.Text = string.Format("{0} :", rp.Prompt);
                hbox.PackStart(labelPrompt, true, true, 0);
                Entry entryValue = new();
                if (Parameters.Contains(rp.Name))
                {
                    if (Parameters[rp.Name] != null)
                    {
                        entryValue.Text = Parameters[rp.Name].ToString();
                    }
                }
                else
                {
                    if (rp.DefaultValue != null)
                    {
                        StringBuilder sb = new();
                        for (int i = 0; i < rp.DefaultValue.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }

                            sb.Append(rp.DefaultValue[i]);
                        }

                        entryValue.Text = sb.ToString();
                    }
                }

                hbox.PackStart(entryValue, false, false, 0);
                _vboxParameters.PackStart(hbox, false, false, 0);
            }
        }

        private void SetParametersFromControls()
        {
            int i = 0;
            foreach (UserReportParameter rp in _report.UserReportParameters)
            {
                HBox hbox = (HBox)_vboxParameters.Children[i];
                Entry entry = (Entry)hbox.Children[1];
                //parameters.Add (rp.Name, entry.Text);
                Parameters[rp.Name] = entry.Text;
                i++;
            }
        }

        private void SetErrorMessages(IList errors)
        {
            _textviewErrors.Buffer.Clear();

            if (errors == null || errors.Count == 0)
            {
                return;
            }

            StringBuilder msgs = new();
            msgs.AppendLine("Report rendering errors:");
            msgs.AppendLine();
            foreach (object error in errors)
            {
                msgs.AppendLine(error.ToString());
            }

            _textviewErrors.Buffer.Text = msgs.ToString();
            
#pragma warning disable CS0612 // Type or member is obsolete
            //_errorsAction.VisibleHorizontal = _report.ErrorMaxSeverity > 0;
#pragma warning restore CS0612 // Type or member is obsolete

        }

        protected async void OnPdfActionActivated(object sender, EventArgs e)
        {
            await SaveReport();
        }

        public async Task SaveReport()
        {
            // *********************************
            object[] param = new object[4];
            param[0] = Strings.ButtonCancel_Text;
            param[1] = ResponseType.Cancel;
            param[2] = Strings.ButtonSave_Text;
            param[3] = ResponseType.Accept;

            FileChooserDialog fc =
                new(Strings.FileChooser_SaveFileTo_Title,
                    null,
                    FileChooserAction.Save,
                    param);

            fc.CurrentName = DefaultExportFileName ?? _report.Name;

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.PDF))
            {
                FileFilter pdfFilter = new() { Name = "PDF" };
                string extensionPDF = ".pdf";
                pdfFilter.AddPattern($"*{extensionPDF}");
                fc.AddFilter(pdfFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.CSV))
            {
                FileFilter csvFilter = new() { Name = "CSV" };
                string extensionCSV = ".csv";
                csvFilter.AddPattern($"*{extensionCSV}");
                fc.AddFilter(csvFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.Excel2007DataOnly))
            {
                FileFilter excel2007Data = new() { Name = "Excel no formatting" };
                string extensionXLSX = ".xlsx";
                excel2007Data.AddPattern($"*{extensionXLSX}");
                fc.AddFilter(excel2007Data);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.Excel2007))
            {
                FileFilter excel2007 = new() { Name = "Excel with formatting" };
                string extensionXLSX = ".xlsx";
                excel2007.AddPattern($"*{extensionXLSX}");
                fc.AddFilter(excel2007);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.TIF))
            {
                FileFilter tiffFilter = new() { Name = "TIFF" };
                string extensionTIFF = ".tiff";
                tiffFilter.AddPattern($"*{extensionTIFF}");
                fc.AddFilter(tiffFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.ASPHTML))
            {
                FileFilter asphtmlFilter = new() { Name = "ASP HTML" };
                string extensionASPHTML = ".asphtml";
                asphtmlFilter.AddPattern($"*{extensionASPHTML}");
                fc.AddFilter(asphtmlFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.HTML))
            {
                FileFilter htmlFilter = new() { Name = "HTML" };
                string extensionHTML = ".html";
                htmlFilter.AddPattern($"*{extensionHTML}");
                fc.AddFilter(htmlFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.MHTML))
            {
                FileFilter mhtmlFilter = new() { Name = "MHTML" };
                string extensionMHTML = ".mhtml";
                mhtmlFilter.AddPattern($"*{extensionMHTML}");
                fc.AddFilter(mhtmlFilter);
            }

            if (!_restrictedOutputPresentationTypes.Contains(OutputPresentationType.XML))
            {
                FileFilter xmlFilter = new() { Name = "XML" };
                string extensionXML = ".xml";
                xmlFilter.AddPattern($"*{extensionXML}");
                fc.AddFilter(xmlFilter);
            }

            if (!fc.Filters.Any())
            {
                MessageDialog m = new(null, DialogFlags.Modal, MessageType.Info,
                    ButtonsType.Ok, false,
                    "Export in all document formats is prohibited");

                m.WindowPosition = WindowPosition.Center;
                m.Run();
                m.Destroy();
                fc.Destroy();
                return;
            }

            if (fc.Run() == (int)ResponseType.Accept)
            {
                try
                {
                    string searchPattern = "*";

                    string filename = fc.Filename;
                    OutputPresentationType exportType = OutputPresentationType.PDF;
                    if (fc.Filter.Name == "CSV")
                    {
                        exportType = OutputPresentationType.CSV;
                        if (filename.ToLower().Trim().EndsWith(".csv") == false)
                        {
                            filename = filename + ".csv";
                            searchPattern = "*.csv";
                        }
                    }
                    else if (fc.Filter.Name == "PDF")
                    {
                        exportType = OutputPresentationType.PDF;
                        if (filename.ToLower().Trim().EndsWith(".pdf") == false)
                        {
                            filename = filename + ".pdf";
                            searchPattern = "*.pdf";
                        }
                    }
                    else if (fc.Filter.Name == "Excel 2007 Data")
                    {
                        exportType = OutputPresentationType.ExcelTableOnly;
                        if (filename.ToLower().Trim().EndsWith(".xlsx") == false)
                        {
                            filename = filename + ".xlsx";
                            searchPattern = "*.xlsx";
                        }
                    }
                    else if (fc.Filter.Name == "Excel 2007")
                    {
                        exportType = OutputPresentationType.Excel2007;
                        if (filename.ToLower().Trim().EndsWith(".xlsx") == false)
                        {
                            filename = filename + ".xlsx";
                            searchPattern = "*.xlsx";
                        }
                    }
                    else if (fc.Filter.Name == "TIFF")
                    {
                        exportType = OutputPresentationType.TIF;
                        if (filename.ToLower().Trim().EndsWith(".tif") == false)
                        {
                            filename = filename + ".tif";
                            searchPattern = "*.tif";
                        }
                    }
                    else if (fc.Filter.Name == "ASP HTML")
                    {
                        exportType = OutputPresentationType.ASPHTML;
                        if (filename.ToLower().Trim().EndsWith(".asphtml") == false)
                        {
                            filename = filename + ".asphtml";
                            searchPattern = "*.asphtml";
                        }
                    }
                    else if (fc.Filter.Name == "HTML")
                    {
                        exportType = OutputPresentationType.HTML;
                        if (filename.ToLower().Trim().EndsWith(".html") == false)
                        {
                            filename = filename + ".html";
                            searchPattern = "*.html";
                        }
                    }
                    else if (fc.Filter.Name == "MHTML")
                    {
                        exportType = OutputPresentationType.MHTML;
                        if (filename.ToLower().Trim().EndsWith(".mhtml") == false)
                        {
                            filename = filename + ".mhtml";
                            searchPattern = "*.mhtml";
                        }
                    }
                    else if (fc.Filter.Name == "XML")
                    {
                        exportType = OutputPresentationType.XML;
                        if (filename.ToLower().Trim().EndsWith(".xml") == false)
                        {
                            filename = filename + ".xml";
                            searchPattern = "*.xml";
                        }
                    }

                    string directory = System.IO.Path.GetDirectoryName(filename);

                    string[] files = Directory.GetFiles(directory, searchPattern);

                    //Check for files with same name in directory
                    if (files.Any())
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (files[i] == filename)
                            {
                                //If found files with the same name in directory
                                MessageDialog m = new(null, DialogFlags.Modal, MessageType.Question,
                                    ButtonsType.YesNo, false,
                                    Strings.SaveToFile_CheckIf_SameFilesInDir);

                                m.SetPosition(WindowPosition.Center);
                                ResponseType result = (ResponseType)m.Run();
                                m.Destroy();
                                if (result == ResponseType.Yes)
                                {
                                    // Must use the RunGetData before each export or there is no data.
                                    await _report.RunGetData(Parameters);
                                    await ExportReport(_report, filename, exportType);
                                }

                                break;
                            }

                            if (i + 1 == files.Length && files[i] != filename)
                            {
                                //If no files with the same name found in directory
                                // Must use the RunGetData before each export or there is no data.
                                await _report.RunGetData(Parameters);
                                await ExportReport(_report, filename, exportType);
                                break;
                            }
                        }
                    }
                    else
                    {
                        //If no files found in directory
                        // Must use the RunGetData before each export or there is no data.
                        await _report.RunGetData(Parameters);
                        await ExportReport(_report, filename, exportType);
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog m = new(null, DialogFlags.Modal, MessageType.Info,
                        ButtonsType.Ok, false,
                        $"Error Saving Copy of {fc.Filter?.Name}." + Environment.NewLine + ex.Message);

                    m.Run();
                    m.Destroy();
                }
            }

            //Don't forget to call Destroy() or the FileChooserDialog window won't get closed.
            fc.Destroy();
        }

        /// <summary>
        ///     Save the report to the output selected.
        /// </summary>
        /// <param name='report'>
        ///     Report.
        /// </param>
        /// <param name='FileName'>
        ///     File name.
        /// </param>
        private async Task ExportReport(Report report, string FileName, OutputPresentationType exportType)
        {
            try
            {
                using OneFileStreamGen sg = new(FileName, true);
                await report.RunRender(sg, exportType);
            }
            catch (Exception ex)
            {
                using MessageDialog m = new(null, DialogFlags.Modal, MessageType.Error,
                    ButtonsType.Ok, false,
                    ex.Message);

                m.Run();
                m.Destroy();
            }
        }

        protected void OnPrintActionActivated(object sender, EventArgs e)
        {
        }

        public void PrintReport()
        {
            using PrintContext context = new(Window.Handle);
            if (_customPrintAction == null)
            {
                _printing = new PrintOperation();
                _printing.Unit = Unit.Points;
                _printing.UseFullPage = true;
                _printing.DefaultPageSetup = new PageSetup();
                _printing.DefaultPageSetup.Orientation =
                    _report.PageHeightPoints > _report.PageWidthPoints
                        ? PageOrientation.Portrait
                        : PageOrientation.Landscape;

                _printing.BeginPrint += HandlePrintBeginPrint;
                _printing.DrawPage += HandlePrintDrawPage;
                _printing.EndPrint += HandlePrintEndPrint;

                _printing.Run(PrintOperationAction.PrintDialog, null);
            }
            else
            {
                _customPrintAction.Invoke(_pages);
            }
        }

        private void HandlePrintBeginPrint(object o, BeginPrintArgs args)
        {
            _printing.NPages = _pages.Count;
        }

        private void HandlePrintDrawPage(object o, DrawPageArgs args)
        {
            if (args?.Context == null || _pages == null)
            {
                return;
            }

            using (Context g = args.Context.CairoContext)
            using (RenderCairo render = new(g))
            {
                render.RunPage(_pages[args.PageNr]);
            }
        }

        private void HandlePrintEndPrint(object o, EndPrintArgs args)
        {
            ReportPrinted?.Invoke(this, EventArgs.Empty);
            _printing.BeginPrint -= HandlePrintBeginPrint;
            _printing.DrawPage -= HandlePrintDrawPage;
            _printing.EndPrint -= HandlePrintEndPrint;
            _printing.DefaultPageSetup.Dispose();
            _printing.Dispose();
        }

        protected async void OnRefreshActionActivated(object sender, EventArgs e)
        {
            await RefreshReport();
        }

        protected void OnHpanedReportSizeAllocated(object o, SizeAllocatedArgs args)
        {
            if (args.Allocation.Width != _hpanedWidth)
            {
                _hpanedWidth = args.Allocation.Width;
                // int textviewWidth = scrolledwindowErrors.Allocation.Width + 10;
                // hpanedReport.Position = hpanedWidth - textviewWidth;
                _hpanedReport.Position = (int)(_hpanedWidth * 0.8);
            }
        }


        public override void Destroy()
        {
            base.Destroy();
        }

        protected void OnDeleteEvent(object sender, DeleteEventArgs a)
        {
            _errorsAction.Toggled -= OnErrorsActionToggled;
            _pages?.Dispose();
            _pages = null;
            _report?.Dispose();
            Application.Quit();
            a.RetVal = true;
        }
    }
}

