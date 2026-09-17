namespace Majorsilence.Reporting.RdlDesign
{
    partial class ReportParameterCtl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
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
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(ReportParameterCtl));
            this.DoubleBuffered = true;
			this.lbParameters = new System.Windows.Forms.ListBox();
			this.bAdd = new System.Windows.Forms.Button();
			this.bRemove = new System.Windows.Forms.Button();
			this.bParmDown = new System.Windows.Forms.Button();
			this.bParmUp = new System.Windows.Forms.Button();
			this.gbPropertyEdit = new System.Windows.Forms.GroupBox();
			this.ckbParmMultiValue = new System.Windows.Forms.CheckBox();
			this.gbValidValues = new System.Windows.Forms.GroupBox();
			this.cbValidDisplayField = new System.Windows.Forms.ComboBox();
			this.cbValidFields = new System.Windows.Forms.ComboBox();
			this.bValidValues = new System.Windows.Forms.Button();
			this.lDisplayField = new System.Windows.Forms.Label();
			this.lValidValuesField = new System.Windows.Forms.Label();
			this.cbValidDataSets = new System.Windows.Forms.ComboBox();
			this.rbValues = new System.Windows.Forms.RadioButton();
			this.rbDataSet = new System.Windows.Forms.RadioButton();
			this.tbParmValidValues = new System.Windows.Forms.TextBox();
			this.ckbParmAllowBlank = new System.Windows.Forms.CheckBox();
			this.ckbParmAllowNull = new System.Windows.Forms.CheckBox();
			this.tbParmPrompt = new System.Windows.Forms.TextBox();
			this.lParmPrompt = new System.Windows.Forms.Label();
			this.cbParmType = new System.Windows.Forms.ComboBox();
			this.lParmType = new System.Windows.Forms.Label();
			this.tbParmName = new System.Windows.Forms.TextBox();
			this.lParmName = new System.Windows.Forms.Label();
			this.gbDefaultValues = new System.Windows.Forms.GroupBox();
			this.cbDefaultValueField = new System.Windows.Forms.ComboBox();
			this.tbParmDefaultValue = new System.Windows.Forms.TextBox();
			this.bDefaultValues = new System.Windows.Forms.Button();
			this.lDefaultValueFields = new System.Windows.Forms.Label();
			this.cbDefaultDataSets = new System.Windows.Forms.ComboBox();
			this.rbDefaultValues = new System.Windows.Forms.RadioButton();
			this.rbDefaultDataSetName = new System.Windows.Forms.RadioButton();
			this.gbPropertyEdit.SuspendLayout();
			this.gbValidValues.SuspendLayout();
			this.gbDefaultValues.SuspendLayout();
			this.SuspendLayout();
			// 
			// lbParameters
			// 
			resources.ApplyResources(this.lbParameters, "lbParameters");
			this.lbParameters.Name = "lbParameters";
			this.lbParameters.SelectedIndexChanged += this.lbParameters_SelectedIndexChanged;
			// 
			// bAdd
			// 
			resources.ApplyResources(this.bAdd, "bAdd");
			this.bAdd.Name = "bAdd";
			this.bAdd.Click += this.bAdd_Click;
			// 
			// bRemove
			// 
			resources.ApplyResources(this.bRemove, "bRemove");
			this.bRemove.Name = "bRemove";
			this.bRemove.Click += this.bRemove_Click;
			// 
			// bParmDown
			// 
			resources.ApplyResources(this.bParmDown, "bParmDown");
			this.bParmDown.Name = "bParmDown";
			this.bParmDown.Click += this.bParmDown_Click;
			// 
			// bParmUp
			// 
			resources.ApplyResources(this.bParmUp, "bParmUp");
			this.bParmUp.Name = "bParmUp";
			this.bParmUp.Click += this.bParmUp_Click;
			// 
			// gbPropertyEdit
			// 
			resources.ApplyResources(this.gbPropertyEdit, "gbPropertyEdit");
			this.gbPropertyEdit.Controls.Add(this.ckbParmMultiValue);
			this.gbPropertyEdit.Controls.Add(this.gbValidValues);
			this.gbPropertyEdit.Controls.Add(this.ckbParmAllowBlank);
			this.gbPropertyEdit.Controls.Add(this.ckbParmAllowNull);
			this.gbPropertyEdit.Controls.Add(this.tbParmPrompt);
			this.gbPropertyEdit.Controls.Add(this.lParmPrompt);
			this.gbPropertyEdit.Controls.Add(this.cbParmType);
			this.gbPropertyEdit.Controls.Add(this.lParmType);
			this.gbPropertyEdit.Controls.Add(this.tbParmName);
			this.gbPropertyEdit.Controls.Add(this.lParmName);
			this.gbPropertyEdit.Controls.Add(this.gbDefaultValues);
			this.gbPropertyEdit.Name = "gbPropertyEdit";
			this.gbPropertyEdit.TabStop = false;
			// 
			// ckbParmMultiValue
			// 
			resources.ApplyResources(this.ckbParmMultiValue, "ckbParmMultiValue");
			this.ckbParmMultiValue.Name = "ckbParmMultiValue";
			this.ckbParmMultiValue.CheckedChanged += this.ckbParmMultiValue_CheckedChanged;
			// 
			// gbValidValues
			// 
			resources.ApplyResources(this.gbValidValues, "gbValidValues");
			this.gbValidValues.Controls.Add(this.cbValidDisplayField);
			this.gbValidValues.Controls.Add(this.cbValidFields);
			this.gbValidValues.Controls.Add(this.bValidValues);
			this.gbValidValues.Controls.Add(this.lDisplayField);
			this.gbValidValues.Controls.Add(this.lValidValuesField);
			this.gbValidValues.Controls.Add(this.cbValidDataSets);
			this.gbValidValues.Controls.Add(this.rbValues);
			this.gbValidValues.Controls.Add(this.rbDataSet);
			this.gbValidValues.Controls.Add(this.tbParmValidValues);
			this.gbValidValues.Name = "gbValidValues";
			this.gbValidValues.TabStop = false;
			// 
			// cbValidDisplayField
			// 
			resources.ApplyResources(this.cbValidDisplayField, "cbValidDisplayField");
			this.cbValidDisplayField.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbValidDisplayField.Name = "cbValidDisplayField";
			this.cbValidDisplayField.SelectedIndexChanged += this.cbValidDisplayField_SelectedIndexChanged;
			// 
			// cbValidFields
			// 
			resources.ApplyResources(this.cbValidFields, "cbValidFields");
			this.cbValidFields.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbValidFields.Name = "cbValidFields";
			this.cbValidFields.SelectedIndexChanged += this.cbValidFields_SelectedIndexChanged;
			// 
			// bValidValues
			// 
			resources.ApplyResources(this.bValidValues, "bValidValues");
			this.bValidValues.Name = "bValidValues";
			this.bValidValues.Click += this.bValidValues_Click;
			// 
			// lDisplayField
			// 
			resources.ApplyResources(this.lDisplayField, "lDisplayField");
			this.lDisplayField.Name = "lDisplayField";
			// 
			// lValidValuesField
			// 
			resources.ApplyResources(this.lValidValuesField, "lValidValuesField");
			this.lValidValuesField.Name = "lValidValuesField";
			// 
			// cbValidDataSets
			// 
			resources.ApplyResources(this.cbValidDataSets, "cbValidDataSets");
			this.cbValidDataSets.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbValidDataSets.Name = "cbValidDataSets";
			this.cbValidDataSets.SelectedIndexChanged += this.cbValidDataSets_SelectedIndexChanged;
			// 
			// rbValues
			// 
			resources.ApplyResources(this.rbValues, "rbValues");
			this.rbValues.Name = "rbValues";
			this.rbValues.CheckedChanged += this.rbValues_CheckedChanged;
			// 
			// rbDataSet
			// 
			resources.ApplyResources(this.rbDataSet, "rbDataSet");
			this.rbDataSet.Name = "rbDataSet";
			this.rbDataSet.CheckedChanged += this.rbDataSet_CheckedChanged;
			// 
			// tbParmValidValues
			// 
			resources.ApplyResources(this.tbParmValidValues, "tbParmValidValues");
			this.tbParmValidValues.Name = "tbParmValidValues";
			this.tbParmValidValues.ReadOnly = true;
			// 
			// ckbParmAllowBlank
			// 
			resources.ApplyResources(this.ckbParmAllowBlank, "ckbParmAllowBlank");
			this.ckbParmAllowBlank.Name = "ckbParmAllowBlank";
			this.ckbParmAllowBlank.CheckedChanged += this.ckbParmAllowBlank_CheckedChanged;
			// 
			// ckbParmAllowNull
			// 
			resources.ApplyResources(this.ckbParmAllowNull, "ckbParmAllowNull");
			this.ckbParmAllowNull.Name = "ckbParmAllowNull";
			this.ckbParmAllowNull.CheckedChanged += this.ckbParmAllowNull_CheckedChanged;
			// 
			// tbParmPrompt
			// 
			resources.ApplyResources(this.tbParmPrompt, "tbParmPrompt");
			this.tbParmPrompt.Name = "tbParmPrompt";
			this.tbParmPrompt.TextChanged += this.tbParmPrompt_TextChanged;
			// 
			// lParmPrompt
			// 
			resources.ApplyResources(this.lParmPrompt, "lParmPrompt");
			this.lParmPrompt.Name = "lParmPrompt";
			// 
			// cbParmType
			// 
			resources.ApplyResources(this.cbParmType, "cbParmType");
			this.cbParmType.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbParmType.Items.AddRange(new object[] {
            resources.GetString("cbParmType.Items"),
            resources.GetString("cbParmType.Items1"),
            resources.GetString("cbParmType.Items2"),
            resources.GetString("cbParmType.Items3"),
            resources.GetString("cbParmType.Items4")});
			this.cbParmType.Name = "cbParmType";
			this.cbParmType.SelectedIndexChanged += this.cbParmType_SelectedIndexChanged;
			// 
			// lParmType
			// 
			resources.ApplyResources(this.lParmType, "lParmType");
			this.lParmType.Name = "lParmType";
			// 
			// tbParmName
			// 
			resources.ApplyResources(this.tbParmName, "tbParmName");
			this.tbParmName.Name = "tbParmName";
			this.tbParmName.TextChanged += this.tbParmName_TextChanged;
			// 
			// lParmName
			// 
			resources.ApplyResources(this.lParmName, "lParmName");
			this.lParmName.Name = "lParmName";
			// 
			// gbDefaultValues
			// 
			resources.ApplyResources(this.gbDefaultValues, "gbDefaultValues");
			this.gbDefaultValues.Controls.Add(this.cbDefaultValueField);
			this.gbDefaultValues.Controls.Add(this.tbParmDefaultValue);
			this.gbDefaultValues.Controls.Add(this.bDefaultValues);
			this.gbDefaultValues.Controls.Add(this.lDefaultValueFields);
			this.gbDefaultValues.Controls.Add(this.cbDefaultDataSets);
			this.gbDefaultValues.Controls.Add(this.rbDefaultValues);
			this.gbDefaultValues.Controls.Add(this.rbDefaultDataSetName);
			this.gbDefaultValues.Name = "gbDefaultValues";
			this.gbDefaultValues.TabStop = false;
			// 
			// cbDefaultValueField
			// 
			resources.ApplyResources(this.cbDefaultValueField, "cbDefaultValueField");
			this.cbDefaultValueField.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDefaultValueField.Name = "cbDefaultValueField";
			this.cbDefaultValueField.SelectedIndexChanged += this.cbDefaultValueField_SelectedIndexChanged;
			// 
			// tbParmDefaultValue
			// 
			resources.ApplyResources(this.tbParmDefaultValue, "tbParmDefaultValue");
			this.tbParmDefaultValue.Name = "tbParmDefaultValue";
			this.tbParmDefaultValue.ReadOnly = true;
			// 
			// bDefaultValues
			// 
			resources.ApplyResources(this.bDefaultValues, "bDefaultValues");
			this.bDefaultValues.Name = "bDefaultValues";
			this.bDefaultValues.Click += this.bDefaultValues_Click;
			// 
			// lDefaultValueFields
			// 
			resources.ApplyResources(this.lDefaultValueFields, "lDefaultValueFields");
			this.lDefaultValueFields.Name = "lDefaultValueFields";
			// 
			// cbDefaultDataSets
			// 
			resources.ApplyResources(this.cbDefaultDataSets, "cbDefaultDataSets");
			this.cbDefaultDataSets.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDefaultDataSets.Name = "cbDefaultDataSets";
			this.cbDefaultDataSets.SelectedIndexChanged += this.cbDefaultDataSets_SelectedIndexChanged;
			// 
			// rbDefaultValues
			// 
			resources.ApplyResources(this.rbDefaultValues, "rbDefaultValues");
			this.rbDefaultValues.Name = "rbDefaultValues";
			this.rbDefaultValues.CheckedChanged += this.rbDefaultValues_CheckedChanged;
			// 
			// rbDefaultDataSetName
			// 
			resources.ApplyResources(this.rbDefaultDataSetName, "rbDefaultDataSetName");
			this.rbDefaultDataSetName.Name = "rbDefaultDataSetName";
			this.rbDefaultDataSetName.CheckedChanged += this.rbDefaultDataSetName_CheckedChanged;
			// 
			// ReportParameterCtl
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.gbPropertyEdit);
			this.Controls.Add(this.bParmDown);
			this.Controls.Add(this.bParmUp);
			this.Controls.Add(this.lbParameters);
			this.Controls.Add(this.bAdd);
			this.Controls.Add(this.bRemove);
			this.Name = "ReportParameterCtl";
			this.gbPropertyEdit.ResumeLayout(false);
			this.gbPropertyEdit.PerformLayout();
			this.gbValidValues.ResumeLayout(false);
			this.gbValidValues.PerformLayout();
			this.gbDefaultValues.ResumeLayout(false);
			this.gbDefaultValues.PerformLayout();
			this.ResumeLayout(false);

        }
        #endregion

        internal System.Windows.Forms.ListBox lbParameters;
        private System.Windows.Forms.Button bAdd;
        private System.Windows.Forms.Button bRemove;
        private System.Windows.Forms.Button bParmDown;
        private System.Windows.Forms.Button bParmUp;
        private System.Windows.Forms.GroupBox gbPropertyEdit;
        private System.Windows.Forms.CheckBox ckbParmMultiValue;
        private System.Windows.Forms.GroupBox gbValidValues;
        private System.Windows.Forms.Button bValidValues;
        private System.Windows.Forms.Label lDisplayField;
        private System.Windows.Forms.ComboBox cbValidDisplayField;
        private System.Windows.Forms.Label lValidValuesField;
        private System.Windows.Forms.ComboBox cbValidFields;
        private System.Windows.Forms.ComboBox cbValidDataSets;
        private System.Windows.Forms.RadioButton rbValues;
        private System.Windows.Forms.RadioButton rbDataSet;
        private System.Windows.Forms.TextBox tbParmValidValues;
        private System.Windows.Forms.CheckBox ckbParmAllowBlank;
        private System.Windows.Forms.CheckBox ckbParmAllowNull;
        private System.Windows.Forms.TextBox tbParmPrompt;
        private System.Windows.Forms.Label lParmPrompt;
        private System.Windows.Forms.ComboBox cbParmType;
        private System.Windows.Forms.Label lParmType;
        private System.Windows.Forms.TextBox tbParmName;
        private System.Windows.Forms.Label lParmName;
        private System.Windows.Forms.GroupBox gbDefaultValues;
        private System.Windows.Forms.TextBox tbParmDefaultValue;
        private System.Windows.Forms.Button bDefaultValues;
        private System.Windows.Forms.Label lDefaultValueFields;
        private System.Windows.Forms.ComboBox cbDefaultValueField;
        private System.Windows.Forms.ComboBox cbDefaultDataSets;
        private System.Windows.Forms.RadioButton rbDefaultValues;
        private System.Windows.Forms.RadioButton rbDefaultDataSetName;

    }
}