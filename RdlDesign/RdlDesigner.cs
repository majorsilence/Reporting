using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlViewer;
using Majorsilence.WinformUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// RdlDesigner is used for building and previewing RDL based reports.
	/// </summary>
	/// <example>
	/// The designer can be used from other applications by adding a reference to
	/// RdelDesigner.exe to your projects.
	/// <code lang="cs>
	/// Majorsilence.Reporting.RdlDesign.RdlDesigner designer = new Majorsilence.Reporting.RdlDesign.RdlDesigner("myFyiChannel", true);
	/// </code>
	/// <code lang="vb">
	/// Dim designer As New Majorsilence.Reporting.RdlDesign.RdlDesigner("myFyiChannel", true)
	/// designer.Show() 
	/// </code>
	/// </example>
	public partial class RdlDesigner : IMessageFilter
	{
		// The version should match what is set in program.cs
		static readonly string IpcFileName = string.Format("\\fyiIpcData{0}.txt", typeof(RdlDesigner).Assembly.GetName().Version.ToString().Replace(".", ""));

		static readonly string optFileName = Path.Combine(Majorsilence.Reporting.Rdl.Utility.Paths.MajorsilenceRoamingFolder(), "designerstate.xml");
		SortedList<DateTime, string> _RecentFiles = null;
		List<Uri> _CurrentFiles = null;     // temporary variable for current files
		List<string> _Toolbar = null;           // temporary variable for toolbar entries
		List<Uri> _TempReportFiles = null;      // temporary files created for report browsing
		int _RecentFilesMax = 5;            // # of items in recent files
		Process _ServerProcess = null;      // process for the RdlDesktop.exe --
		private Rdl.NeedPassword _GetPassword;
		private string _DataSourceReferencePassword = null;
		private bool bGotPassword = false;
		private readonly string DefaultHelpUrl = "https://github.com/majorsilence/My-FyiReporting/wiki/_pages";
		private readonly string DefaultSupportUrl = "https://github.com/majorsilence/My-FyiReporting/discussions";
		private string _HelpUrl;
		private string _SupportUrl;
		static private string[] _MapSubtypes = new string[] { "usa_map" };

		private bool _ShowPreviewWaitDialog = true;
		private bool _ShowEditLines = true;
		private bool _ShowReportItemOutline = true;  // Changed default to true for better visibility
		private bool _ShowTabbedInterface = true;
		private bool _ShowProperties = true;

		private NewLineChar _XmlNewLine = NewLineChar.Windows;
		private bool _PropertiesAutoHide = true;
		private readonly string TEMPRDL = "_tempfile_.rdl";
		private int TEMPRDL_INC = 0;

		// Tool bar  --- if you add to this list LOOK AT INIT TOOLBAR FIRST
		bool bSuppressChange = false;
		private Color _SaveExprBackColor = Color.LightGray;

		private ToolStripButton ctlInsertCurrent = null;
		private ToolStripMenuItem ctlMenuInsertCurrent = null;
		static string _MeasureUnits;

		private RdlDesigner()
		{
		}

		/// <summary>
		/// To retrieve / pass MeasureUnits parameter (see config file)
		/// </summary>
		public static string MeasureUnits
		{
			get { return _MeasureUnits; }
			set { _MeasureUnits = value; }
		}
		/// <summary>
		/// Get units measure from parameters and not from culture info
		/// </summary>
		/// <returns></returns>
		private bool IsMetric()
		{
			if (MeasureUnits == "cm")
				return true;
			else
				return false;
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
		public async Task OpenFileAsync(string filePath)
		{
			await this.OpenFileAsync(new Uri(filePath));
		}
		public async Task OpenFileAsync(Uri filePath)
		{
			await CreateMDIChildAsync(filePath, null, false);
			RecentFilesMenu();
		}

		/// <summary>
		/// Open a file programmatically when the designer is already open.
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="connectionString">The connection string that will be used</param>
		public async Task OpenFileAsync(string filePath, string connectionString)
		{
			await OpenFileAsync(new Uri(filePath), connectionString);
		}
		public async Task OpenFileAsync(Uri filePath, string connectionString)
		{
			XmlDocument xmlDoc = new XmlDocument();
            #if NET6_0_OR_GREATER
            string xml = await File.ReadAllTextAsync(filePath.AbsolutePath);
            #else
            string xml = File.ReadAllText(filePath.AbsolutePath);
            #endif
			xmlDoc.LoadXml(xml);

			foreach (XmlNode node in xmlDoc.GetElementsByTagName("ConnectString"))
			{
				node.InnerText = connectionString;
			}

			xmlDoc.Save(filePath.AbsolutePath);

			await CreateMDIChildAsync(filePath, null, false);
			RecentFilesMenu();
		}

		// TODO: Event raise on file save (with path and name to saved file)
		// Properties to control which controls in the menu and toolbar are available.

		public event RdlDesign.RdlDesignerSavedFileEventHandler SavedFileEvent;

        private bool _openPreviousSession;
		/// <summary>
		/// Designer constructor.
		/// </summary>
		/// <param name="IpcChannelPortName">The IPC channel that the designer will use.</param>
		/// <param name="openPreviousSession">True or False open the previous reports that were open in the designer.</param>
		public RdlDesigner(string IpcChannelPortName, bool openPreviousSession)
		{
            InitializeComponent();
            _openPreviousSession = openPreviousSession;
        }
        
        private async void RdlDesigner_Load(object sender, EventArgs e)
        {
            this.ShowWaiter();
            KeyPreview = true;
            await GetStartupStateAsync();

            // Enable alignment grid by default for better visibility
            AlignmentGridEnable.Checked = true;

            // Initialize the recent file menu
            RecentFilesMenu();
            propertiesWindowsToolStripMenuItem.Checked = _ShowProperties;
            IsMdiContainer = true;
            
            Application.AddMessageFilter(this);

            this.MdiChildActivate += new EventHandler(RdlDesigner_MdiChildActivate);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.RdlDesigner_Closing);
            _GetPassword = new Rdl.NeedPassword(this.GetPassword);

            InitToolbar();

            //Build CustomItems insert menu
            BuildCustomItemsInsertMenu(insertToolStripMenuItem);

            // open up the current files if any
            if (_CurrentFiles != null && _openPreviousSession == true)
            {
                foreach (Uri file in _CurrentFiles)
                {
                    await CreateMDIChildAsync(file, null, false);
                }
                _CurrentFiles = null;       // don't need this any longer
            }

            DesignTabChanged(this, new EventArgs());        // force toolbar to get updated
            this.HideWaiter();
        }

        /// <summary>
        /// Handles mousewheel processing when window under mousewheel doesn't have focus
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x20a)
            {
                // WM_MOUSEWHEEL, find the control at screen position m.LParam
                Point pos = new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
                IntPtr hWnd = WindowFromPoint(pos);
                if (hWnd != IntPtr.Zero && hWnd != m.HWnd && Control.FromHandle(hWnd) != null)
                {
                    SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
                    return true;
                }
            }

            return false;
        }

        // P/Invoke declarations
		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(Point pt);
		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		private DockStyle GetPropertiesDockStyle(string l)
		{
			DockStyle ds;
			try
			{
				ds = (DockStyle)Enum.Parse(typeof(DockStyle), l, true);
			}
			catch
			{
				ds = DockStyle.Right;
			}

			return ds;
		}

		private void InitToolbar()
		{
			InitToolbarFont();
			InitToolbarFontSize();

			zoomToolStripComboBox1.Items.AddRange(StaticLists.ZoomList);

			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RdlDesigner));


			mainTC.Visible = _ShowTabbedInterface;
			if (_ShowTabbedInterface)
			{   // When tabbed we force the mdi children to be maximized (on reset)
				foreach (MDIChild mc in this.MdiChildren)
				{
					mc.WindowState = FormWindowState.Maximized;
				}
			}


			mainTB.Name = "mainTB";

			//			mainTB.Size = new Size(440,20);


		}
		/// <summary>
		/// Handles right mouse button processing on current tab
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void mainTC_MouseClick(object sender, MouseEventArgs e)
		{
			TabControl tc = sender as TabControl;
			if (tc == null)
				return;

			if (e.Button != MouseButtons.Right)
				return;
			Point p = e.Location;
			int current = -1;
			for (int i = 0; i < tc.TabCount; i++)
			{
				Rectangle r = tc.GetTabRect(i);
				if (r.Contains(p))
				{
					current = i;
					break;
				}
			}
			if (current != tc.SelectedIndex)
				return;             // didn't find the tab

			ContextMenuTB.Show(tc, p);
		}

		void mainTB_SizeChanged(object sender, EventArgs e)
		{
			mainTC.Width = mainTB.Width;
		}

		void mainTC_SelectedIndexChanged(object sender, EventArgs e)
		{
			MDIChild mc = mainTC.SelectedTab == null ? null : mainTC.SelectedTab.Tag as MDIChild;
			mdi_Activate(mc);
		}

		[DllImport("user32.dll")]
		public static extern bool LockWindowUpdate(IntPtr hWndLock);

		void mdi_Activate(MDIChild mc)
		{
			if (mc == null)
				return;

			LockWindowUpdate(this.Handle);
			mc.Activate();
			this.Refresh(); //Forces a synchronous redraw of all controls

			LockWindowUpdate(IntPtr.Zero);
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal int RecentFilesMax
		{
			get { return _RecentFilesMax; }
			set { _RecentFilesMax = value; }
		}

		internal Rdl.NeedPassword SharedDatasetPassword
		{
			get { return _GetPassword; }
		}

		internal SortedList<DateTime, string> RecentFiles
		{
			get { return _RecentFiles; }
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal string HelpUrl
		{
			get
			{
				if (_HelpUrl != null && _HelpUrl.Length > 0)
					return _HelpUrl;
				return DefaultHelpUrl;
			}
			set
			{
				_HelpUrl = value.Length > 0 ? value : DefaultHelpUrl;
			}
		}

		static internal string[] MapSubtypes
		{
			get
			{
				return _MapSubtypes;
			}
			set
			{
				_MapSubtypes = value;
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal bool ShowPreviewWaitDialog
		{
			get { return _ShowPreviewWaitDialog; }
			set { _ShowPreviewWaitDialog = value; }
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal bool ShowEditLines
		{
			get
			{
				return _ShowEditLines;
			}
			set
			{
				_ShowEditLines = value;
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal bool PropertiesAutoHide
		{
			get { return _PropertiesAutoHide; }
			set { _PropertiesAutoHide = value; }
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal DockStyle PropertiesLocation
		{
			get
			{
				return _PropertiesLocation;
			}
			set
			{
				if (_PropertiesLocation == value)
					return;         // didn't change nothing to do
				_PropertiesLocation = value;

				mainSP.Dock = _PropertiesLocation;
				mainProperties.Dock = _PropertiesLocation;
				// Adjust the size of the property window so that it doesn't
				//   fill the whole main window
				switch (_PropertiesLocation)
				{
					case DockStyle.Left:
					case DockStyle.Right:
						mainProperties.Width = this.Width / 3;
						break;
					case DockStyle.Top:
					case DockStyle.Bottom:
						mainProperties.Height = this.Height / 3;
						break;
				}
			}
		}

		private void BuildCustomItemsInsertMenu(ToolStripDropDownItem menuItem)
		{
			try
			{
				var sa = RdlEngineConfig.GetCustomReportTypes();
				if (sa == null || sa.Length == 0)
					return;

				var separator = menuItem.DropDownItems["CustomSeparator"];

				if (separator == null)
				{
					separator = new ToolStripSeparator { Name = "CustomSeparator" };
					menuItem.DropDownItems.Add(separator);       // put a separator
				}
				else
				{
					var index = menuItem.DropDownItems.IndexOf(separator) + 1;

					while (menuItem.DropDownItems.Count > index)
					{
						menuItem.DropDownItems.RemoveAt(index);
					}
				}

				// Add the custom report items to the insert menu
				foreach (var m in sa)
				{
					var mi = new ToolStripMenuItem(m + "...");
					mi.Click += InsertToolStripMenuItem_Click;
					mi.Tag = m;
					menuItem.DropDownItems.Add(mi);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(Strings.DesignCtl_ShowB_CustomReportItemError, ex.Message), Strings.DesignCtl_Show_Insert, MessageBoxButtons.OK);
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal bool ShowReportItemOutline
		{
			get
			{
				return _ShowReportItemOutline;
			}
			set
			{
				_ShowReportItemOutline = value;
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal bool ShowTabbedInterface
		{
			get { return _ShowTabbedInterface; }
			set { _ShowTabbedInterface = value; }
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal NewLineChar XmlNewLine
		{
			get => _XmlNewLine;
			set => _XmlNewLine = value;
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal string SupportUrl
		{
			get
			{
				if (_SupportUrl != null && _SupportUrl.Length > 0)
					return _SupportUrl;
				return DefaultSupportUrl;
			}
			set
			{
				_SupportUrl = value.Length > 0 ? value : DefaultSupportUrl;
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal List<string> Toolbar
		{
			get { return _Toolbar; }
			set
			{
				_Toolbar = value;
				InitToolbar();          // reset the toolbar
			}
		}

		internal List<string> ToolbarDefault
		{
			get
			{
				return new List<string>(new string[] {
					"New", "Open", "Save", "Space", "Cut", "Copy", "Paste", "Undo", "Space",
					"Textbox", "Chart", "Table", "List", "Image", "Matrix", "Subreport",
					"Rectangle", "Line", "Space","Edit", "Newline",
					"Bold", "Italic", "Underline", "Space","Align","Space",
					"Font", "FontSize", "Space", "ForeColor", "BackColor",
					"Space", "Print", "Space", "Zoom", "SelectTool", "Space",
					"PDF", "HTML", "Excel", "XML", "MHT", "CSV", "RTF", "TIF"});
				;
			}
		}

		internal List<string> ToolbarAllowDups
		{
			get
			{
				return new List<string>(new string[] {
				  "Space",
				  "Newline"});
				;
			}
		}

		/// <summary>
		/// All of the possible items that can be placed on a toolbar
		/// </summary>
		internal string[] ToolbarOperations
		{
			get
			{
				return new string[]
					{"Newline",
					"Align",
					"Bold",
					"Italic",
					"Underline",
					"Space",
					"Font",
					"FontSize",
					"ForeColor",
					"BackColor",
					"New",
					"Open",
					"Save",
					"Cut",
					"Copy",
					"Undo",
					"Paste",
					"Print",
					"XML",
					"PDF",
					"HTML",
					"MHT",
					"CSV",
					"RTF",
					"Excel",
					"TIF",
					"Edit",
					"Textbox",
					"Chart",
					"Rectangle",
					"Table",
					"Matrix",
					"List",
					"Line",
					"Image",
					"SelectTool",
					"Subreport",
					"Zoom"   };
			}
		}



		void fxExpr_MouseLeave(object sender, EventArgs e)
		{
			Label lbl = sender as Label;
			if (lbl == null)
				return;

			lbl.BackColor = _SaveExprBackColor;
		}

		void fxExpr_MouseEnter(object sender, EventArgs e)
		{
			Label lbl = sender as Label;
			if (lbl == null)
				return;
			_SaveExprBackColor = lbl.BackColor;
			lbl.BackColor = Color.LightGray;

			return;
		}

		void fxExpr_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null ||
				mc.DesignTab != DesignTabs.Design || mc.DrawCtl.SelectedCount != 1 ||
				mc.Editor == null)
				return;

			XmlNode tn = mc.DrawCtl.SelectedList[0];

			using (DialogExprEditor de = new DialogExprEditor(mc.DrawCtl, ctlEditTextbox.Text, tn))
			{
				// Display the UI editor dialog
				if (de.ShowDialog(this) == DialogResult.OK)
				{
					ctlEditTextbox.Text = de.Expression;
					mc.Editor.SetSelectedText(de.Expression);
					SetProperties(mc);
				}
			}
		}

		private SimpleToggle InitToolbarInsertToggle(ref int x, int y, string t)
		{
			return InitToolbarInsertToggle(ref x, y, t, null, new EventHandler(this.Insert_Click));
		}

		private SimpleToggle InitToolbarInsertToggle(ref int x, int y, string t,
			Image bImg)
		{
			return InitToolbarInsertToggle(ref x, y, t, bImg, new EventHandler(this.Insert_Click));
		}

		private SimpleToggle InitToolbarInsertToggle(ref int x, int y, string t,
			Image bImg, EventHandler eh)
		{
			SimpleToggle ctl = new SimpleToggle();
			ctl.UpColor = this.BackColor;
			ctl.Click += eh;    // click handler for all inserts

			if (bImg == null)
			{
				ctl.Text = t;
				using (Graphics g = ctl.CreateGraphics())
				{
					SizeF fs = g.MeasureString(ctl.Text, ctl.Font);
					ctl.Height = (int)fs.Height + 8;    // 8 is for margins
					ctl.Width = (int)fs.Width + 12;
				}
			}
			else
			{
				ctl.Image = bImg;
				ctl.ImageAlign = ContentAlignment.MiddleCenter;
				ctl.Height = bImg.Height + 5;
				ctl.Width = bImg.Width + 8;
				ctl.Text = "";
			}

			ctl.Tag = t;
			ctl.Left = x;
			ctl.Top = y;
			ctl.FlatStyle = FlatStyle.Flat;
			ToolTip tipb = new ToolTip();
			tipb.AutomaticDelay = 500;
			tipb.ShowAlways = true;
			tipb.SetToolTip(ctl, t);
			mainTB.Controls.Add(ctl);

			x = x + ctl.Width;

			return ctl;
		}


		private int InitToolbarFont()
		{
			// Create the font


			foreach (FontFamily ff in FontFamily.Families)
			{
				fontToolStripComboBox1.Items.Add(ff.Name);
			}

			return fontToolStripComboBox1.Width;
		}

		private int InitToolbarFontSize()
		{
			// Create the font

			string[] sizes = new string[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "36", "48", "72" };
			fontSizeToolStripComboBox1.Items.AddRange(sizes);

			return fontSizeToolStripComboBox1.Width;
		}

		void tip_Popup_Fore(object sender, PopupEventArgs e)
		{
			ToolTip tt = sender as ToolTip;
			if (tt == null)
				return;
			string title = null;
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc != null && mc.DesignTab == DesignTabs.Design && mc.DrawCtl.SelectedCount == 1)
			{
				title = foreColorPicker1.Text;
			}

			tt.ToolTipTitle = title;
		}

		private void ctlForeColor_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("Color", foreColorPicker1.Text);
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		void tip_Popup_Back(object sender, PopupEventArgs e)
		{
			ToolTip tt = sender as ToolTip;
			if (tt == null)
				return;

			string title = null;
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc != null && mc.DesignTab == DesignTabs.Design && mc.DrawCtl.SelectedCount == 1)
			{
				title = backColorPicker1.Text;
			}

			tt.ToolTipTitle = title;
		}



		private bool OkToSave()
		{
			foreach (MDIChild mc in this.MdiChildren)
			{
				if (!mc.OkToClose())
					return false;
			}
			return true;
		}

		private void menuFile_Popup(object sender, EventArgs e)
		{
			// These menus require an MDIChild in order to work
			bool bEnable = this.MdiChildren.Length > 0 ? true : false;
			closeToolStripMenuItem.Enabled = bEnable;
			saveToolStripMenuItem.Enabled = bEnable;
			saveAsToolStripMenuItem.Enabled = bEnable;

			MDIChild mc = this.ActiveMdiChild as MDIChild;
			printToolStripMenuItem.Enabled = exportToolStripMenuItem.Enabled = (mc != null && mc.DesignTab == DesignTabs.Preview);

			// Recent File is enabled when there exists some files 
			recentFilesToolStripMenuItem.Enabled = this._RecentFiles.Count <= 0 ? false : true;
		}

		private void menuFileClose_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc != null)
				mc.Close();
		}

		private void menuFileExit_Click(object sender, EventArgs e)
		{
			if (!OkToSave())
				return;
			SaveStartupState();
			menuToolsCloseProcess(false);
			CleanupTempFiles();
			Application.Exit();
			//			Environment.Exit(0);
		}

		private async void menuFileOpen_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			OpenFileDialog ofd = new OpenFileDialog();
			if (mc != null)
			{
				try
				{
					ofd.InitialDirectory = Path.GetDirectoryName(mc.SourceFile.LocalPath);
				}
				catch
				{
				}
			}
			ofd.DefaultExt = "rdl";
			ofd.Filter = Strings.RdlDesigner_menuFileOpen_Click_ReportFilesFilter;
			ofd.FilterIndex = 1;
			ofd.CheckFileExists = true;
			ofd.Multiselect = true;
			try
			{
				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					foreach (string file in ofd.FileNames)
					{
						await CreateMDIChildAsync(new Uri(file), null, false);
					}
					RecentFilesMenu();      // update the menu for recent files
				}
			}
			finally
			{
				ofd.Dispose();
			}
		}

		// Create an MDI child.   Only creates it if not already open.
		private async Task<MDIChild> CreateMDIChildAsync(Uri file, string rdl, bool bMenuUpdate)
		{
			MDIChild mcOpen = null;
			if (file != null)
			{

				foreach (MDIChild mc in this.MdiChildren)
				{
					if (mc.SourceFile != null && file.LocalPath == mc.SourceFile.LocalPath)
					{                           // we found it
						mcOpen = mc;
						break;
					}
				}
			}
			if (mcOpen == null)
			{
				MDIChild mc = null;
				try
				{
					mc = new MDIChild(this.ClientRectangle.Width * 3 / 5, this.ClientRectangle.Height * 3 / 5);
					mc.OnSelectionChanged += new MDIChild.RdlChangeHandler(SelectionChanged);
					mc.OnSelectionMoved += new MDIChild.RdlChangeHandler(SelectionMoved);
					mc.OnReportItemInserted += new MDIChild.RdlChangeHandler(ReportItemInserted);
					mc.OnDesignTabChanged += new MDIChild.RdlChangeHandler(DesignTabChanged);
					mc.OnOpenSubreport += new DesignCtl.OpenSubreportEventHandler(OpenSubReportEvent);
					mc.OnHeightChanged += new DesignCtl.HeightEventHandler(HeightChanged);

					mc.MdiParent = this;
					mc.Editor.DesignCtl.RdlDesigner = this;
					if (this._ShowTabbedInterface)
						mc.WindowState = FormWindowState.Maximized;
					mc.Viewer.GetDataSourceReferencePassword = _GetPassword;
					if (file != null)
					{
						mc.Viewer.Folder = Path.GetDirectoryName(file.LocalPath);
						await mc.SetSourceFileAsync(file);
						mc.Text = Path.GetFileName(file.LocalPath);
						mc.DrawCtl.Folder = Path.GetDirectoryName(file.LocalPath);
						mc.Viewer.ReportName = Path.GetFileNameWithoutExtension(file.LocalPath);
						NoteRecentFiles(file, bMenuUpdate);
					}
					else
					{
						mc.SourceRdl = rdl;
						mc.Viewer.ReportName = mc.Text = Strings.RdlDesigner_CreateMDIChild_Untitled;
					}
					mc.ShowEditLines(this._ShowEditLines);
					mc.ShowReportItemOutline = this.ShowReportItemOutline;
					mc.ShowPreviewWaitDialog(this._ShowPreviewWaitDialog);
					// add to toolbar tab
					TabPage tp = new TabPage();
					tp.Location = new System.Drawing.Point(0, 0);
					tp.Name = mc.Text;
					tp.TabIndex = 1;
					tp.Text = mc.Text;
					tp.Tag = mc;                // tie the mdichild to the tabpage
					tp.ToolTipText = file == null ? "" : file.LocalPath;
					mainTC.Controls.Add(tp);
					mc.Tab = tp;
                    
					mc.Show();
					mcOpen = mc;

				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					if (mc != null)
						mc.Close();
					return null;
				}
			}
			else
			{
				mcOpen.Activate();
			}
			return mcOpen;
		}

		private void DesignTabChanged(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			DesignTabs tab = DesignTabs.Edit;
			if (mc != null)
				tab = mc.DesignTab;
			bool bEnableEdit = false;
			bool bEnableDesign = false;
			bool bEnablePreview = false;
			bool bShowProp = _ShowProperties;
			switch (tab)
			{
				case DesignTabs.Edit:
					bEnableEdit = true;
					if (_PropertiesAutoHide)
						bShowProp = false;
					break;
				case DesignTabs.Design:
					bEnableDesign = true;
					break;
				case DesignTabs.Preview:
					if (_PropertiesAutoHide)
						bShowProp = false;
					bEnablePreview = true;
					break;
			}
			if (!bEnableEdit && this._ValidateRdl != null)
			{
				this._ValidateRdl.Close();
			}
			mainProperties.Visible = mainSP.Visible = bShowProp;
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
				selectToolStripButton2.Checked = mc == null ? false : mc.SelectionTool;
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

			foreach (var item in insertToolStripMenuItem.DropDownItems.OfType<ToolStripItem>())
			{
				item.Enabled = bEnableDesign;
			}

			this.EnableEditTextBox();

			if (zoomToolStripComboBox1 != null)
			{
				zoomToolStripComboBox1.Enabled = bEnablePreview;
				string zText = "Actual Size";
				if (mc != null)
				{
					switch (mc.ZoomMode)
					{
						case ZoomEnum.FitWidth:
							zText = "Fit Width";
							break;
						case ZoomEnum.FitPage:
							zText = "Fit Page";
							break;
						case ZoomEnum.UseZoom:
							if (mc.Zoom == 1)
								zText = "Actual Size";
							else
								zText = string.Format("{0:0}", mc.Zoom * 100f);
							break;
					}
					zoomToolStripComboBox1.Text = zText;
				}
			}
			// when no active sheet
			if (this.saveToolStripButton1 != null)
				this.saveToolStripButton1.Enabled = mc != null;

			// Update the status and position information
			SetStatusNameAndPosition();
		}

		private void ctlUnderline_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.ApplyStyleToSelected("TextDecoration", underlineToolStripButton2.Checked ? "Underline" : "None");
			SetProperties(mc);

			SetMDIChildFocus(mc);
		}

		private void ctlFont_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("FontFamily", fontToolStripComboBox1.Text);
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		private void ctlFontSize_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("FontSize", fontSizeToolStripComboBox1.Text + "pt");
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		private void ctlSelectTool_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.SelectionTool = selectToolStripButton2.Checked;

			SetMDIChildFocus(mc);
		}

		private void ctlBackColor_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("BackgroundColor", backColorPicker1.Text);
				SetProperties(mc);
			}

			SetMDIChildFocus(mc);
		}

		private void ctlZoom_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.SetFocus();

			switch (zoomToolStripComboBox1.Text)
			{
				case "Actual Size":
					mc.Zoom = 1;
					break;
				case "Fit Page":
					mc.ZoomMode = ZoomEnum.FitPage;
					break;
				case "Fit Width":
					mc.ZoomMode = ZoomEnum.FitWidth;
					break;
				default:
					string s = zoomToolStripComboBox1.Text.Substring(0, zoomToolStripComboBox1.Text.Length - 1);
					float z;
					try
					{
						z = Convert.ToSingle(s) / 100f;
						mc.Zoom = z;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, Strings.RdlDesigner_Show_ZoomValueInvalid);
					}
					break;
			}
		}

		private void EnableEditTextBox()
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			bool bEnable = false;

			if (this.ctlEditTextbox == null || mc == null ||
				mc.DesignTab != DesignTabs.Design || mc.DrawCtl.SelectedCount != 1)
			{ }
			else
			{
				XmlNode tn = mc.DrawCtl.SelectedList[0] as XmlNode;
				if (tn != null && tn.Name == "Textbox")
				{
					ctlEditTextbox.Text = mc.DrawCtl.GetElementValue(tn, "Value", "");
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

		private void ctlItalic_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.ApplyStyleToSelected("FontStyle", italiacToolStripButton1.Checked ? "Italic" : "Normal");
			SetProperties(mc);

			SetMDIChildFocus(mc);
		}

		private void ReportItemInserted(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
			{
				return;
			}

			// turn off the current selection after an item is inserted
			if (ctlInsertCurrent != null)
			{
				ctlInsertCurrent.Checked = false;
				mc.CurrentInsert = null;
				ctlInsertCurrent = null;
			}
			if (ctlMenuInsertCurrent != null)
			{
				ctlMenuInsertCurrent.Checked = false;
				mc.CurrentInsert = null;
				ctlMenuInsertCurrent = null;
			}
		}

		private async void OpenSubReportEvent(object sender, SubReportEventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			Uri file = new Uri(mc.Viewer.Folder);
			if (e.SubReportName[0] == Path.DirectorySeparatorChar)
			{
				file = new Uri(file.LocalPath + e.SubReportName);
			}
			else
			{
				file = new Uri(file.LocalPath + Path.DirectorySeparatorChar + e.SubReportName + ".rdl");
			}

			await CreateMDIChildAsync(file, null, true);
		}

		private void HeightChanged(object sender, HeightEventArgs e)
		{
			if (e.Height == null)
			{
				SetProperties(ActiveMdiChild as MDIChild);

				statusPosition.Text = "";
				return;
			}

			//   var rinfo = new RegionInfo(CultureInfo.CurrentCulture.Name);
			var unit = IsMetric() ? Strings.RdlDesigner_Status_cm : Strings.RdlDesigner_Status_in;
			var h = DesignXmlDraw.GetSize(e.Height) / DesignXmlDraw.POINTSIZED;

			if (IsMetric())
			{
				h *= 2.54f;
			}

			statusPosition.Text = string.Format("   {1}={0:0.00}{2}        ", h, Strings.RdlDesigner_Status_Height, unit);
		}

		private void SelectionMoved(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			SetStatusNameAndPosition();
		}

		private void SelectionChanged(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			// handle edit tab first
			if (mc.DesignTab == DesignTabs.Edit)
			{
				SetStatusNameAndPosition();
				return;
			}

			bSuppressChange = true;         // don't process changes in status bar

			SetStatusNameAndPosition();
			this.EnableEditTextBox();   // handling enabling/disabling of textbox

			StyleInfo si = mc.SelectedStyle;
			if (si == null)
				return;

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
				foreColorPicker1.Text = si.Color.IsEmpty ? si.ColorText : ColorTranslator.ToHtml(si.Color);
			}
			if (backColorPicker1 != null)
			{
				backColorPicker1.Text = si.BackgroundColor.IsEmpty ? si.BackgroundColorText : ColorTranslator.ToHtml(si.BackgroundColor);
			}

			bSuppressChange = false;
		}

		private void menuData_Popup(object sender, EventArgs ea)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			bool bEnable = false;
			if (mc != null && mc.DesignTab == DesignTabs.Design)
				bEnable = true;

			this.dataSourcesToolStripMenuItem1.Enabled = this.dataSetsToolStripMenuItem.Enabled = this.embeddedImagesToolStripMenuItem.Enabled = bEnable;
			if (!bEnable)
				return;

			// Run thru all the existing DataSets
			dataSetsToolStripMenuItem.DropDownItems.Clear();
			dataSetsToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem(Strings.RdlDesigner_menuData_Popup_New, null,
						new EventHandler(this.dataSetsToolStripMenuItem_Click)));

			DesignXmlDraw draw = mc.DrawCtl;
			XmlNode rNode = draw.GetReportNode();
			XmlNode dsNode = draw.GetNamedChildNode(rNode, "DataSets");
			if (dsNode != null)
			{
				foreach (XmlNode dNode in dsNode)
				{
					if (dNode.Name != "DataSet")
						continue;
					XmlAttribute nAttr = dNode.Attributes["Name"];
					if (nAttr == null)  // shouldn't really happen
						continue;
					dataSetsToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem(nAttr.Value, null,
						new EventHandler(this.dataSetsToolStripMenuItem_Click)));
				}
			}
		}

		private void dataSourcesToolStripMenuItem1_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.StartUndoGroup(Strings.RdlDesigner_Undo_DataSourcesDialog);
			using (DialogDataSources dlgDS = new DialogDataSources(mc.SourceFile, mc.DrawCtl))
			{
				dlgDS.StartPosition = FormStartPosition.CenterParent;
				DialogResult dr = dlgDS.ShowDialog();
				mc.Editor.EndUndoGroup(dr == DialogResult.OK);
				if (dr == DialogResult.OK)
					mc.Modified = true;
			}
		}

		private void dataSetsToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null || mc.DrawCtl == null || mc.ReportDocument == null)
				return;

			ToolStripMenuItem menu = sender as ToolStripMenuItem;
			if (menu == null)
				return;
			mc.Editor.StartUndoGroup(Strings.RdlDesigner_Undo_DataSetDialog);

			string dsname = menu.Text;

			// Find the dataset we need
			List<XmlNode> ds = new List<XmlNode>();
			DesignXmlDraw draw = mc.DrawCtl;
			XmlNode rNode = draw.GetReportNode();
			XmlNode dsNode = draw.GetCreateNamedChildNode(rNode, "DataSets");
			XmlNode dataset = null;

			// find the requested dataset: the menu text equals the name of the dataset
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
				if (nAttr.Value == dsname)
					dataset = dNode;
			}

			bool bNew = false;
			if (dataset == null)    // This must be the new menu item
			{
				dataset = draw.CreateElement(dsNode, "DataSet", null);  // create empty node
				bNew = true;
			}
			ds.Add(dataset);

			using (PropertyDialog pd = new PropertyDialog(mc.DrawCtl, ds, PropertyTypeEnum.DataSets))
			{
				DialogResult dr = pd.ShowDialog();
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
					mc.Modified = true;
				}
				else if (bNew)  // if canceled and new DataSet get rid of temp node
				{
					dsNode.RemoveChild(dataset);
				}
				if (pd.Delete)  // user must have hit a delete button for this to get set
					dsNode.RemoveChild(dataset);

				if (!dsNode.HasChildNodes)      // If no dataset exists we remove DataSets
					draw.RemoveElement(rNode, "DataSets");

				mc.Editor.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
			}
		}

		private void embeddedImagesToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.StartUndoGroup(Strings.RdlDesigner_Undo_EmbeddedImagesDialog);
			using (DialogEmbeddedImages dlgEI = new DialogEmbeddedImages(mc.DrawCtl))
			{
				dlgEI.StartPosition = FormStartPosition.CenterParent;
				DialogResult dr = dlgEI.ShowDialog();
				mc.Editor.EndUndoGroup(dr == DialogResult.OK);
				if (dr == DialogResult.OK)
					mc.Modified = true;
			}
		}

		private void menuFileNewDataSourceRef_Click(object sender, System.EventArgs e)
		{
			using (DialogDataSourceRef dlgDS = new DialogDataSourceRef())
			{
				dlgDS.StartPosition = FormStartPosition.CenterParent;
				dlgDS.ShowDialog();
				if (dlgDS.DialogResult == DialogResult.Cancel)
					return;
			}
		}

		private async void menuFileNewReport_Click(object sender, System.EventArgs e)
		{
			using (DialogDatabase dlgDB = new DialogDatabase(this))
			{
				dlgDB.StartPosition = FormStartPosition.CenterParent;
				//dlgDB.FormBorderStyle = FormBorderStyle.SizableToolWindow;

				// show modally
				dlgDB.ShowDialog();
				if (dlgDB.DialogResult == DialogResult.Cancel)
					return;
				string rdl = dlgDB.ResultReport;

				// Create the MDI child using the RDL syntax the wizard generates
				MDIChild mc = await CreateMDIChildAsync(null, rdl, false);
				mc.Modified = true;
				// Force building of report names for new reports
				if (mc.DrawCtl.ReportNames == null) { }
			}
		}

		private void menuFilePrint_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
			{
				return;
			}
			if (printChild != null)         // already printing
			{
				MessageBox.Show(Strings.RdlDesigner_Show_PrintOneFile, Strings.RdlDesigner_Show_RDLDesign);
				return;
			}

			printChild = mc;

			PrintDocument pd = new PrintDocument();
			pd.DocumentName = mc.SourceFile.LocalPath;
			pd.PrinterSettings.FromPage = 1;
			pd.PrinterSettings.ToPage = mc.PageCount;
			pd.PrinterSettings.MaximumPage = mc.PageCount;
			pd.PrinterSettings.MinimumPage = 1;
			pd.DefaultPageSettings.Landscape = mc.PageWidth > mc.PageHeight ? true : false;

			// Set the paper size.
			if (mc.SourceRdl != null)
			{
				System.Xml.XmlDocument docxml = new System.Xml.XmlDocument();
				docxml.LoadXml(mc.SourceRdl);

				float height = 11;
				float width = 8.5f;
				XmlNodeList heightList = docxml.GetElementsByTagName("PageHeight");
				for (int i = 0; i < heightList.Count; i++)
				{
					height = Conversions.MeasurementTypeAsHundrethsOfAnInch(heightList[i].InnerText);
				}

				XmlNodeList widthList = docxml.GetElementsByTagName("PageWidth");
				for (int i = 0; i < widthList.Count; i++)
				{
					width = Conversions.MeasurementTypeAsHundrethsOfAnInch(widthList[i].InnerText);
				}

				pd.DefaultPageSettings.PaperSize = new PaperSize("Custom", (int)width, (int)height);
			}
			using (PrintDialog dlg = new PrintDialog())
			{
				dlg.Document = pd;
				dlg.AllowSelection = true;
				dlg.AllowSomePages = true;
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					try
					{
						if (pd.PrinterSettings.PrintRange == PrintRange.Selection)
						{
							pd.PrinterSettings.FromPage = mc.PageCurrent;
						}
						mc.Print(pd);
					}
					catch (Exception ex)
					{
						MessageBox.Show(Strings.RdlDesigner_Show_PrintError + ex.Message, Strings.RdlDesigner_Show_RDLDesign);
					}
				}
				printChild = null;
			}
		}

		private void menuFileSave_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!mc.FileSave())
				return;

			if (this.SavedFileEvent != null)
			{
				this.SavedFileEvent(this, new RdlDesignerSavedFileEvent(mc.SourceFile));
			}

			NoteRecentFiles(mc.SourceFile, true);

			if (mc.Editor != null)
				mc.Editor.ClearUndo();

			return;
		}

		private async void exportToolStripMenuItemCsv_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
            
            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.CSV);
            });
		}

		private async void exportToolStripMenuItemExcel_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
            
            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.ExcelTableOnly);
            });
		}

		private async void exportToolStripMenuItemRtf_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.RTF);
            });
		}

		private async void exportToolStripMenuItemXml_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.XML);
            });
		}

		private async void exportToolStripMenuItemHtml_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
            
            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.HTML);
            });
		}

		private async void exportToolStripMenuItemMHtml_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.MHTML);
            });
		}

		private async void exportToolStripMenuItemPdf_Click(object sender, EventArgs e)
		{
			await PdfExportAsync(false);
		}

		private async Task PdfExportAsync(bool oldStyle)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
			    if (oldStyle)
			    {
				    await mc.ExportAsync(Rdl.OutputPresentationType.PDFOldStyle);
			    }
			    else
			    {
				    await mc.ExportAsync(Rdl.OutputPresentationType.PDF);
			    }
            });
		}

		private async void exportToolStripMenuItemTif_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.TIF);
            });
		}

		private void menuFileSaveAs_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!mc.FileSaveAs())
				return;

			mc.Viewer.Folder = Path.GetDirectoryName(mc.SourceFile.LocalPath);
			mc.Viewer.ReportName = Path.GetFileNameWithoutExtension(mc.SourceFile.LocalPath);
			mc.Text = Path.GetFileName(mc.SourceFile.LocalPath);


			if (this.SavedFileEvent != null)
			{
				this.SavedFileEvent(this, new RdlDesignerSavedFileEvent(mc.SourceFile));
			}

			NoteRecentFiles(mc.SourceFile, true);

			if (mc.Editor != null)
				mc.Editor.ClearUndo();

			return;
		}

		private void menuEdit_Popup(object sender, EventArgs ea)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			// These menus require an MDIChild in order to work
			RdlEditPreview e = mc == null ? null : mc.RdlEditor;
			bool bNotPreview = true;

			foreach (object a in this.editToolStripMenuItem.DropDownItems)
			{
				if (a.GetType() == typeof(ToolStripMenuItem))
				{
					((ToolStripMenuItem)a).Enabled = false;
				}
			}

			if (e == null || e.DesignTab != DesignTabs.Edit)
			{
				undoToolStripMenuItem.Text = e == null ? Strings.RdlDesigner_menuEdit_Popup_Undo : Strings.RdlDesigner_menuEdit_Popup_Undo + " " + e.UndoDescription;
				if (e != null && e.DesignTab == DesignTabs.Preview)
				{
					bNotPreview = false;
					undoToolStripMenuItem.Enabled = true;
					cutToolStripMenuItem.Enabled = true;
					copyToolStripMenuItem.Enabled = true;
					pasteToolStripMenuItem.Enabled = true;
					deleteToolStripMenuItem.Enabled = true;
					findToolStripMenuItem.Enabled = true;
					selectAllToolStripMenuItem.Enabled = true;
				}
				else
				{
					undoToolStripMenuItem.Enabled = true;
					cutToolStripMenuItem.Enabled = true;
					copyToolStripMenuItem.Enabled = true;
					pasteToolStripMenuItem.Enabled = true;
					deleteToolStripMenuItem.Enabled = true;
					selectAllToolStripMenuItem.Enabled = true;
				}



				if (mc == null || e == null)
				{
					undoToolStripMenuItem.Enabled = redoToolStripMenuItem.Enabled =
						cutToolStripMenuItem.Enabled = copyToolStripMenuItem.Enabled =
						pasteToolStripMenuItem.Enabled = deleteToolStripMenuItem.Enabled =
						selectAllToolStripMenuItem.Enabled =
						findToolStripMenuItem.Enabled = false;
					return;
				}
			}
			else
			{
				undoToolStripMenuItem.Text = Strings.RdlDesigner_menuEdit_Popup_Undo;
				undoToolStripMenuItem.Enabled = true;
				redoToolStripMenuItem.Enabled = true;
				cutToolStripMenuItem.Enabled = true;
				copyToolStripMenuItem.Enabled = true;
				pasteToolStripMenuItem.Enabled = true;
				deleteToolStripMenuItem.Enabled = true;
				selectAllToolStripMenuItem.Enabled = true;
				findToolStripMenuItem.Enabled = true;
				findNextToolStripMenuItem.Enabled = true;
				replaceToolStripMenuItem.Enabled = true;
				goToToolStripMenuItem.Enabled = true;
				formatXMLToolStripMenuItem.Enabled = true;

				bool bAnyText = e.Text.Length > 0;          // any text to search at all?
				findToolStripMenuItem.Enabled = findNextToolStripMenuItem.Enabled =
					replaceToolStripMenuItem.Enabled = goToToolStripMenuItem.Enabled = bAnyText;
			}
			undoToolStripMenuItem.Enabled = e.CanUndo && bNotPreview;
			redoToolStripMenuItem.Enabled = e.CanRedo && bNotPreview;
			bool bSelection = e.SelectionLength > 0;    // any text selected?
			cutToolStripMenuItem.Enabled = bSelection && bNotPreview;
			copyToolStripMenuItem.Enabled = bSelection;
			pasteToolStripMenuItem.Enabled = Clipboard.GetDataObject().GetDataPresent(DataFormats.Text) && bNotPreview;
			deleteToolStripMenuItem.Enabled = bSelection && bNotPreview;
			selectAllToolStripMenuItem.Enabled = bNotPreview;

		}

		private void menuEditUndo_Click(object sender, System.EventArgs ea)
		{
			if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
			{
				ctlEditTextbox.Undo();
				return;
			}

			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (e.CanUndo == true)
			{
				e.Undo();

				MDIChild mc = this.ActiveMdiChild as MDIChild;
				if (mc != null && mc.DesignTab == DesignTabs.Design)
				{
					e.DesignCtl.SetScrollControls();
				}
				this.SelectionChanged(this, new EventArgs());
			}
		}

		private void menuEditRedo_Click(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (e.CanRedo == true)
			{
				e.Redo();
			}
		}

		private void menuEditCut_Click(object sender, System.EventArgs ea)
		{
			if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
			{
				ctlEditTextbox.Cut();
				return;
			}

			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (e.SelectionLength > 0)
				e.Cut();
		}

		private void menuEditCopy_Click(object sender, System.EventArgs ea)
		{
			if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
			{
				ctlEditTextbox.Copy();
				return;
			}
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			RdlEditPreview e = mc.RdlEditor;
			if (e == null)
				return;

			if (e.SelectionLength > 0)
				e.Copy();
		}

		private void menuEditPaste_Click(object sender, System.EventArgs ea)
		{
			if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
			{
				ctlEditTextbox.Paste();
				return;
			}

			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text) == true ||
				Clipboard.GetDataObject().GetDataPresent(DataFormats.Bitmap) == true)
				e.Paste();
		}

		private void menuEditDelete_Click(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (e.SelectionLength > 0)
				e.SelectedText = "";
		}

		private void menuEditProperties_Click(object sender, System.EventArgs ea)
		{
			//RdlEditPreview e = GetEditor();
			//if (e == null)
			//    return;

			//e.DesignCtl.menuProperties_Click();
			ShowProperties(!_ShowProperties);
		}

		internal void ShowProperties(bool bShow)
		{
			_ShowProperties = bShow;
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null || !_ShowProperties || mc.DesignTab != DesignTabs.Design)
				mainProperties.ResetSelection(null, null);
			else
				mainProperties.ResetSelection(mc.RdlEditor.DrawCtl, mc.RdlEditor.DesignCtl);

			if (mc != null && !_ShowProperties)
				mc.SetFocus();
			mainProperties.Visible = mainSP.Visible = _ShowProperties;
			propertiesWindowsToolStripMenuItem.Checked = _ShowProperties;
		}

		private void menuEditSelectAll_Click(object sender, System.EventArgs ea)
		{
			if (this.ctlEditTextbox != null && ctlEditTextbox.Focused)
			{
				ctlEditTextbox.SelectAll();
				return;
			}
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			e.SelectAll();
		}

		private async void menuEditFind_Click(object sender, System.EventArgs ea)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			// These menus require an MDIChild in order to work
			RdlEditPreview e = mc == null ? null : mc.RdlEditor;

			if (e == null)
				return;
			if (e.DesignTab == DesignTabs.Preview)
			{
				if (!e.PreviewCtl.ShowFindPanel)
					e.PreviewCtl.ShowFindPanel = true;
				await e.PreviewCtl.FindNext();
			}
			else
			{
				FindTab tab = e.FindTab;
				if (tab == null)
					tab = new FindTab(e);
				tab.tcFRG.SelectedTab = tab.tabFind;
				tab.Show();
			}
		}

		private void menuEditFindNext_Click(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			FindTab tab = e.FindTab;
			if (tab == null)
				tab = new FindTab(e);

			tab.tcFRG.SelectedTab = tab.tabFind;
			tab.Show();
			tab.FindNextClick();
		}

		private void menuEdit_FormatXml(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			if (e.Text.Length > 0)
			{
				try
				{
					e.Text = DesignerUtility.FormatXml(e.Text);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, Strings.RdlDesigner_Showl_FormatXML);
				}
			}
		}

		private void menuEditReplace_Click(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			FindTab tab = e.FindTab;
			if (tab == null)
				tab = new FindTab(e);

			tab.tcFRG.SelectedTab = tab.tabReplace;
			tab.Show();
			tab.Focus();
		}

		private void menuEditGoto_Click(object sender, System.EventArgs ea)
		{
			RdlEditPreview e = GetEditor();
			if (e == null)
				return;

			FindTab tab = e.FindTab;
			if (tab == null)
				tab = new FindTab(e);

			tab.tcFRG.SelectedTab = tab.tabGoTo;
			tab.Show();
			tab.Focus();
		}

		private void menuHelpAbout_Click(object sender, System.EventArgs ea)
		{
			using (DialogAbout dlg = new DialogAbout())
			{
				dlg.ShowDialog();
			}
		}

		private void menuHelpHelp_Click(object sender, System.EventArgs ea)
		{
			try
			{
				System.Diagnostics.Process.Start(HelpUrl);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + Strings.RdlDesigner_Show_ResettingHelpURL, Strings.RdlDesigner_Show_HelpURLInvalid);
				_HelpUrl = DefaultHelpUrl;
			}
		}

		private void menuHelpSupport_Click(object sender, System.EventArgs ea)
		{
			try
			{
                using var p = new System.Diagnostics.Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = SupportUrl,
                        UseShellExecute = true
                    }
                };
                p.Start();

            }
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + Strings.RdlDesigner_Show_ResettingSupportURL, Strings.RdlDesigner_Show_SupportURLInvalid);
				_SupportUrl = DefaultSupportUrl;
			}
		}

		internal RdlEditPreview GetEditor()
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return null;
			return mc.Editor;
		}

		private void menuTools_Popup(object sender, EventArgs e)
		{
			if (_ServerProcess != null && _ServerProcess.HasExited)
				_ServerProcess = null;
			startDesktopServerToolStripMenuItem.Text = this._ServerProcess == null ? Strings.RdlDesigner_menuTools_Popup_StartDesktop : Strings.RdlDesigner_menuTools_Popup_StopDesktop;

			MDIChild mc = this.ActiveMdiChild as MDIChild;
			this.validateRDLToolStripMenuItem.Enabled = (mc != null && mc.DesignTab == DesignTabs.Edit);
		}

		private void menuToolsProcess_Click(object sender, EventArgs e)
		{
			if (_ServerProcess == null)
				menuToolsStartProcess(true);
			else
				menuToolsCloseProcess(true);
		}

		internal void menuToolsStartProcess(bool bMsg)
		{
			if (_ServerProcess != null && !_ServerProcess.HasExited)
				return;

			string pswd = GetPassword();

			try
			{
				string filename = string.Format("{0}{1}",
					AppDomain.CurrentDomain.BaseDirectory, "RdlDesktop.exe");

				ProcessStartInfo psi = new ProcessStartInfo(filename);
				if (pswd != null)
					psi.Arguments = "/p" + pswd;
				psi.RedirectStandardError = psi.RedirectStandardInput = psi.RedirectStandardOutput = true;
				psi.UseShellExecute = false;
				//psi.WindowStyle = ProcessWindowStyle.Hidden;
				psi.CreateNoWindow = true;
				_ServerProcess = Process.Start(psi);
			}
			catch (Exception ex)
			{
				if (bMsg)
					MessageBox.Show(ex.Message, Strings.RdlDesigner_Show_UnableStartDesktop);
			}

			return;
		}

		internal void menuToolsCloseProcess(bool bMsg)
		{
			if (_ServerProcess == null)
				return;
			if (!_ServerProcess.HasExited)
			{
				try
				{
					_ServerProcess.StandardInput.WriteLine("x");    // x stops the server
				}
				catch (Exception ex)
				{
					if (bMsg)
						MessageBox.Show(ex.Message, Strings.RdlDesigner_Show_ErrorStopProcess);
				}
			}
			_ServerProcess = null;
		}

		private void menuToolsOptions_Click(object sender, EventArgs e)
		{
			string SaveUnits = MeasureUnits;
			using (DialogToolOptions dlg = new DialogToolOptions(this))
			{
				DialogResult rc = dlg.ShowDialog();
				if (SaveUnits != MeasureUnits)
				{
					MDIChild mc = this.ActiveMdiChild as MDIChild;
					if (mc == null)
						return;
					if (mc.Editor == null)
					{
						return;
					}
					mc.Editor.DesignCtl._DrawPanel.Invalidate();
					mc.Editor.dcTopRuler.Invalidate();
					mc.Editor.dcLeftRuler.Invalidate();
				}
			}
		}

		private void menuToolsValidateSchema_Click(object sender, EventArgs e)
		{
			if (_ValidateRdl == null)
			{
				_ValidateRdl = new DialogValidateRdl(this);
				_ValidateRdl.Show();
			}
			else
				_ValidateRdl.BringToFront();
			return;
		}

		internal void ValidateSchemaClosing()
		{
			this._ValidateRdl = null;
		}

		private void menuWnd_Popup(object sender, EventArgs e)
		{
			// These menus require an MDIChild in order to work
			bool bEnable = this.MdiChildren.Length > 0 ? true : false;

			cascadeToolStripMenuItem.Enabled = bEnable;
			tileToolStripMenuItem.Enabled = bEnable;
			closeAllToolStripMenuItem.Enabled = bEnable;
		}

		private void menuWndCascade_Click(object sender, EventArgs e)
		{
			this.LayoutMdi(MdiLayout.Cascade);
		}

		private void menuWndCloseAll_Click(object sender, EventArgs e)
		{
			foreach (Form f in this.MdiChildren)
			{
				f.Close();
			}
		}

		private void menuWndCloseAllButCurrent_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			foreach (Form f in this.MdiChildren)
			{
				if (mc == f as MDIChild)
					continue;
				f.Close();
			}
			return;
		}

		private void menuWndTileH_Click(object sender, EventArgs e)
		{
			this.LayoutMdi(MdiLayout.TileHorizontal);
		}

		private void menuWndTileV_Click(object sender, EventArgs e)
		{
			this.LayoutMdi(MdiLayout.TileVertical);
		}

		private async void menuRecentItem_Click(object sender, System.EventArgs e)
		{
			ToolStripMenuItem m = (ToolStripMenuItem)sender;

			int si = m.Text.IndexOf(" ");
			Uri file = new Uri(m.Text.Substring(si + 1));

			await CreateMDIChildAsync(file, null, true);
		}

		private void RdlDesigner_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveStartupState();
			menuToolsCloseProcess(false);
			CleanupTempFiles();
		}

		private void NoteRecentFiles(Uri name, bool bResetMenu)
		{
			if (name == null)
			{
				return;
			}

			if (_RecentFiles.ContainsValue(name.LocalPath))
			{   // need to move it to top of list; so remove old one
				int loc = _RecentFiles.IndexOfValue(name.LocalPath);
				_RecentFiles.RemoveAt(loc);
			}
			if (_RecentFiles.Count >= _RecentFilesMax)
			{
				_RecentFiles.RemoveAt(0);   // remove the first entry
			}
			_RecentFiles.Add(DateTime.Now, name.LocalPath);
			if (bResetMenu)
			{
				RecentFilesMenu();
			}
			return;
		}

		internal void RecentFilesMenu()
		{

			recentFilesToolStripMenuItem.DropDownItems.Clear();
			int mi = 1;
			for (int i = _RecentFiles.Count - 1; i >= 0; i--)
			{
				string menuText = string.Format("&{0} {1}", mi++, _RecentFiles.Values[i]);
				ToolStripMenuItem m = new ToolStripMenuItem(menuText);
				m.Click += new EventHandler(this.menuRecentItem_Click);
				recentFilesToolStripMenuItem.DropDownItems.Add(m);
			}
		}

		internal void ResetPassword()
		{
			bGotPassword = false;
			_DataSourceReferencePassword = null;
		}

		internal string GetPassword()
		{
			if (bGotPassword)
				return _DataSourceReferencePassword;

			using (DataSourcePassword dlg = new DataSourcePassword())
			{
				DialogResult rc = dlg.ShowDialog();
				bGotPassword = true;
				if (rc == DialogResult.OK)
					_DataSourceReferencePassword = dlg.PassPhrase;

				return _DataSourceReferencePassword;
			}
		}

		private async Task GetStartupStateAsync()
		{
			_RecentFiles = new SortedList<DateTime, string>();
			_CurrentFiles = new List<Uri>();
			_HelpUrl = DefaultHelpUrl;              // set as default
			_SupportUrl = DefaultSupportUrl;

			try
			{
				XmlDocument xDoc = new XmlDocument();
				xDoc.PreserveWhitespace = false;
                #if NET6_0_OR_GREATER
                string xml = await System.IO.File.ReadAllTextAsync(optFileName);
                #else
                string xml = System.IO.File.ReadAllText(optFileName);
                #endif
				xDoc.LoadXml(xml);
				XmlNode xNode;
				xNode = xDoc.SelectSingleNode("//designerstate");

				string[] args = Environment.GetCommandLineArgs();
				for (int i = 1; i < args.Length; i++)
				{
					string larg = args[i].ToLower();
					if (larg == "/m" || larg == "-m")
						continue;

					if (File.Exists(args[i]))           // only add it if it exists
					{
						_CurrentFiles.Add(new Uri(args[i]));
					}
				}

				// Loop thru all the child nodes
				foreach (XmlNode xNodeLoop in xNode.ChildNodes)
				{
					switch (xNodeLoop.Name)
					{
						case "RecentFiles":
							DateTime now = DateTime.Now;
							now = now.Subtract(new TimeSpan(0, 1, 0, 0, 0));    // subtract an hour
							foreach (XmlNode xN in xNodeLoop.ChildNodes)
							{
								Uri file = new Uri(xN.InnerText.Trim());
								if (File.Exists(file.LocalPath)) // only add it if it exists 	
								{
									_RecentFiles.Add(now, file.LocalPath);
									now = now.AddSeconds(1);
								}
							}
							break;
						case "RecentFilesMax":
							try
							{
								this._RecentFilesMax = Convert.ToInt32(xNodeLoop.InnerText);
							}
							catch
							{
								this._RecentFilesMax = 5;
							}
							break;
						case "CurrentFiles":
							if (_CurrentFiles.Count > 0)    // don't open other current files if opened with argument
								break;
							foreach (XmlNode xN in xNodeLoop.ChildNodes)
							{
								Uri file = new Uri(xN.InnerText.Trim());
								if (File.Exists(file.LocalPath)) // only add it if it exists 
								{
									_CurrentFiles.Add(file);
								}
							}
							break;
						case "Toolbar":
							_Toolbar = new List<string>();
							foreach (XmlNode xN in xNodeLoop.ChildNodes)
							{
								string item = xN.InnerText.Trim();
								_Toolbar.Add(item);
							}
							break;
						case "Help":
							if (xNodeLoop.InnerText.Length > 0)     //empty means to use the default
								_HelpUrl = xNodeLoop.InnerText;
							break;
						case "Support":
							if (xNodeLoop.InnerText.Length > 0)     //empty means to use the default
								_SupportUrl = xNodeLoop.InnerText;
							break;
						case "EditLines":
							_ShowEditLines = (xNodeLoop.InnerText.ToLower() == "true");
							break;
						case "ShowPreviewWaitDialog":
							_ShowPreviewWaitDialog = (xNodeLoop.InnerText.ToLower() == "true");
							break;
						case "OutlineReportItems":
							this.ShowReportItemOutline = (xNodeLoop.InnerText.ToLower() == "true");
							break;
						case "ShowTabbedInterface":
							this._ShowTabbedInterface = (xNodeLoop.InnerText.ToLower() == "true");
							break;
						case "PropertiesLocation":
							this._PropertiesLocation = GetPropertiesDockStyle(xNodeLoop.InnerText);
							break;
						case "PropertiesAutoHide":
							this._PropertiesAutoHide = (xNodeLoop.InnerText.ToLower() == "true");
							break;
						case "XmlNewLine":
							if (Enum.TryParse<NewLineChar>(xNodeLoop.InnerText, out NewLineChar newLine))
								this._XmlNewLine = newLine;
							break;
						case "MapSubtypes":
							RdlDesigner.MapSubtypes = xNodeLoop.InnerText.Split(new char[] { ',' });
							break;
						case "CustomColors":
							break;
						case "units":
							MeasureUnits = xNodeLoop.InnerText;
							break;
						default:
							break;
					}
				}
			}
			catch (Exception ex)
			{       // Didn't sucessfully get the startup state but don't really care
				Console.WriteLine(string.Format("Exception in GetStartupState ignored.\n{0}\n{1}", ex.Message, ex.StackTrace));
			}

			if (_Toolbar == null)       // Use this as the default toolbar
				_Toolbar = this.ToolbarDefault;
			return;
		}

		private void SaveStartupState()
		{
			try
			{
				int[] colors = GetCustomColors();       // get custom colors

				XmlDocument xDoc = new XmlDocument();
				XmlProcessingInstruction xPI;
				xPI = xDoc.CreateProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
				xDoc.AppendChild(xPI);

				XmlNode xDS = xDoc.CreateElement("designerstate");
				xDoc.AppendChild(xDS);

				XmlNode xN;
				// Loop thru the current files
				XmlNode xFiles = xDoc.CreateElement("CurrentFiles");
				xDS.AppendChild(xFiles);
				foreach (MDIChild mc in this.MdiChildren)
				{
					Uri file = mc.SourceFile;
					if (file == null)
					{
						continue;
					}
					xN = xDoc.CreateElement("file");
					xN.InnerText = file.LocalPath;
					xFiles.AppendChild(xN);
				}

				// Recent File Count
				XmlNode rfc = xDoc.CreateElement("RecentFilesMax");
				xDS.AppendChild(rfc);
				rfc.InnerText = this._RecentFilesMax.ToString();

				// Loop thru recent files list
				xFiles = xDoc.CreateElement("RecentFiles");
				xDS.AppendChild(xFiles);
				foreach (string f in _RecentFiles.Values)
				{
					xN = xDoc.CreateElement("file");
					xN.InnerText = f;
					xFiles.AppendChild(xN);
				}

				// Help File URL
				XmlNode hfu = xDoc.CreateElement("Help");
				xDS.AppendChild(hfu);
				hfu.InnerText = this._HelpUrl;

				// Map chart subtypes
				XmlNode hmap = xDoc.CreateElement("MapSubtypes");
				xDS.AppendChild(hmap);
				StringBuilder maps = new StringBuilder();
				for (int mi = 0; mi < MapSubtypes.Length; mi++)
				{
					maps.Append(MapSubtypes[mi]);
					if (mi + 1 < MapSubtypes.Length)
						maps.Append(',');
				}
				hmap.InnerText = maps.ToString();

				// Show Line numbers
				XmlNode bln = xDoc.CreateElement("EditLines");
				xDS.AppendChild(bln);
				bln.InnerText = this._ShowEditLines ? "true" : "false";

				// Show Preview Wait dialog
				XmlNode pwd = xDoc.CreateElement("ShowPreviewWaitDialog");
				xDS.AppendChild(pwd);
				pwd.InnerText = this.ShowPreviewWaitDialog ? "true" : "false";

				// Outline reportitems
				XmlNode ori = xDoc.CreateElement("OutlineReportItems");
				xDS.AppendChild(ori);
				ori.InnerText = this.ShowReportItemOutline ? "true" : "false";

				// ShowTabbedInterface
				XmlNode sti = xDoc.CreateElement("ShowTabbedInterface");
				xDS.AppendChild(sti);
				sti.InnerText = this._ShowTabbedInterface ? "true" : "false";

				// PropertiesAutoHide
				XmlNode pah = xDoc.CreateElement("PropertiesAutoHide");
				xDS.AppendChild(pah);
				pah.InnerText = this._PropertiesAutoHide ? "true" : "false";

				// XmlNewLine
				XmlNode xnl = xDoc.CreateElement("XmlNewLine");
				xDS.AppendChild(xnl);
				xnl.InnerText = this._XmlNewLine.ToString();

				// PropertiesLocation
				string loc = "right";
				switch (_PropertiesLocation)
				{
					case DockStyle.Left:
						loc = "left";
						break;
					case DockStyle.Top:
						loc = "top";
						break;
					case DockStyle.Bottom:
						loc = "bottom";
						break;
				}
				XmlNode pl = xDoc.CreateElement("PropertiesLocation");
				xDS.AppendChild(pl);
				pl.InnerText = loc;

				// Save the toolbar items
				XmlNode xTB = xDoc.CreateElement("Toolbar");
				xDS.AppendChild(xTB);
				foreach (string t in _Toolbar)
				{
					xN = xDoc.CreateElement("item");
					xN.InnerText = t;
					xTB.AppendChild(xN);
				}

				// Save the custom colors
				StringBuilder sb = new StringBuilder();
				foreach (int c in colors)
				{
					sb.Append(c.ToString());
					sb.Append(",");
				}
				sb.Remove(sb.Length - 1, 1);    // remove last ","

				xN = xDoc.CreateElement("CustomColors");
				xN.InnerText = sb.ToString();
				xDS.AppendChild(xN);

				//
				// Save Current Units used
				//
				xN = xDoc.CreateElement("units");
				xN.InnerText = MeasureUnits;
				xDS.AppendChild(xN);

				// Save the file
				Directory.CreateDirectory(Path.GetDirectoryName(optFileName)); //Create directory if not exist
				xDoc.Save(optFileName);
			}
			catch { }       // still want to leave even on error

			return;
		}

		static internal int[] GetCustomColors()
		{
			int white = 16777215;   // default to white (magic number)
			int[] cArray = new int[] {white, white, white, white,white, white, white, white,
									white, white, white, white, white, white, white, white};
			try
			{
				XmlDocument xDoc = new XmlDocument();
				xDoc.PreserveWhitespace = false;
				xDoc.Load(optFileName);
				XmlNode xNode;
				xNode = xDoc.SelectSingleNode("//designerstate");

				string tcolors = "";
				// Loop thru all the child nodes
				foreach (XmlNode xNodeLoop in xNode.ChildNodes)
				{
					if (xNodeLoop.Name != "CustomColors")
						continue;
					tcolors = xNodeLoop.InnerText;
					break;
				}
				string[] colorList = tcolors.Split(',');
				int i = 0;

				foreach (string c in colorList)
				{
					try { cArray[i] = int.Parse(c); }
					catch { cArray[i] = white; }
					i++;
					if (i >= cArray.Length)     // Only allow 16 custom colors
						break;
				}
			}
			catch
			{       // Didn't sucessfully get the startup state but don't really care
			}
			return cArray;
		}

		static internal void SetCustomColors(int[] colors)
		{
			StringBuilder sb = new StringBuilder();
			foreach (int c in colors)
			{
				sb.Append(c.ToString());
				sb.Append(",");
			}

			sb.Remove(sb.Length - 1, 1);    // remove last ","
			try
			{
				XmlDocument xDoc = new XmlDocument();
				xDoc.PreserveWhitespace = false;
				xDoc.Load(optFileName);
				XmlNode xNode;
				xNode = xDoc.SelectSingleNode("//designerstate");

				// Loop thru all the child nodes
				XmlNode cNode = null;
				foreach (XmlNode xNodeLoop in xNode.ChildNodes)
				{
					if (xNodeLoop.Name == "CustomColors")
					{
						cNode = xNodeLoop;
						break;
					}
				}

				if (cNode == null)
				{
					cNode = xDoc.CreateElement("CustomColors");
					xNode.AppendChild(cNode);
				}

				cNode.InnerText = sb.ToString();

				Directory.CreateDirectory(Path.GetDirectoryName(optFileName)); //Create directory if not exist
				xDoc.Save(optFileName);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, Strings.RdlDesigner_Show_CustomColorSaveFailed);
			}
			return;
		}

		private void EditTextbox_Validated(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null ||
				mc.DesignTab != DesignTabs.Design || mc.DrawCtl.SelectedCount != 1 ||
				mc.Editor == null)
				return;

			mc.Editor.SetSelectedText(ctlEditTextbox.Text);
			SetProperties(mc);
		}

		private void InsertToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctlMenuInsertCurrent != null)
				ctlMenuInsertCurrent.Checked = false;

			ToolStripMenuItem ctl = (ToolStripMenuItem)sender;
			ctl.Checked = true;
			ctlMenuInsertCurrent = ctl.Checked ? ctl : null;

			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.SetFocus();

			mc.CurrentInsert = ctlMenuInsertCurrent == null ? null : (string)ctlMenuInsertCurrent.Tag;
		}

		private void Insert_Click(object sender, EventArgs e)
		{
			if (ctlInsertCurrent != null)
				ctlInsertCurrent.Checked = false;

			ToolStripButton ctl = (ToolStripButton)sender;
			ctlInsertCurrent = ctl.Checked ? ctl : null;

			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.SetFocus();

			mc.CurrentInsert = ctlInsertCurrent == null ? null : (string)ctlInsertCurrent.Tag;
		}

		private void boldToolStripButton1_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.ApplyStyleToSelected("FontWeight", boldToolStripButton1.Checked ? "Bold" : "Normal");
			SetProperties(mc);

			SetMDIChildFocus(mc);
		}

		private void italiacToolStripButton1_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.ApplyStyleToSelected("FontStyle", italiacToolStripButton1.Checked ? "Italic" : "Normal");
			SetProperties(mc);

			SetMDIChildFocus(mc);
		}

		private void underlineToolStripButton2_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.ApplyStyleToSelected("TextDecoration", underlineToolStripButton2.Checked ? "Underline" : "None");
			SetProperties(mc);

			SetMDIChildFocus(mc);
		}

		private void foreColorPicker1_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("Color", foreColorPicker1.Text);
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		private void backColorPicker1_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("BackgroundColor", backColorPicker1.Text);
				SetProperties(mc);
			}

			SetMDIChildFocus(mc);
		}

		private void fontToolStripComboBox1_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("FontFamily", fontToolStripComboBox1.Text);
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		private void fontSizeToolStripComboBox1_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (!bSuppressChange)
			{
				mc.ApplyStyleToSelected("FontSize", fontSizeToolStripComboBox1.Text + "pt");
				SetProperties(mc);
			}
			SetMDIChildFocus(mc);
		}

		private void selectToolStripButton2_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.SelectionTool = selectToolStripButton2.Checked;

			SetMDIChildFocus(mc);
		}

		private void zoomToolStripComboBox1_Change(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.SetFocus();

			switch (zoomToolStripComboBox1.Text)
			{
				case "Actual Size":
					mc.Zoom = 1;
					break;
				case "Fit Page":
					mc.ZoomMode = ZoomEnum.FitPage;
					break;
				case "Fit Width":
					mc.ZoomMode = ZoomEnum.FitWidth;
					break;
				default:
					string s = zoomToolStripComboBox1.Text.Substring(0, zoomToolStripComboBox1.Text.Length - 1);
					float z;
					try
					{
						z = Convert.ToSingle(s) / 100f;
						mc.Zoom = z;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, Strings.RdlDesigner_Show_ZoomValueInvalid);
					}
					break;
			}
		}

		private void RdlDesigner_MdiChildActivate(object sender, EventArgs e)
		{
			if (this._ValidateRdl != null)      // don't keep the validation open when window changes
				this._ValidateRdl.Close();

			DesignTabChanged(sender, e);
			SelectionChanged(sender, e);
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.SetFocus();
			if (mc.Tab != null)
				mainTC.SelectTab(mc.Tab);
		}

		private void SetMDIChildFocus(MDIChild mc)
		{
			// We don't want to be triggering any change events when the focus is changing
			bool bSuppress = bSuppressChange;
			bSuppressChange = true;
			mc.SetFocus();
			bSuppressChange = bSuppress;
		}

		private void SetProperties(MDIChild mc)
		{
			if (mc == null || !_ShowProperties || mc.DesignTab != DesignTabs.Design)
				mainProperties.ResetSelection(null, null);
			else
				mainProperties.ResetSelection(mc.RdlEditor.DrawCtl, mc.RdlEditor.DesignCtl);
		}

		private void SetStatusNameAndPosition()
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;

			SetProperties(mc);

			if (mc == null)
			{
				statusPosition.Text = statusSelected.Text = "";
			}
			else if (mc.DesignTab == DesignTabs.Design)
				SetStatusNameAndPositionDesign(mc);
			else if (mc.DesignTab == DesignTabs.Edit)
				SetStatusNameAndPositionEdit(mc);
			else
			{
				statusPosition.Text = statusSelected.Text = "";
			}
			return;
		}

		private void SetStatusNameAndPositionDesign(MDIChild mc)
		{
			if (mc.DrawCtl.SelectedCount <= 0)
			{
				statusPosition.Text = statusSelected.Text = "";
				return;
			}

			// Handle position
			var pos = mc.SelectionPosition;
			var sz = mc.SelectionSize;
			string spos;

			if (pos.X == float.MinValue) // no item selected is probable cause
			{
				spos = "";
			}
			else
			{
				var rinfo = new RegionInfo(CultureInfo.CurrentCulture.Name);
				double m72 = DesignXmlDraw.POINTSIZED;

				var x = pos.X / m72;
				var y = pos.Y / m72;
				var unit = IsMetric() ? Strings.RdlDesigner_Status_cm : Strings.RdlDesigner_Status_in;

				if (IsMetric())
				{
					x *= 2.54d;
					y *= 2.54d;
				}

				if (sz.Width == float.MinValue) // item is in a table/matrix is probably cause
				{
					spos = string.Format("   x={0:0.00}{2}, y={1:0.00}{2}        ",
										 x, y, unit);
				}
				else
				{
					var w = sz.Width / m72;
					var h = sz.Height / m72;

					if (IsMetric())
					{
						w *= 2.54d;
						h *= 2.54d;
					}

					spos = string.Format("   x={0:0.00}{4}, y={1:0.00}{4}, w={2:0.00}{4}, h={3:0.00}{4}        ",
										 x, y, w, h, unit);
				}
			}

			statusPosition.Text = spos;

			// Handle text
			statusSelected.Text = mc.SelectionName;
		}

		private void SetStatusNameAndPositionEdit(MDIChild mc)
		{
			var spos = string.Format("{2} {0}  {3} {1}", mc.CurrentLine, mc.CurrentCh, Strings.RdlDesigner_Status_Ln, Strings.RdlDesigner_Status_Ch);
			statusSelected.Text = spos;
			statusPosition.Text = "";
		}

		private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			// Force scroll up and down
			switch (e.KeyCode)
			{
				case Keys.Enter:
					mc.SetFocus();
					e.Handled = true;
					break;
				case Keys.Escape:
					if (mc.DrawCtl.SelectedCount == 1)
					{
						XmlNode tn = mc.DrawCtl.SelectedList[0] as XmlNode;
						if (tn != null && tn.Name == "Textbox")
						{
							ctlEditTextbox.Text = mc.DrawCtl.GetElementValue(tn, "Value", "");
							e.Handled = true;
						}
					}
					break;
				default:
					break;
			}

		}
		/// <summary>
		/// Enable - Disable options just after a item selected
		/// For group items selected left the original code
		/// For single item selected new code
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuFormat_Popup(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;

			// Determine if group operation on selected is currently allowed
			bool bEnable = (mc != null && mc.DesignTab == DesignTabs.Design && mc.DrawCtl.AllowGroupOperationOnSelected);

			this.bottomsToolStripMenuItem.Enabled = this.centersToolStripMenuItem.Enabled =
				this.leftsToolStripMenuItem.Enabled = this.middlesToolStripMenuItem.Enabled =
				this.rightsToolStripMenuItem.Enabled = this.topsToolStripMenuItem.Enabled =
				bEnable;

			widthToolStripMenuItem.Enabled = heightToolStripMenuItem.Enabled = bothToolStripMenuItem.Enabled = bEnable;

			makeEqualToolStripMenuItem.Enabled = increaseToolStripMenuItem.Enabled = decreaseToolStripMenuItem.Enabled =
				zeroToolStripMenuItem.Enabled = bEnable;

			makeEqualToolStripMenuItem1.Enabled = increaseToolStripMenuItem1.Enabled = decreaseToolStripMenuItem1.Enabled =
				zeroToolStripMenuItem1.Enabled = bEnable;

			bEnable = (mc != null && mc.DesignTab == DesignTabs.Design && mc.DrawCtl.SelectedCount > 0);
			this.increaseToolStripMenuItem5.Enabled =
				this.decreaseToolStripMenuItem5.Enabled =
				this.zeroToolStripMenuItem5.Enabled =
				this.increaseToolStripMenuItem4.Enabled =
				this.decreaseToolStripMenuItem4.Enabled =
				this.zeroToolStripMenuItem4.Enabled =
				this.increaseToolStripMenuItem2.Enabled =
				this.decreaseToolStripMenuItem2.Enabled =
				this.zeroToolStripMenuItem2.Enabled =
				this.increaseToolStripMenuItem3.Enabled =
				this.decreaseToolStripMenuItem3.Enabled =
				this.zeroToolStripMenuItem3.Enabled =
					bEnable;

			//
			// If single Item selected enable alignment to container
			//
			centerHorizontallyToolStripMenuItem.Enabled = false;
			centerVerticallyToolStripMenuItem.Enabled = false;
			centerBothToolStripMenuItem.Enabled = false;
			if (mc.DrawCtl.SelectedCount == 1)
			{
				//string sect = mc.DrawCtl.SectionBelongsTo(mc.DrawCtl.SelectedList[0]);
				//if (sect == "Body")
				//{
				//    centerHorizontallyToolStripMenuItem.Enabled = true;
				//}
				//else
				//{
				centerHorizontallyToolStripMenuItem.Enabled = true;
				centerVerticallyToolStripMenuItem.Enabled = true;
				centerBothToolStripMenuItem.Enabled = true;
				//}
			}
		}

		private void bottomsToolStripMenuItemutton_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

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

			mc.ApplyStyleToSelected("TextAlign", ta.ToString());
			SetProperties(mc);

			SetProperties(mc);
			SetMDIChildFocus(mc);
		}

		private void centersToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignCenters();
			SetProperties(mc);
		}

		private void leftsToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignLefts();
			SetProperties(mc);
		}

		private void rightsToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignRights();
			SetProperties(mc);
		}

		private void bottomsToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignBottoms();
			SetProperties(mc);
		}

		private void topsToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignTops();
			SetProperties(mc);
		}

		private void middlesToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignMiddles();
			SetProperties(mc);
		}

		private void heightToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeHeights();
			SetProperties(mc);
		}

		private void widthToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeWidths();
			SetProperties(mc);
		}

		private void bothToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeBoth();
			SetProperties(mc);
		}

		private void makeEqualToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingMakeEqual();
			SetProperties(mc);
		}

		private void increaseToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingIncrease();
			SetProperties(mc);
		}

		private void decreaseToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingDecrease();
			SetProperties(mc);
		}

		private void zeroToolStripMenuItem_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingMakeZero();
			SetProperties(mc);
		}

		private void makeEqualToolStripMenuItem1_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingMakeEqual();
			SetProperties(mc);
		}

		private void increaseToolStripMenuItem1_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingIncrease();
			SetProperties(mc);
		}

		private void decreaseToolStripMenuItem1_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingDecrease();
			SetProperties(mc);
		}

		private void zeroToolStripMenuItem1_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingMakeZero();
			SetProperties(mc);
		}

		private void menuView_Popup(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			bool bEnable = mc != null;

			designerToolStripMenuItem.Enabled = rDLTextToolStripMenuItem.Enabled =
				previewToolStripMenuItem.Enabled = bEnable;

			propertiesWindowsToolStripMenuItem.Enabled = bEnable && mc.DesignTab == DesignTabs.Design;
		}

		private void menuViewDesigner_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.RdlEditor.DesignTab = DesignTabs.Design;
		}

		private void menuViewRDL_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.RdlEditor.DesignTab = DesignTabs.Edit;
		}

		private void menuViewBrowser_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			try
			{
				menuToolsStartProcess(true);        // start desktop if not already up

				DesktopConfig dc = DialogToolOptions.DesktopConfiguration;

				string rdlfile = Path.GetFileNameWithoutExtension(mc.SourceFile.LocalPath) + "_" + (++TEMPRDL_INC).ToString() + TEMPRDL;
				Uri file;
				if (Path.IsPathRooted(dc.Directory))
				{
					file = new Uri(dc.Directory + Path.DirectorySeparatorChar + rdlfile);
				}
				else
				{
					file = new Uri(AppDomain.CurrentDomain.BaseDirectory +
						dc.Directory + Path.DirectorySeparatorChar + rdlfile);
				}

				if (_TempReportFiles == null)
				{
					_TempReportFiles = new List<Uri>();
					_TempReportFiles.Add(file);
				}
				else
				{
					if (!_TempReportFiles.Contains(file))
					{
						_TempReportFiles.Add(file);
					}
				}
				StreamWriter sw = File.CreateText(file.LocalPath);
				sw.Write(mc.SourceRdl);
				sw.Close();
				// http://localhost:8080/aReport.rdl?rs:Format=HTML
				Uri url = new Uri(string.Format("http://localhost:{0}/{1}?rd:Format=HTML", dc.Port, rdlfile));
				System.Diagnostics.Process.Start(url.AbsoluteUri);

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.RdlDesigner_Show_UnableShowReport);
			}

		}

		private void menuViewPreview_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.RdlEditor.DesignTab = DesignTabs.Preview;
		}

		private void menuFormatPadding_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			ToolStripMenuItem mi = sender as ToolStripMenuItem;

			string padname = null;
			int paddiff = 0;
			if (mi == increaseToolStripMenuItem2)
			{
				padname = "PaddingLeft";
				paddiff = 4;
			}
			else if (mi == decreaseToolStripMenuItem2)
			{
				padname = "PaddingLeft";
				paddiff = -4;
			}
			else if (mi == zeroToolStripMenuItem2)
			{
				padname = "PaddingLeft";
				paddiff = 0;
			}
			else if (mi == increaseToolStripMenuItem3)
			{
				padname = "PaddingRight";
				paddiff = 4;
			}
			else if (mi == decreaseToolStripMenuItem3)
			{
				padname = "PaddingRight";
				paddiff = -4;
			}
			else if (mi == zeroToolStripMenuItem3)
			{
				padname = "PaddingRight";
				paddiff = 0;
			}
			else if (mi == increaseToolStripMenuItem4)
			{
				padname = "PaddingTop";
				paddiff = 4;
			}
			else if (mi == decreaseToolStripMenuItem4)
			{
				padname = "PaddingTop";
				paddiff = -4;
			}
			else if (mi == zeroToolStripMenuItem4)
			{
				padname = "PaddingTop";
				paddiff = 0;
			}
			else if (mi == increaseToolStripMenuItem5)
			{
				padname = "PaddingBottom";
				paddiff = 4;
			}
			else if (mi == decreaseToolStripMenuItem5)
			{
				padname = "PaddingBottom";
				paddiff = -4;
			}
			else if (mi == zeroToolStripMenuItem5)
			{
				padname = "PaddingBottom";
				paddiff = 0;
			}

			if (padname != null)
			{
				mc.Editor.DesignCtl.SetPadding(padname, paddiff);
				SetProperties(mc);
			}

		}

		private void CleanupTempFiles()
		{
			if (_TempReportFiles == null)
			{
				return;
			}
			foreach (Uri file in _TempReportFiles)
			{
				try
				{   // It's ok for the delete to fail
					File.Delete(file.LocalPath);
				}
				catch
				{ }
			}
			_TempReportFiles = null;
		}

		private void menuFormatAlignButton_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

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

			mc.ApplyStyleToSelected("TextAlign", ta.ToString());
			SetProperties(mc);

			SetProperties(mc);
			SetMDIChildFocus(mc);
		}

		private void menuFormatAlignC_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignCenters();
			SetProperties(mc);
		}

		private void menuFormatAlignL_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignLefts();
			SetProperties(mc);
		}

		private void menuFormatAlignR_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignRights();
			SetProperties(mc);
		}

		private void menuFormatAlignB_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignBottoms();
			SetProperties(mc);
		}

		private void menuFormatAlignT_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignTops();
			SetProperties(mc);
		}

		private void menuFormatAlignM_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.AlignMiddles();
			SetProperties(mc);
		}

		private void menuFormatSizeH_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeHeights();
			SetProperties(mc);
		}

		private void menuFormatSizeW_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeWidths();
			SetProperties(mc);
		}

		private void menuFormatSizeB_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.SizeBoth();
			SetProperties(mc);
		}

		private void menuFormatHorzE_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingMakeEqual();
			SetProperties(mc);
		}

		private void menuFormatHorzI_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingIncrease();
			SetProperties(mc);
		}

		private void menuFormatHorzD_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingDecrease();
			SetProperties(mc);
		}

		private void menuFormatHorzZ_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorzSpacingMakeZero();
			SetProperties(mc);
		}

		private void menuFormatVertE_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingMakeEqual();
			SetProperties(mc);
		}

		private void menuFormatVertI_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingIncrease();
			SetProperties(mc);
		}

		private void menuFormatVertD_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingDecrease();
			SetProperties(mc);
		}

		private void menuFormatVertZ_Click(object sender, System.EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VertSpacingMakeZero();
			SetProperties(mc);
		}

		private async void RdlDesigner_DragDrop(object sender, DragEventArgs e)
		{
			try
			{
				string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
				int i;
				for (i = 0; i < s.Length; i++)
				{
					if (s[i].ToLower().EndsWith(".rdl"))
					{
						await CreateMDIChildAsync(new Uri(s[i]), null, false);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.RdlDesigner_ShowD_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void RdlDesigner_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.All;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private async void pDFToolStripMenuItem_Click(object sender, EventArgs e)
		{
			await PdfExportAsync(false);
		}

		private async void pDFOldStyleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			await PdfExportAsync(true);
		}

		private void tIFFToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemTif_Click(sender, e);
		}

		private void excelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemExcel_Click(sender, e);
		}

		private void xMLToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemXml_Click(sender, e);
		}

		private void webArchiveSingleFileMHTToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemMHtml_Click(sender, e);
		}

		private void webPageHTMLToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemHtml_Click(sender, e);
		}

		private void cSVToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemCsv_Click(sender, e);
		}

		private void rTFDOCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportToolStripMenuItemRtf_Click(sender, e);
		}

		private async void dOCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			await mc.ExportAsync(Rdl.OutputPresentationType.Word);
			return;
		}

		private async void Excel2007ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
            
            await ShowSaveInProgressAsync(mc.Tab.Text,async () => {
                await mc.ExportAsync(Rdl.OutputPresentationType.Excel2007);
            });
        }
        
		private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			menuFileExit_Click(sender, e);
		}

		private void ZoomControl1_ValueChanged(object sender, UserZoomControl.CambiaValori e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			if (mc.Editor == null)
			{
				return;
			}


			mc.Editor.DesignCtl.SCALEX = e.ValoreZoom;
			mc.Editor.DesignCtl.SCALEY = e.ValoreZoom;

			mc.Editor.DesignCtl._DrawPanel.SCALAX = e.ValoreZoom;
			mc.Editor.DesignCtl._DrawPanel.SCALAY = e.ValoreZoom;

			// Applicare la scala anche alla dimensione del disegno

			mc.Editor.DesignCtl._DrawPanel.ReportVisualSize = mc.Editor.DesignCtl._DrawPanel.ReportVisualSizeBase * mc.Editor.DesignCtl._DrawPanel.SCALAX;
			mc.Editor.DesignCtl._DrawPanel.Invalidate();

			mc.Editor.DesignCtl.HorizontalScroll.Visible = true;
			mc.Editor.DesignCtl.VerticalScroll.Visible = true;
			mc.Editor.DesignCtl.VerticalScroll.Enabled = true;

			mc.Editor.dcTopRuler.Invalidate();
			mc.Editor.dcLeftRuler.Invalidate();
			mc.Editor.DesignCtl._hScroll.Maximum = (int)mc.Editor.DesignCtl._DrawPanel.ReportVisualSize;

			mc.Editor.DesignCtl._vScroll.Maximum = (int)(mc.Editor.DesignCtl._DrawPanel.VerticalMax * mc.Editor.DesignCtl._DrawPanel.SCALAY);
		}

		/// <summary>
		/// Enable Alignment grid on RdlEditor
		/// </summary>
		/// <param name = "sender" ></ param >
		/// < param name="e"></param>
		private void AlignmentGridEnable_CheckStateChanged(object sender, EventArgs e)
		{
			foreach (var form in MdiChildren)
			{
				MDIChild mc = form as MDIChild;
				if (mc == null)
					continue;
				if (mc.Editor == null)
				{
					continue;
				}
				mc.Editor.DesignCtl._DrawPanel.EnableDrawGriglia = AlignmentGridEnable.Checked;
				mc.Editor.DesignCtl._DrawPanel.Invalidate();
				mc.Editor.dcTopRuler.Invalidate();
				mc.Editor.dcLeftRuler.Invalidate();
			}
		}

		private void CenterHorizontallyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.HorizontalCenterInsideContainer();
			SetProperties(mc);
		}

		private void CenterVerticallyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;

			mc.Editor.DesignCtl.VerticalCenterInsideContainer();
			SetProperties(mc);
		}

		private void CenterBothToolStripMenu_Click(object sender, EventArgs e)
		{
			MDIChild mc = this.ActiveMdiChild as MDIChild;
			if (mc == null)
				return;
			mc.Editor.DesignCtl.HorizontalCenterInsideContainer();
			mc.Editor.DesignCtl.VerticalCenterInsideContainer();
			SetProperties(mc);
		}
        
        private async Task ShowSaveInProgressAsync(string fileName,
            Func<Task> operation)
        {
            toolStripStatusLabel2.Text = $"Saving {fileName}";
            try
            {
                await operation();
                toolStripStatusLabel2.Text = $"Saved {fileName}";
            }
            catch
            {
                toolStripStatusLabel2.Text = $"Failed to save {fileName}";
            }
            
            await Task.Delay(4000);
            toolStripStatusLabel2.Text = string.Empty;
        }

	}

	public class RdlIpcObject : MarshalByRefObject
	{
		public RdlIpcObject()
		{
		}

		private List<string> _commands;

		//public List<string> Commands
		//{
		// get { return _commands; }
		// set { _commands = value; }
		//}

		public List<string> getCommands()
		{
			return _commands;
		}

		public void setCommands(List<string> value)
		{
			_commands = value;
		}

        [Obsolete]
        public override object InitializeLifetimeService()
		{
			return null;
		}

	}

	public enum NewLineChar
	{
		[LocalizedDescription("Windows (CR LF)")]
		Windows,
		[LocalizedDescription("Unix (LF)")]
		Unix
	}
}
