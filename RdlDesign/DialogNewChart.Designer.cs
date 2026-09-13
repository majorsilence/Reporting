using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogNewChart : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bCancel;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.ComboBox cbDataSets;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.Label label3;
private System.Windows.Forms.ListBox lbFields;
private System.Windows.Forms.ListBox lbChartCategories;
private System.Windows.Forms.Button bCategoryUp;
private System.Windows.Forms.Button bCategoryDown;
private System.Windows.Forms.Button bCategory;
private System.Windows.Forms.Button bSeries;
private System.Windows.Forms.ListBox lbChartSeries;
private System.Windows.Forms.Button bCategoryDelete;
private System.Windows.Forms.Button bSeriesDelete;
private System.Windows.Forms.Button bSeriesDown;
private System.Windows.Forms.Button bSeriesUp;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.Label lChartData;
private System.Windows.Forms.ComboBox cbChartData;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.ComboBox cbSubType;
private System.Windows.Forms.ComboBox cbChartType;
private System.Windows.Forms.Label label7;
private ComboBox cbChartData2;
private Label lChartData2;
private ComboBox cbChartData3;
private Label lChartData3;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DialogNewChart));
			this.bOK = new System.Windows.Forms.Button();
			this.bCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.cbDataSets = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.lbFields = new System.Windows.Forms.ListBox();
			this.lbChartCategories = new System.Windows.Forms.ListBox();
			this.bCategoryUp = new System.Windows.Forms.Button();
			this.bCategoryDown = new System.Windows.Forms.Button();
			this.bCategory = new System.Windows.Forms.Button();
			this.bSeries = new System.Windows.Forms.Button();
			this.lbChartSeries = new System.Windows.Forms.ListBox();
			this.bCategoryDelete = new System.Windows.Forms.Button();
			this.bSeriesDelete = new System.Windows.Forms.Button();
			this.bSeriesDown = new System.Windows.Forms.Button();
			this.bSeriesUp = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.lChartData = new System.Windows.Forms.Label();
			this.cbChartData = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.cbSubType = new System.Windows.Forms.ComboBox();
			this.cbChartType = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.cbChartData2 = new System.Windows.Forms.ComboBox();
			this.lChartData2 = new System.Windows.Forms.Label();
			this.cbChartData3 = new System.Windows.Forms.ComboBox();
			this.lChartData3 = new System.Windows.Forms.Label();
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
