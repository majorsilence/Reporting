namespace Majorsilence.Reporting.RdlDesign
{
    partial class StaticSeriesCtl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(StaticSeriesCtl));
			this.label1 = new Majorsilence.Forms.Label();
			this.lbDataSeries = new Majorsilence.Forms.ListBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.chkShowLabels = new Majorsilence.Forms.CheckBox();
			this.txtSeriesName = new Majorsilence.Forms.TextBox();
			this.txtLabelValue = new Majorsilence.Forms.TextBox();
			this.btnAdd = new Majorsilence.Forms.Button();
			this.btnDel = new Majorsilence.Forms.Button();
			this.btnLabelValue = new Majorsilence.Forms.Button();
			this.btnDataValue = new Majorsilence.Forms.Button();
			this.btnSeriesName = new Majorsilence.Forms.Button();
			this.txtDataValue = new Majorsilence.Forms.TextBox();
			this.label4 = new Majorsilence.Forms.Label();
			this.cbPlotType = new Majorsilence.Forms.ComboBox();
			this.chkLeft = new Majorsilence.Forms.RadioButton();
			this.chkRight = new Majorsilence.Forms.RadioButton();
			this.label5 = new Majorsilence.Forms.Label();
			this.btnUp = new Majorsilence.Forms.Button();
			this.btnDown = new Majorsilence.Forms.Button();
			this.txtX = new Majorsilence.Forms.TextBox();
			this.label6 = new Majorsilence.Forms.Label();
			this.btnX = new Majorsilence.Forms.Button();
			this.chkMarker = new Majorsilence.Forms.CheckBox();
			this.label7 = new Majorsilence.Forms.Label();
			this.cbLine = new Majorsilence.Forms.ComboBox();
			this.label8 = new Majorsilence.Forms.Label();
			this.colorPicker1 = new Majorsilence.Reporting.RdlDesign.ColorPicker();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// lbDataSeries
			// 
			this.lbDataSeries.FormattingEnabled = true;
			resources.ApplyResources(this.lbDataSeries, "lbDataSeries");
			this.lbDataSeries.Name = "lbDataSeries";
			this.lbDataSeries.SelectedIndexChanged += this.lbDataSeries_SelectedIndexChanged;
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
			// chkShowLabels
			// 
			resources.ApplyResources(this.chkShowLabels, "chkShowLabels");
			this.chkShowLabels.Name = "chkShowLabels";
			this.chkShowLabels.UseVisualStyleBackColor = true;
			this.chkShowLabels.CheckedChanged += this.chkShowLabels_CheckedChanged;
			// 
			// txtSeriesName
			// 
			resources.ApplyResources(this.txtSeriesName, "txtSeriesName");
			this.txtSeriesName.Name = "txtSeriesName";
			this.txtSeriesName.TextChanged += this.txtSeriesName_TextChanged;
			// 
			// txtLabelValue
			// 
			resources.ApplyResources(this.txtLabelValue, "txtLabelValue");
			this.txtLabelValue.Name = "txtLabelValue";
			this.txtLabelValue.TextChanged += this.txtLabelValue_TextChanged;
			// 
			// btnAdd
			// 
			resources.ApplyResources(this.btnAdd, "btnAdd");
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += this.btnAdd_Click;
			// 
			// btnDel
			// 
			resources.ApplyResources(this.btnDel, "btnDel");
			this.btnDel.Name = "btnDel";
			this.btnDel.UseVisualStyleBackColor = true;
			this.btnDel.Click += this.btnDel_Click;
			// 
			// btnLabelValue
			// 
			resources.ApplyResources(this.btnLabelValue, "btnLabelValue");
			this.btnLabelValue.Name = "btnLabelValue";
			this.btnLabelValue.UseVisualStyleBackColor = true;
			this.btnLabelValue.Click += this.FunctionButtonClick;
			// 
			// btnDataValue
			// 
			resources.ApplyResources(this.btnDataValue, "btnDataValue");
			this.btnDataValue.Name = "btnDataValue";
			this.btnDataValue.UseVisualStyleBackColor = true;
			this.btnDataValue.Click += this.FunctionButtonClick;
			// 
			// btnSeriesName
			// 
			resources.ApplyResources(this.btnSeriesName, "btnSeriesName");
			this.btnSeriesName.Name = "btnSeriesName";
			this.btnSeriesName.UseVisualStyleBackColor = true;
			this.btnSeriesName.Click += this.FunctionButtonClick;
			// 
			// txtDataValue
			// 
			resources.ApplyResources(this.txtDataValue, "txtDataValue");
			this.txtDataValue.Name = "txtDataValue";
			this.txtDataValue.TextChanged += this.txtDataValue_TextChanged;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// cbPlotType
			// 
			this.cbPlotType.FormattingEnabled = true;
			this.cbPlotType.Items.AddRange(new object[] {
            resources.GetString("cbPlotType.Items"),
            resources.GetString("cbPlotType.Items1")});
			resources.ApplyResources(this.cbPlotType, "cbPlotType");
			this.cbPlotType.Name = "cbPlotType";
			this.cbPlotType.SelectedIndexChanged += this.cbPlotType_SelectedIndexChanged;
			// 
			// chkLeft
			// 
			resources.ApplyResources(this.chkLeft, "chkLeft");
			this.chkLeft.Name = "chkLeft";
			this.chkLeft.TabStop = true;
			this.chkLeft.UseVisualStyleBackColor = true;
			this.chkLeft.CheckedChanged += this.chkLeft_CheckedChanged;
			// 
			// chkRight
			// 
			resources.ApplyResources(this.chkRight, "chkRight");
			this.chkRight.Name = "chkRight";
			this.chkRight.TabStop = true;
			this.chkRight.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// btnUp
			// 
			resources.ApplyResources(this.btnUp, "btnUp");
			this.btnUp.Name = "btnUp";
			this.btnUp.UseVisualStyleBackColor = true;
			this.btnUp.Click += this.btnUp_Click;
			// 
			// btnDown
			// 
			resources.ApplyResources(this.btnDown, "btnDown");
			this.btnDown.Name = "btnDown";
			this.btnDown.UseVisualStyleBackColor = true;
			this.btnDown.Click += this.btnDown_Click;
			// 
			// txtX
			// 
			resources.ApplyResources(this.txtX, "txtX");
			this.txtX.Name = "txtX";
			this.txtX.TextChanged += this.txtX_TextChanged;
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// btnX
			// 
			resources.ApplyResources(this.btnX, "btnX");
			this.btnX.Name = "btnX";
			this.btnX.UseVisualStyleBackColor = true;
			this.btnX.Click += this.FunctionButtonClick;
			// 
			// chkMarker
			// 
			resources.ApplyResources(this.chkMarker, "chkMarker");
			this.chkMarker.Name = "chkMarker";
			this.chkMarker.UseVisualStyleBackColor = true;
			this.chkMarker.CheckedChanged += this.chkMarker_CheckedChanged;
			// 
			// label7
			// 
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			// 
			// cbLine
			// 
			this.cbLine.FormattingEnabled = true;
			this.cbLine.Items.AddRange(new object[] {
            resources.GetString("cbLine.Items"),
            resources.GetString("cbLine.Items1"),
            resources.GetString("cbLine.Items2"),
            resources.GetString("cbLine.Items3"),
            resources.GetString("cbLine.Items4")});
			resources.ApplyResources(this.cbLine, "cbLine");
			this.cbLine.Name = "cbLine";
			this.cbLine.SelectedIndexChanged += this.cbLine_SelectedIndexChanged;
			// 
			// label8
			// 
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			// 
			// colorPicker1
			// 
			this.colorPicker1.DrawMode = Majorsilence.Forms.DrawMode.OwnerDrawFixed;
			this.colorPicker1.DropDownHeight = 1;
			this.colorPicker1.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.colorPicker1, "colorPicker1");
			this.colorPicker1.FormattingEnabled = true;
			this.colorPicker1.Name = "colorPicker1";
			this.colorPicker1.SelectedIndexChanged += this.colorPicker1_SelectedIndexChanged;
			// 
			// StaticSeriesCtl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label8);
			this.Controls.Add(this.colorPicker1);
			this.Controls.Add(this.cbLine);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.chkMarker);
			this.Controls.Add(this.btnX);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.txtX);
			this.Controls.Add(this.btnDown);
			this.Controls.Add(this.btnUp);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.chkRight);
			this.Controls.Add(this.chkLeft);
			this.Controls.Add(this.cbPlotType);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.txtDataValue);
			this.Controls.Add(this.btnSeriesName);
			this.Controls.Add(this.btnDataValue);
			this.Controls.Add(this.btnLabelValue);
			this.Controls.Add(this.btnDel);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.txtLabelValue);
			this.Controls.Add(this.txtSeriesName);
			this.Controls.Add(this.chkShowLabels);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.lbDataSeries);
			this.Controls.Add(this.label1);
			this.Name = "StaticSeriesCtl";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private Majorsilence.Forms.Label label1;
        private Majorsilence.Forms.ListBox lbDataSeries;
        private Majorsilence.Forms.Label label2;
        private Majorsilence.Forms.Label label3;
        private Majorsilence.Forms.CheckBox chkShowLabels;
        private Majorsilence.Forms.TextBox txtSeriesName;
        private Majorsilence.Forms.TextBox txtLabelValue;
        private Majorsilence.Forms.Button btnAdd;
        private Majorsilence.Forms.Button btnDel;
        private Majorsilence.Forms.Button btnLabelValue;
        private Majorsilence.Forms.Button btnDataValue;
        private Majorsilence.Forms.Button btnSeriesName;
        private Majorsilence.Forms.TextBox txtDataValue;
        private Majorsilence.Forms.Label label4;
        private Majorsilence.Forms.ComboBox cbPlotType;
        private Majorsilence.Forms.RadioButton chkLeft;
        private Majorsilence.Forms.RadioButton chkRight;
        private Majorsilence.Forms.Label label5;
        private Majorsilence.Forms.Button btnUp;
        private Majorsilence.Forms.Button btnDown;
        private Majorsilence.Forms.TextBox txtX;
        private Majorsilence.Forms.Label label6;
        private Majorsilence.Forms.Button btnX;
        private Majorsilence.Forms.CheckBox chkMarker;
        private Majorsilence.Forms.Label label7;
        private Majorsilence.Forms.ComboBox cbLine;
        private ColorPicker colorPicker1;
        private Majorsilence.Forms.Label label8;
    }
}
