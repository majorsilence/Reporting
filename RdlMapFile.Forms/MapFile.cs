using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using Majorsilence.Forms;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using RdlMapFile.Resources;

namespace Majorsilence.Reporting.RdlMapFile
{
    public partial class MapFile : Form, IMessageFilter
    {
        private DesignXmlDraw map;

        public MapFile(string file)
        {

            InitializeComponent();
            //map = new DesignXmlDraw();          // designer keeps deleting this code??!!
            Application.AddMessageFilter(this);
            // 
            // manually add controls to the splitter.  visual studio keeps deleting them
            // 
            map = new DesignXmlDraw();         
            this.splitContainer1.Panel1.Controls.Add(this.map);
            // 
            // map
            // 
            this.map.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.map.Location = new System.Drawing.Point(0, 0);
            this.map.Name = "map";
            this.map.Size = new System.Drawing.Size(620, 474);
            this.map.TabIndex = 0;
            this.map.Zoom = 1F;
            map.ZoomChange += new DesignXmlDraw.DrawEventHandler(map_ZoomChange);
            map.XmlChange += new DesignXmlDraw.DrawEventHandler(map_XmlChange);
            map.SelectionChange += new DesignXmlDraw.DrawEventHandler(map_SelectionChange);
            map.ToolChange += new DesignXmlDraw.DrawEventHandler(map_ToolChange);
            this.Closing += MapFile_Closing;

            if (file != null)
            {
                map.SetMapFile(file);
                if (map.MapDoc == null)      // failed to open?
                    map.SetNew();           //  yes, just start a new one
            }
            else
                map.SetNew();
            SetTitle(false);
        }

        void map_ToolChange(DesignXmlDraw dxd)
        {
            bToolStrip.Image = selectionToolStripMenuItem.Image;
        }

        void MapFile_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !OkToClose();
        }

        void map_SelectionChange(DesignXmlDraw dxd)
        {
            XmlNode sNode = dxd.SelectedItem;
            if (sNode == null)
            {
                pgXmlNode.SelectedObject = null;
                return;
            }

            switch (sNode.Name)
            {
                case "Text":
                    pgXmlNode.SelectedObject = new PropertyText(this.map);
                    break;
                case "Polygon":
                    pgXmlNode.SelectedObject = new PropertyPolygon(this.map);
                    // expand the Keys property.  Makes it easier for user to identify the polygon
                    GridItem keys = findPropertyItem("Keys", findPropertyRoot());
                    if (keys != null)
                        keys.Expanded = true;
                    break;
                case "Lines":
                    pgXmlNode.SelectedObject = new PropertyLine(this.map);
                    break;
                default:
                    pgXmlNode.SelectedObject = null;
                    break;
            }

        }
        
        private GridItem findPropertyRoot()
        {
            // get the root item
            GridItem root = pgXmlNode.SelectedGridItem;
            if (root == null)
                return null;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            return root;
        }
        
        private GridItem findPropertyItem(string label, GridItem root) 
        {
            if (root == null)
                return null;
            if (root.Label == label)
                return root;
            // search the property grid's item tree for the indicated item
            foreach (GridItem gi in root.GridItems)
            {
                GridItem result = findPropertyItem(label, gi);
                if (result != null)
                    return result;
            }
            return null;
        }
 
        void map_XmlChange(DesignXmlDraw dxd)
        {
            pgXmlNode.Refresh();
            if (map.Modified)           // only need to process on the first change
                return;
            SetTitle(true);
        }

        void map_ZoomChange(DesignXmlDraw dxd)
        {
            string z = string.Format("{0}%", Math.Round(dxd.Zoom * 100, 0));
            cbZoom.Text = z;
        }
        // Windows-only P/Invoke cross-app mousewheel routing (WindowFromPoint/SendMessage) --
        // already dead under Majorsilence.Forms (Application.AddMessageFilter is a documented
        // no-op) and would fail at runtime on Linux/macOS anyway. Same pattern as D3 (RdlReader.cs)
        // and D4 (RdlDesigner.cs). Gutted to a no-op rather than ported.
        public bool PreFilterMessage(ref Message m)
        {
            return false;
        }

