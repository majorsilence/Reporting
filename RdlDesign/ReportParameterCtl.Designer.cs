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
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ReportParameterCtl));
            this.DoubleBuffered = true;
			this.lbParameters = new Majorsilence.Forms.ListBox();
			this.bAdd = new Majorsilence.Forms.Button();
			this.bRemove = new Majorsilence.Forms.Button();
			this.bParmDown = new Majorsilence.Forms.Button();
			this.bParmUp = new Majorsilence.Forms.Button();
			this.gbPropertyEdit = new Majorsilence.Forms.GroupBox();
			this.ckbParmMultiValue = new Majorsilence.Forms.CheckBox();
			this.gbValidValues = new Majorsilence.Forms.GroupBox();
			this.cbValidDisplayField = new Majorsilence.Forms.ComboBox();
			this.cbValidFields = new Majorsilence.Forms.ComboBox();
			this.bValidValues = new Majorsilence.Forms.Button();
			this.lDisplayField = new Majorsilence.Forms.Label();
			this.lValidValuesField = new Majorsilence.Forms.Label();
			this.cbValidDataSets = new Majorsilence.Forms.ComboBox();
			this.rbValues = new Majorsilence.Forms.RadioButton();
			this.rbDataSet = new Majorsilence.Forms.RadioButton();
			this.tbParmValidValues = new Majorsilence.Forms.TextBox();
			this.ckbParmAllowBlank = new Majorsilence.Forms.CheckBox();
			this.ckbParmAllowNull = new Majorsilence.Forms.CheckBox();
			this.tbParmPrompt = new Majorsilence.Forms.TextBox();
			this.lParmPrompt = new Majorsilence.Forms.Label();
			this.cbParmType = new Majorsilence.Forms.ComboBox();
			this.lParmType = new Majorsilence.Forms.Label();
			this.tbParmName = new Majorsilence.Forms.TextBox();
			this.lParmName = new Majorsilence.Forms.Label();
			this.gbDefaultValues = new Majorsilence.Forms.GroupBox();
			this.cbDefaultValueField = new Majorsilence.Forms.ComboBox();
			this.tbParmDefaultValue = new Majorsilence.Forms.TextBox();
			this.bDefaultValues = new Majorsilence.Forms.Button();
			this.lDefaultValueFields = new Majorsilence.Forms.Label();
			this.cbDefaultDataSets = new Majorsilence.Forms.ComboBox();
			this.rbDefaultValues = new Majorsilence.Forms.RadioButton();
			this.rbDefaultDataSetName = new Majorsilence.Forms.RadioButton();
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

        internal Majorsilence.Forms.ListBox lbParameters;
        private Majorsilence.Forms.Button bAdd;
        private Majorsilence.Forms.Button bRemove;
        private Majorsilence.Forms.Button bParmDown;
        private Majorsilence.Forms.Button bParmUp;
        private Majorsilence.Forms.GroupBox gbPropertyEdit;
        private Majorsilence.Forms.CheckBox ckbParmMultiValue;
        private Majorsilence.Forms.GroupBox gbValidValues;
        private Majorsilence.Forms.Button bValidValues;
        private Majorsilence.Forms.Label lDisplayField;
        private Majorsilence.Forms.ComboBox cbValidDisplayField;
        private Majorsilence.Forms.Label lValidValuesField;
        private Majorsilence.Forms.ComboBox cbValidFields;
        private Majorsilence.Forms.ComboBox cbValidDataSets;
        private Majorsilence.Forms.RadioButton rbValues;
        private Majorsilence.Forms.RadioButton rbDataSet;
        private Majorsilence.Forms.TextBox tbParmValidValues;
        private Majorsilence.Forms.CheckBox ckbParmAllowBlank;
        private Majorsilence.Forms.CheckBox ckbParmAllowNull;
        private Majorsilence.Forms.TextBox tbParmPrompt;
        private Majorsilence.Forms.Label lParmPrompt;
        private Majorsilence.Forms.ComboBox cbParmType;
        private Majorsilence.Forms.Label lParmType;
        private Majorsilence.Forms.TextBox tbParmName;
        private Majorsilence.Forms.Label lParmName;
        private Majorsilence.Forms.GroupBox gbDefaultValues;
        private Majorsilence.Forms.TextBox tbParmDefaultValue;
        private Majorsilence.Forms.Button bDefaultValues;
        private Majorsilence.Forms.Label lDefaultValueFields;
        private Majorsilence.Forms.ComboBox cbDefaultValueField;
        private Majorsilence.Forms.ComboBox cbDefaultDataSets;
        private Majorsilence.Forms.RadioButton rbDefaultValues;
        private Majorsilence.Forms.RadioButton rbDefaultDataSetName;

    }
}