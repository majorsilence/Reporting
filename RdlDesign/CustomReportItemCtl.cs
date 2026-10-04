
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.Text;
using System.IO;
using System.Reflection;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// CustomReportItemCtl provides property values for a CustomReportItem
	/// </summary>
	internal class CustomReportItemCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
        private DesignXmlDraw _Draw;
        private string _Type;
        private PropertyGrid pgProps;
        private Button bExpr;
        private XmlNode _RiNode;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal CustomReportItemCtl(DesignXmlDraw dxDraw, List<XmlNode> reportItems)
		{
			_Draw = dxDraw;
            this._ReportItems = reportItems;
            _Type = _Draw.GetElementValue(_ReportItems[0], "Type", "");
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
            ICustomReportItem cri=null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(_Type);
                _RiNode = _Draw.GetNamedChildNode(_ReportItems[0], "CustomProperties").Clone();
                object props = cri.GetPropertiesInstance(_RiNode);
                pgProps.SelectedObject = props;
            }
            catch
            {
                return;
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(CustomReportItemCtl));
            this.DoubleBuffered = true;
			this.pgProps = new Majorsilence.Forms.PropertyGrid();
			this.bExpr = new Majorsilence.Forms.Button();
			this.SuspendLayout();
			// 
			// pgProps
			// 
			resources.ApplyResources(this.pgProps, "pgProps");
			this.pgProps.Name = "pgProps";
			// 
			// bExpr
			// 
			resources.ApplyResources(this.bExpr, "bExpr");
			this.bExpr.Name = "bExpr";
			this.bExpr.Tag = "sd";
			this.bExpr.Click += this.bExpr_Click;
			// 
			// CustomReportItemCtl
			// 
			this.Controls.Add(this.bExpr);
			this.Controls.Add(this.pgProps);
			this.Name = "CustomReportItemCtl";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		public bool IsValid()
		{
			return true;
		}

		public void Apply()
		{
            ICustomReportItem cri = null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(_Type);
                foreach (XmlNode node in _ReportItems)
                {
                    cri.SetPropertiesInstance(_Draw.GetNamedChildNode(node, "CustomProperties"), 
                        pgProps.SelectedObject);
                }
            }
            catch
            {
                return;
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }
            return;
		}

        private void bExpr_Click(object sender, EventArgs e)
        {
            GridItem gi = this.pgProps.SelectedGridItem;
            
            XmlNode sNode = _ReportItems[0];
            DialogExprEditor ee = new DialogExprEditor(_Draw, gi.Value.ToString(), sNode, false);
            try
            {
                DialogResult dr = ee.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    // There's probably a better way without reflection but this works fine.
                    
                    object sel = pgProps.SelectedObject;
                    gi.PropertyDescriptor.SetValue(sel, ee.Expression); 
                    gi.Select();
                }
            }
            finally
            {
                ee.Dispose();
            }
        }

	}
}
