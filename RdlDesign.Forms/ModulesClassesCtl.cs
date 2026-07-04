
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;
using System.IO;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for ModulesClassesCtl.
	/// </summary>
	internal class ModulesClassesCtl : Majorsilence.Forms.UserControl, IProperty
	{
		private DesignXmlDraw _Draw;
		private DataTable _DTCM;
		private DataTable _DTCL;

		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Button bDeleteCM;
		private Majorsilence.Forms.DataGridView dgCodeModules;
		private Majorsilence.Forms.Button bDeleteClass;
		private Majorsilence.Forms.DataGridView dgClasses;
		private Majorsilence.Forms.Label label2;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		internal ModulesClassesCtl(DesignXmlDraw dxDraw)
		{
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			BuildCodeModules();
			BuildClasses();
		}

		private void BuildCodeModules()
		{
			XmlNode rNode = _Draw.GetReportNode();

			// Initialize the DataTable
			_DTCM = new DataTable();
			_DTCM.Columns.Add(new DataColumn("Code Module", typeof(string)));

			string[] rowValues = new string[1];
			XmlNode cmsNode = _Draw.GetNamedChildNode(rNode, "CodeModules");

			if (cmsNode != null)
				foreach (XmlNode cmNode in cmsNode.ChildNodes)
				{
					if (cmNode.NodeType != XmlNodeType.Element || 
						cmNode.Name != "CodeModule")
						continue;
					rowValues[0] = cmNode.InnerText;

					_DTCM.Rows.Add(rowValues);
				}
			this.dgCodeModules.DataSource = _DTCM;
		}

		private void BuildClasses()
		{
			XmlNode rNode = _Draw.GetReportNode();

			// Initialize the DataTable
			_DTCL = new DataTable();
			_DTCL.Columns.Add(new DataColumn("Class Name", typeof(string)));
			_DTCL.Columns.Add(new DataColumn("Instance Name", typeof(string)));

			string[] rowValues = new string[2];
			XmlNode clsNode = _Draw.GetNamedChildNode(rNode, "Classes");

			if (clsNode != null)
				foreach (XmlNode clNode in clsNode.ChildNodes)
				{
					if (clNode.NodeType != XmlNodeType.Element || 
						clNode.Name != "Class")
						continue;

					XmlNode node = _Draw.GetNamedChildNode(clNode, "ClassName");
					if (node != null)
						rowValues[0] = node.InnerText;

					node = _Draw.GetNamedChildNode(clNode, "InstanceName");
					if (node != null)
						rowValues[1] = node.InnerText;

					_DTCL.Rows.Add(rowValues);
				}
			this.dgClasses.DataSource = _DTCL;
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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ModulesClassesCtl));
            this.DoubleBuffered = true;
			this.label1 = new Majorsilence.Forms.Label();
			this.bDeleteCM = new Majorsilence.Forms.Button();
			this.dgCodeModules = new Majorsilence.Forms.DataGridView();
			this.bDeleteClass = new Majorsilence.Forms.Button();
			this.dgClasses = new Majorsilence.Forms.DataGridView();
			this.label2 = new Majorsilence.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.dgCodeModules)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.dgClasses)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// bDeleteCM
			// 
			resources.ApplyResources(this.bDeleteCM, "bDeleteCM");
			this.bDeleteCM.Name = "bDeleteCM";
			this.bDeleteCM.Click += this.bDeleteCM_Click;
			// 
			// dgCodeModules
			// 
			resources.ApplyResources(this.dgCodeModules, "dgCodeModules");
			this.dgCodeModules.DataMember = "";
			this.dgCodeModules.Name = "dgCodeModules";
			// 
			// bDeleteClass
			// 
			resources.ApplyResources(this.bDeleteClass, "bDeleteClass");
			this.bDeleteClass.Name = "bDeleteClass";
			this.bDeleteClass.Click += this.bDeleteClass_Click;
			// 
			// dgClasses
			// 
			resources.ApplyResources(this.dgClasses, "dgClasses");
			this.dgClasses.DataMember = "";
			this.dgClasses.Name = "dgClasses";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// ModulesClassesCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bDeleteClass);
			this.Controls.Add(this.dgClasses);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.bDeleteCM);
			this.Controls.Add(this.dgCodeModules);
			this.Controls.Add(this.label1);
			this.Name = "ModulesClassesCtl";
			((System.ComponentModel.ISupportInitialize)(this.dgCodeModules)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.dgClasses)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		public bool IsValid()
		{
			return true;
		}

		public void Apply()
		{
			ApplyCodeModules();
			ApplyClasses();
		}
		
		private void ApplyCodeModules()
		{
			XmlNode rNode = _Draw.GetReportNode(); 
			_Draw.RemoveElement(rNode, "CodeModules");
			if (!HasRows(this._DTCM, 1))
				return;				

			// Set the CodeModules
			XmlNode cms = _Draw.CreateElement(rNode, "CodeModules", null);
			foreach (DataRow dr in _DTCM.Rows)
			{
				if (dr[0] == DBNull.Value ||
					dr[0].ToString().Trim().Length <= 0)
					continue;

				_Draw.CreateElement(cms, "CodeModule", dr[0].ToString());
			}
		}

		private void ApplyClasses()
		{
			XmlNode rNode = _Draw.GetReportNode(); 
			_Draw.RemoveElement(rNode, "Classes");
			if (!HasRows(this._DTCL, 2))
				return;				

			// Set the classes
			XmlNode cs = _Draw.CreateElement(rNode, "Classes", null);
			foreach (DataRow dr in _DTCL.Rows)
			{
				if (dr[0] == DBNull.Value ||
					dr[1] == DBNull.Value ||
					dr[0].ToString().Trim().Length <= 0 ||
					dr[1].ToString().Trim().Length <= 0)
					continue;

				XmlNode c = _Draw.CreateElement(cs, "Class", null);
				_Draw.CreateElement(c, "ClassName", dr[0].ToString());
				_Draw.CreateElement(c, "InstanceName", dr[1].ToString());
			}

		}

		private bool HasRows(DataTable dt, int columns)
		{
			foreach (DataRow dr in dt.Rows)
			{
				bool bCompleteRow = true;
				for (int i=0; i < columns; i++)
				{
					if (dr[i] == DBNull.Value)
					{
						bCompleteRow = false;
						break;
					}
					string ge = (string) dr[i];
					if (ge.Length <= 0)
					{
						bCompleteRow = false;
						break;
					}
				}
				if (bCompleteRow)
					return true;
			}
			return false;
		}

		private void bDeleteCM_Click(object sender, System.EventArgs e)
		{
			int cr = this.dgCodeModules.CurrentRow.Index;
			if (cr < 0)		// already at the top
				return;

			_DTCM.Rows.RemoveAt(cr);

		}

		private void bDeleteClass_Click(object sender, System.EventArgs e)
		{
			int cr = this.dgClasses.CurrentRow.Index;
			if (cr < 0)		// already at the top
				return;

			_DTCL.Rows.RemoveAt(cr);
		}
	}
}