        // P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!OkToClose())
                return;
            Application.Exit();

        }

        private bool OkToClose()
        {
            if (!map.Modified)
                return true;

            DialogResult mb = MessageBox.Show(string.Format(Strings.MapFile_ShowB_WantSave, map.File == null ? Strings.MapFile_ShowB_Untitled : Path.GetFileName(map.File)),
                Strings.MapFile_ShowB_RdlMapFileDesigner, MessageBoxButtons.YesNoCancel) ;
            if (mb == DialogResult.Cancel)
                return false;

            if (mb == DialogResult.No)
                return true;

            
            return Save();
        }

        private bool Save()
        {
            // need to save file first then exit
            if (map.File == null)
            {
                // SaveAs is async (FileDialog.ShowDialog(Form) is async in Majorsilence.Forms),
                // but Save/OkToClose have synchronous callers (e.g. the Closing event handler),
                // so bridge synchronously here -- same pattern as D4's MDIChild.FileSave().
                if (!System.Threading.Tasks.Task.Run(async () => await SaveAsAsync()).GetAwaiter().GetResult())
                    return false;
            }
            string file = map.File;

            StreamWriter writer = null;
            bool bOK = true;
            try
            {
                writer = new StreamWriter(file);
                writer.Write(map.MapSource);
                map.Modified = false;
                map.ClearUndo();
                SetTitle(false);
            }
            catch (Exception ae)
            {
                bOK = false;
                MessageBox.Show(ae.Message + "\r\n" + ae.StackTrace);
            }
            finally
            {
                writer.Close();
            }
            if (bOK)
                map.Modified = false;
            return bOK;
                   
        }
