using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogNewChart : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.ComboBox cbDataSets;
private Majorsilence.Forms.Label label2;
private Majorsilence.Forms.Label label3;
private Majorsilence.Forms.ListBox lbFields;
private Majorsilence.Forms.ListBox lbChartCategories;
private Majorsilence.Forms.Button bCategoryUp;
private Majorsilence.Forms.Button bCategoryDown;
private Majorsilence.Forms.Button bCategory;
private Majorsilence.Forms.Button bSeries;
private Majorsilence.Forms.ListBox lbChartSeries;
private Majorsilence.Forms.Button bCategoryDelete;
private Majorsilence.Forms.Button bSeriesDelete;
private Majorsilence.Forms.Button bSeriesDown;
private Majorsilence.Forms.Button bSeriesUp;
private Majorsilence.Forms.Label label4;
private Majorsilence.Forms.Label lChartData;
private Majorsilence.Forms.ComboBox cbChartData;
private Majorsilence.Forms.Label label6;
private Majorsilence.Forms.ComboBox cbSubType;
private Majorsilence.Forms.ComboBox cbChartType;
private Majorsilence.Forms.Label label7;
private ComboBox cbChartData2;
private Label lChartData2;
private ComboBox cbChartData3;
private Label lChartData3;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogNewChart));
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.cbDataSets = new Majorsilence.Forms.ComboBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.lbFields = new Majorsilence.Forms.ListBox();
			this.lbChartCategories = new Majorsilence.Forms.ListBox();
			this.bCategoryUp = new Majorsilence.Forms.Button();
			this.bCategoryDown = new Majorsilence.Forms.Button();
			this.bCategory = new Majorsilence.Forms.Button();
			this.bSeries = new Majorsilence.Forms.Button();
			this.lbChartSeries = new Majorsilence.Forms.ListBox();
			this.bCategoryDelete = new Majorsilence.Forms.Button();
			this.bSeriesDelete = new Majorsilence.Forms.Button();
			this.bSeriesDown = new Majorsilence.Forms.Button();
			this.bSeriesUp = new Majorsilence.Forms.Button();
			this.label4 = new Majorsilence.Forms.Label();
			this.lChartData = new Majorsilence.Forms.Label();
			this.cbChartData = new Majorsilence.Forms.ComboBox();
			this.label6 = new Majorsilence.Forms.Label();
			this.cbSubType = new Majorsilence.Forms.ComboBox();
			this.cbChartType = new Majorsilence.Forms.ComboBox();
			this.label7 = new Majorsilence.Forms.Label();
			this.cbChartData2 = new Majorsilence.Forms.ComboBox();
			this.lChartData2 = new Majorsilence.Forms.Label();
			this.cbChartData3 = new Majorsilence.Forms.ComboBox();
			this.lChartData3 = new Majorsilence.Forms.Label();
			this.SuspendLayout();
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.Name = "bOK";
			this.bOK.Click += this.bOK_Click;
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// cbDataSets
			// 
			resources.ApplyResources(this.cbDataSets, "cbDataSets");
			this.cbDataSets.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDataSets.Name = "cbDataSets";
			this.cbDataSets.SelectedIndexChanged += this.cbDataSets_SelectedIndexChanged;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// lbFields
			// 
			resources.ApplyResources(this.lbFields, "lbFields");
			this.lbFields.Name = "lbFields";
			this.lbFields.SelectionMode = Majorsilence.Forms.SelectionMode.MultiExtended;
			// 
			// lbChartCategories
			// 
			resources.ApplyResources(this.lbChartCategories, "lbChartCategories");
			this.lbChartCategories.Name = "lbChartCategories";
			// 
			// bCategoryUp
			// 
			resources.ApplyResources(this.bCategoryUp, "bCategoryUp");
			this.bCategoryUp.Name = "bCategoryUp";
			this.bCategoryUp.Click += this.bCategoryUp_Click;
			// 
			// bCategoryDown
			// 
			resources.ApplyResources(this.bCategoryDown, "bCategoryDown");
			this.bCategoryDown.Name = "bCategoryDown";
			this.bCategoryDown.Click += this.bCategoryDown_Click;
			// 
			// bCategory
			// 
			resources.ApplyResources(this.bCategory, "bCategory");
			this.bCategory.Name = "bCategory";
			this.bCategory.Click += this.bCategory_Click;
			// 
			// bSeries
			// 
			resources.ApplyResources(this.bSeries, "bSeries");
			this.bSeries.Name = "bSeries";
			this.bSeries.Click += this.bSeries_Click;
			// 
			// lbChartSeries
			// 
			resources.ApplyResources(this.lbChartSeries, "lbChartSeries");
			this.lbChartSeries.Name = "lbChartSeries";
			// 
			// bCategoryDelete
			// 
			resources.ApplyResources(this.bCategoryDelete, "bCategoryDelete");
			this.bCategoryDelete.Name = "bCategoryDelete";
			this.bCategoryDelete.Click += this.bCategoryDelete_Click;
			// 
			// bSeriesDelete
			// 
			resources.ApplyResources(this.bSeriesDelete, "bSeriesDelete");
			this.bSeriesDelete.Name = "bSeriesDelete";
			this.bSeriesDelete.Click += this.bSeriesDelete_Click;
			// 
			// bSeriesDown
			// 
			resources.ApplyResources(this.bSeriesDown, "bSeriesDown");
			this.bSeriesDown.Name = "bSeriesDown";
			this.bSeriesDown.Click += this.bSeriesDown_Click;
			// 
			// bSeriesUp
			// 
			resources.ApplyResources(this.bSeriesUp, "bSeriesUp");
			this.bSeriesUp.Name = "bSeriesUp";
			this.bSeriesUp.Click += this.bSeriesUp_Click;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// lChartData
			// 
			resources.ApplyResources(this.lChartData, "lChartData");
			this.lChartData.Name = "lChartData";
			// 
			// cbChartData
			// 
			resources.ApplyResources(this.cbChartData, "cbChartData");
			this.cbChartData.Name = "cbChartData";
			this.cbChartData.TextChanged += this.cbChartData_TextChanged;
			this.cbChartData.Enter += this.cbChartData_Enter;
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// cbSubType
			// 
			resources.ApplyResources(this.cbSubType, "cbSubType");
			this.cbSubType.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbSubType.Name = "cbSubType";
			// 
			// cbChartType
			// 
			resources.ApplyResources(this.cbChartType, "cbChartType");
			this.cbChartType.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbChartType.Items.AddRange(new object[] {
            resources.GetString("cbChartType.Items"),
            resources.GetString("cbChartType.Items1"),
            resources.GetString("cbChartType.Items2"),
            resources.GetString("cbChartType.Items3"),
            resources.GetString("cbChartType.Items4"),
            resources.GetString("cbChartType.Items5"),
            resources.GetString("cbChartType.Items6"),
            resources.GetString("cbChartType.Items7")});
			this.cbChartType.Name = "cbChartType";
			this.cbChartType.SelectedIndexChanged += this.cbChartType_SelectedIndexChanged;
			// 
			// label7
			// 
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			// 
			// cbChartData2
			// 
			resources.ApplyResources(this.cbChartData2, "cbChartData2");
			this.cbChartData2.Name = "cbChartData2";
			this.cbChartData2.TextChanged += this.cbChartData_TextChanged;
			this.cbChartData2.Enter += this.cbChartData_Enter;
			// 
			// lChartData2
			// 
			resources.ApplyResources(this.lChartData2, "lChartData2");
			this.lChartData2.Name = "lChartData2";
			// 
			// cbChartData3
			// 
			resources.ApplyResources(this.cbChartData3, "cbChartData3");
			this.cbChartData3.Name = "cbChartData3";
			this.cbChartData3.TextChanged += this.cbChartData_TextChanged;
			this.cbChartData3.Enter += this.cbChartData_Enter;
			// 
			// lChartData3
			// 
			resources.ApplyResources(this.lChartData3, "lChartData3");
			this.lChartData3.Name = "lChartData3";
			// 
			// DialogNewChart
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.cbChartData3);
			this.Controls.Add(this.lChartData3);
			this.Controls.Add(this.cbChartData2);
			this.Controls.Add(this.lChartData2);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.cbSubType);
			this.Controls.Add(this.cbChartType);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.cbChartData);
			this.Controls.Add(this.lChartData);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.bSeriesDelete);
			this.Controls.Add(this.bSeriesDown);
			this.Controls.Add(this.bSeriesUp);
			this.Controls.Add(this.bCategoryDelete);
			this.Controls.Add(this.lbChartSeries);
			this.Controls.Add(this.bSeries);
			this.Controls.Add(this.bCategory);
			this.Controls.Add(this.bCategoryDown);
			this.Controls.Add(this.bCategoryUp);
			this.Controls.Add(this.lbChartCategories);
			this.Controls.Add(this.lbFields);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.cbDataSets);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogNewChart";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);

		}
		#endregion

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
	}
}