/// <summary>
/// Asks user for file name and sets the map file name to user specified one
/// </summary>
/// <returns></returns>
        private async System.Threading.Tasks.Task<bool> SaveAsAsync()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "xml";
            sfd.Filter = Strings.MapFile_SaveAs_RDL_MapFilesFilter;
            sfd.FilterIndex = 1;
            sfd.CheckFileExists = false;
            bool rc = false;
            try
            {
                if (await sfd.ShowDialog(this) == DialogResult.OK)
                {
                    map.File = sfd.FileName;
                    rc = true;
                }
            }
            finally
            {
                sfd.Dispose();
            }
            return rc;
        }

        private void SetTitle(bool bModified)
        {
            var title = Strings.MapFile_SetTitle_fyiReporting_MapFile_Designer + " - " +
                (map.File ?? Strings.MapFile_SetTitle_untitled) +
                (bModified ? "*" : "");

            Text = title;
        }
        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!OkToClose())
                return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = "xml";
            ofd.Filter = Strings.MapFile_openToolStripMenuItem_Click_OpenMap;
            ofd.FilterIndex = 1;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;
            if (map.File != null)
            {
                try
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(map.File);
                }
                catch
                {
                }
            }

            try
            {
                if (await ofd.ShowDialog(this) == DialogResult.OK)
                {
                    map.SetMapFile(ofd.FileName);
                    SetTitle(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, string.Format("{1} {0}", ofd.FileName, Strings.MapFile_openToolStripMenuItem_Click_ErrorOpening));
            }
            finally
            {
                ofd.Dispose();
            }

        }

        private async void setBackgroundImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.MapFile_setBackgroundImageToolStripMenuItem_Click_OpenPicture;
            ofd.FilterIndex = 6;
            ofd.CheckFileExists = true;
            try
            {
                if (await ofd.ShowDialog(this) == DialogResult.OK)
                {
                    map.SetBackgroundImage(ofd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, string.Format("{1} {0}", ofd.FileName, Strings.MapFile_openToolStripMenuItem_Click_ErrorOpening));
            }
            finally
            {
                ofd.Dispose();
            }

        }

        private void cbZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                float z = int.Parse(cbZoom.Text.Replace("%", ""), System.Globalization.NumberStyles.Integer) / 100f;
                if (z < .1f)
                    z = .1f;
                else if (z > 10)
                    z = 8;
                this.map.Zoom = z;
            }
            catch { }       // happens when user types in a bad zoom value
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.map.DeleteSelected();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.map.Undo();
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            undoToolStripMenuItem.Enabled = map.CanUndo;
			undoToolStripMenuItem.Text = map.CanUndo ? Strings.MapFile_editToolStripMenuItem_DropDownOpening_Undo + " " + map.UndoText : Strings.MapFile_editToolStripMenuItem_DropDownOpening_Undo;

            deleteToolStripMenuItem.Enabled = reducePolygonPointsToolStripMenuItem.Enabled = (map.SelectedItem != null);
            
            selectAllToolStripMenuItem.Enabled = map.MapDoc != null;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Save();
        }

        private async void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (await SaveAsAsync())
                Save();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (DialogAbout dlg = new DialogAbout())
            { 
                dlg.ShowDialog();
            }
        }

        private void insertPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Tool = DesignXmlDraw.ToolMode.InsertPolygon;
            bToolStrip.Image = ((ToolStripMenuItem)sender).Image;
        }

        private void insertTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Tool = DesignXmlDraw.ToolMode.InsertText;
            bToolStrip.Image = ((ToolStripMenuItem)sender).Image;
        }

        private void insertLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Tool = DesignXmlDraw.ToolMode.InsertLine;
            bToolStrip.Image = ((ToolStripMenuItem)sender).Image;
        }

        private void selectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Tool = DesignXmlDraw.ToolMode.Selection;
            bToolStrip.Image = ((ToolStripMenuItem)sender).Image;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!OkToClose())
                return;
            
            map.SetNew();
            pgXmlNode.SelectedObject = null;
            SetTitle(false);
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.SelectAll();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Paste(new Point(0, 0));
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            map.Copy();
            this.map.DeleteSelected();
        }

        private void helpHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/majorsilence/Reporting/discussions");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.MapFile_helpHelpToolStripMenuItem_Click_Help_URL_Invalid);
            }

        }

        private void supportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/majorsilence/Reporting/discussions");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.MapFile_supportToolStripMenuItem_Click_Support_URL_Invalid);
            }

        }

        private async void importMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Strings.MapFile_importMenuItem_Click_OpenShape;
            ofd.FilterIndex = 0;
            ofd.CheckFileExists = true;
            try
            {
                if (await ofd.ShowDialog(this) == DialogResult.OK)
                {
                    map.ClearUndo();
                    ShapeFile sf = new ShapeFile();
                    sf.Read(ofd.FileName);
                    StringBuilder sb = new StringBuilder("<MapItems>", sf.Records.Count * 100);
                    float xOffset = (float)-sf.FileHeader.XMin;
                    float yOffset = (float)-sf.FileHeader.YMin;
                    //PointF offset = this.MercatorConversion(new PointF((float)sf.FileHeader.XMin, (float)sf.FileHeader.YMin));
                    //float xOffset = (float)-offset.X;
                    //float yOffset = (float)-offset.Y;
                    foreach (ShapeFileRecord sfr in sf.Records)
                    {
                        if (sfr.ShapeType == (int)ShapeType.Polygon)
                        {
                            HandlePolygon(sb, xOffset, yOffset, sfr);
                        }
                    }
                    
                    sb.Append("</MapItems>");
                    map.Paste(new Point(0, 0), sb.ToString());
                    map.ClearUndo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, string.Format("{1} {0}", ofd.FileName, Strings.MapFile_openToolStripMenuItem_Click_ErrorOpening));
            }
            finally
            {
                ofd.Dispose();
            }

        }

        private void HandlePolygon(StringBuilder sb, float xOffset, float yOffset, ShapeFileRecord sfr)
        {
            // we'll use this key for all the polygons
            StringBuilder keys = new StringBuilder();
            keys.Append("<Keys>");
            int len = sfr.Attributes.ItemArray.GetLength(0);
            for (int j = 0; j < len; j++)
            {
                string key = sfr.Attributes.ItemArray[j].ToString();
                if (key.Length == 0)
                    continue;
                try { float.Parse(key); continue; }
                catch { } // not a number 
                keys.Append(key);
                if (j + 1 < len)
                    keys.Append(", ");
            }
            if (keys.ToString().EndsWith(", "))
                keys.Remove(keys.Length - 2, 2);
            keys.Append("</Keys>");
            string skeys = keys.ToString();

            for (int i = 0; i < sfr.NumberOfParts; i++)
            {
                sb.Append("<Polygon>");
                sb.Append("<Points>");
                int oldx = int.MaxValue;
                int oldy = int.MaxValue;
                int cp = 0;
                // Determine the starting index and the end index
                // into the points array that defines the figure.
                int start = sfr.Parts[i];
                int end;
                if (sfr.NumberOfParts > 1 && i != (sfr.NumberOfParts - 1))
                    end = sfr.Parts[i + 1];
                else
                    end = sfr.NumberOfPoints;

                // Add line segments to the figure.
                for (int j = start; j < end; j++)
                {
                    PointF ll = sfr.Points[j];
                    //PointF ll = MercatorConversion(sfr.Points[j]);        
                    int x = (int)((ll.X + xOffset) * 4);
                    int y = (int)(((-ll.Y) + yOffset) * 4);
                    if (x == oldx && y == oldy)         // we're truncating the data so some points are redundant
                        continue;
                    cp++;
                    oldx = x;
                    oldy = y;

                    sb.AppendFormat("{0},{1}", x, y);
                    if (j + 1 < sfr.Points.Count)
                        sb.Append(",");
                }
                if (cp == 1)
                    sb.AppendFormat(",{0},{1}", oldx, oldy);

                sb.Append("</Points>");
                sb.Append(skeys);
                sb.Append("</Polygon>");

            }
        }

        private PointF MercatorConversion(PointF ll)
        {
			double dLat = Degrees2Radians(ll.Y);
           
			if (Math.Abs(Math.Abs(dLat) - HALF_PI)  <= .0001f)
			{   // Not perfect but should be close;  latitude fails near poles (90 and -90 degree)
                dLat = Degrees2Radians(ll.Y < 0 ? -87f: 87f);
			}
            // see http://en.wikipedia.org/wiki/Mercator_projection for formula
			double y = Math.Log(Math.Tan(dLat)+ (1f / Math.Cos(dLat)));
			return new PointF(ll.X, (float)Radians2Degrees(y)); 
        }

        static double HALF_PI = Math.PI / 2;

        private double Degrees2Radians(double d)
        {
            return d * Math.PI / 180;
        }

        private double Radians2Degrees(double r)
        {
            return r * 180 / Math.PI;
        }

        private void reducePolygonPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int count = map.ReducePointCount();

			MessageBox.Show(string.Format("{1} {0}.", count, Strings.MapFile_reducePolygonPointsToolStripMenuItem_ReducePolygon), Strings.MapFile_reducePolygonPointsToolStripMenuItem_Click_Reduce_Polygon_Count);
        }

        private void sizePolygonPoints_Click(object sender, EventArgs e)
        {
            ToolStripItem tsi = (ToolStripItem)sender;
            float zoom = (100f + Convert.ToSingle((string)(tsi.Tag))) / 100f;
            map.SizeSelected(zoom);
        }

        private void menuFindByKey_Click(object sender, EventArgs e)
        {
            DialogFindByKey fbk = new DialogFindByKey(map);
            try
            {
                fbk.ShowDialog(this);           // it does all the work
            }
            finally
            {
                fbk.Dispose();
            }
        }



    }
}