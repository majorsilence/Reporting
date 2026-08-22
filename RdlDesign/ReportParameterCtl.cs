
using System;
using System.Collections.Generic;
using Majorsilence.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for StyleCtl.
	/// </summary>
	public partial class ReportParameterCtl : UserControl, IProperty
	{
		private DesignXmlDraw _Draw;

        public ReportParameterCtl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
        }

		internal ReportParameterCtl(DesignXmlDraw dxDraw):this()
		{
            SetDraw(dxDraw);	          
		}

        internal void SetDraw(DesignXmlDraw dxDraw)
        {
            _Draw = dxDraw;

            // Initialize form using the style node values
            InitValues();

            rbDefaultDataSetName.Visible = cbDefaultDataSets.Visible = lDefaultValueFields.Visible =
                cbDefaultValueField.Visible = rbDataSet.Visible = cbValidDataSets.Visible = lValidValuesField.Visible =
                cbValidFields.Visible = lDisplayField.Visible = lDisplayField.Visible = cbValidDisplayField.Visible = true;
        }

        #region IProperty Members
        public bool IsValid()
        {
            return true;
        }

        public void Apply()
        {
            if (_Draw == null) 
                return;

            XmlNode rNode = _Draw.GetReportNode();
            _Draw.RemoveElement(rNode, "ReportParameters");	// remove old ReportParameters
            if (this.lbParameters.Items.Count <= 0)
                return;			// nothing in list?  all done

            XmlNode rpsNode = _Draw.SetElement(rNode, "ReportParameters", null);
            foreach (ReportParm repParm in lbParameters.Items)
            {
                if (repParm.Name == null || repParm.Name.Length <= 0)
                    continue;	// shouldn't really happen
                XmlNode repNode = _Draw.CreateElement(rpsNode, "ReportParameter", null);
                // Create the name attribute
                _Draw.SetElementAttribute(repNode, "Name", repParm.Name);

                _Draw.SetElement(repNode, "DataType", repParm.DataType);
                // Handle default values
                ApplyDefaultValues(repNode, repParm);

                _Draw.SetElement(repNode, "Nullable", repParm.AllowNull ? "true" : "false");
                _Draw.SetElement(repNode, "AllowBlank", repParm.AllowBlank ? "true" : "false");
                _Draw.SetElement(repNode, "MultiValue", repParm.MultiValue ? "true" : "false");
                _Draw.SetElement(repNode, "Prompt", repParm.Prompt);

                // Handle ValidValues
                ApplyValidValues(repNode, repParm);
            }
        }
        #endregion IProperty Members

        private void InitValues()
		{
			// Populate datasets combos
			object[] datasets = _Draw.DataSetNames;
			this.cbDefaultDataSets.Items.AddRange(datasets);
			this.cbValidDataSets.Items.AddRange(datasets);

			XmlNode rNode = _Draw.GetReportNode();
			XmlNode rpsNode = _Draw.GetNamedChildNode(rNode, "ReportParameters");
			if (rpsNode == null)
				return;
			foreach (XmlNode repNode in rpsNode)
			{	
				XmlAttribute nAttr = repNode.Attributes["Name"];
				if (nAttr == null)	// shouldn't really happen
					continue;
				ReportParm repParm = new ReportParm(nAttr.Value);
				repParm.DataType = _Draw.GetElementValue(repNode, "DataType", "String");
				// Get default values
				InitDefaultValues(repNode, repParm);

				string nullable  = _Draw.GetElementValue(repNode, "Nullable", "false");
				repParm.AllowNull = (nullable.ToLower() == "true"); // Majorsilence.Reporting.Rdl.XmlUtil can do it
				string allowBlank  = _Draw.GetElementValue(repNode, "AllowBlank", "false");
				repParm.AllowBlank = (allowBlank.ToLower() == "true");
                string mvalue = _Draw.GetElementValue(repNode, "MultiValue", "false");
                repParm.MultiValue = (mvalue.ToLower() == "true");
                repParm.Prompt = _Draw.GetElementValue(repNode, "Prompt", "");

				InitValidValues(repNode, repParm);

				this.lbParameters.Items.Add(repParm);
			}
			if (lbParameters.Items.Count > 0)
				lbParameters.SelectedIndex = 0;
		}

		private void InitDefaultValues(XmlNode reportParameterNode, ReportParm repParm)
		{
			repParm.Default = true;
			XmlNode dfNode = _Draw.GetNamedChildNode(reportParameterNode, "DefaultValue");
			if (dfNode == null)
				return;

			XmlNode vNodes = _Draw.GetNamedChildNode(dfNode, "Values");
			if (vNodes != null)
			{
				List<string> al = new List<string>();
				foreach (XmlNode v in vNodes.ChildNodes)
				{
					if (v.InnerText.Length <= 0)
						continue;
					al.Add(v.InnerText);
				}
				if (al.Count >= 1)
					repParm.DefaultValue  = al;
			}
			XmlNode dsNodes = _Draw.GetNamedChildNode(dfNode, "DataSetReference");
			if (dsNodes != null)
			{
				repParm.Default = false;
				repParm.DefaultDSRDataSetName = _Draw.GetElementValue(dsNodes, "DataSetName", "");
				repParm.DefaultDSRValueField = _Draw.GetElementValue(dsNodes, "ValueField", "");
			}
		}

		private void InitValidValues(XmlNode reportParameterNode, ReportParm repParm)
		{
			repParm.Valid = true;
			XmlNode vvsNode = _Draw.GetNamedChildNode(reportParameterNode, "ValidValues");
			if (vvsNode == null)
				return;

			XmlNode vNodes = _Draw.GetNamedChildNode(vvsNode, "ParameterValues");
			if (vNodes != null)
			{
                List<ParameterValueItem> pvs = new List<ParameterValueItem>();
				foreach (XmlNode v in vNodes.ChildNodes)
				{
					if (v.Name != "ParameterValue")
						continue;
					XmlNode pv = _Draw.GetNamedChildNode(v, "Value");
					if (pv == null)
						continue;
					if (pv == null || pv.InnerText.Length <= 0)
						continue;
					ParameterValueItem pvi = new ParameterValueItem();
					pvi.Value = pv.InnerText;
					pvi.Label = _Draw.GetElementValue(v, "Label", null);
					pvs.Add(pvi);
				}
				if (pvs.Count > 0)
				{
					repParm.ValidValues = pvs;
				}
			}
			XmlNode dsNodes = _Draw.GetNamedChildNode(vvsNode, "DataSetReference");
			if (dsNodes != null)
			{
				repParm.Valid = false;
				repParm.ValidValuesDSRDataSetName = _Draw.GetElementValue(dsNodes, "DataSetName", "");
				repParm.ValidValuesDSRValueField = _Draw.GetElementValue(dsNodes, "ValueField", "");
				repParm.ValidValuesDSRLabelField = _Draw.GetElementValue(dsNodes, "LabelField", "");
			}
		}				

		private void ApplyDefaultValues(XmlNode repNode, ReportParm repParm)
		{
			_Draw.RemoveElement(repNode, "DefaultValue");
			if (repParm.Default)
			{
				if (repParm.DefaultValue == null || repParm.DefaultValue.Count == 0)
					return;

				XmlNode defNode = _Draw.GetCreateNamedChildNode(repNode, "DefaultValue");
				XmlNode vNodes = _Draw.GetCreateNamedChildNode(defNode, "Values");
				foreach (string dv in repParm.DefaultValue)
				{
					_Draw.CreateElement(vNodes, "Value", dv);
				}
			}
			else
			{
				if (repParm.DefaultDSRDataSetName == null || repParm.DefaultDSRDataSetName.Length == 0 ||
					repParm.DefaultDSRValueField == null || repParm.DefaultDSRValueField.Length == 0)
					return;
				XmlNode defNode = _Draw.GetCreateNamedChildNode(repNode, "DefaultValue");
				XmlNode dsrNode = _Draw.GetCreateNamedChildNode(defNode, "DataSetReference");
				_Draw.CreateElement(dsrNode, "DataSetName", repParm.DefaultDSRDataSetName);
				_Draw.CreateElement(dsrNode, "ValueField", repParm.DefaultDSRValueField);
			}
		}

		private void ApplyValidValues(XmlNode repNode, ReportParm repParm)
		{
			_Draw.RemoveElement(repNode, "ValidValues");
			if (repParm.Valid)
			{
				if (repParm.ValidValues == null || repParm.ValidValues.Count == 0)
					return;

				XmlNode vvNode = _Draw.GetCreateNamedChildNode(repNode, "ValidValues");
				XmlNode vNodes = _Draw.GetCreateNamedChildNode(vvNode, "ParameterValues");
				// put out the parameter values
				foreach (ParameterValueItem dvi in repParm.ValidValues)
				{
					XmlNode pvNode = _Draw.CreateElement(vNodes, "ParameterValue", null);
					_Draw.CreateElement(pvNode, "Value", dvi.Value);
					if (dvi.Label != null)
						_Draw.CreateElement(pvNode, "Label", dvi.Label);
				}
			}
			else
			{
				if (repParm.ValidValuesDSRDataSetName == null || repParm.ValidValuesDSRDataSetName.Length == 0 ||
					repParm.ValidValuesDSRValueField == null || repParm.ValidValuesDSRValueField.Length == 0)
					return;
				XmlNode defNode = _Draw.GetCreateNamedChildNode(repNode, "ValidValues");
				XmlNode dsrNode = _Draw.GetCreateNamedChildNode(defNode, "DataSetReference");
				_Draw.CreateElement(dsrNode, "DataSetName", repParm.ValidValuesDSRDataSetName);
				_Draw.CreateElement(dsrNode, "ValueField", repParm.ValidValuesDSRValueField);
				if (repParm.ValidValuesDSRLabelField != null && repParm.ValidValuesDSRLabelField.Length > 0)
					_Draw.CreateElement(dsrNode, "LabelField", repParm.ValidValuesDSRLabelField);
			}
		}

        #region Event Handlers
        private void bAdd_Click(object sender, System.EventArgs e)
		{
			ReportParm rp = new ReportParm("newparm");
			int cur = this.lbParameters.Items.Add(rp);
			lbParameters.SelectedIndex = cur;
			this.tbParmName.Focus();
		}

		private void bRemove_Click(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;
			lbParameters.Items.RemoveAt(cur);
			if (lbParameters.Items.Count <= 0)
				return;
			cur--;
			if (cur < 0)
				cur = 0;
			lbParameters.SelectedIndex = cur;
		}

		private void lbParameters_SelectedIndexChanged(object sender, System.EventArgs e)
		{
            if (tbParmName.Focused)
                return;

            gbPropertyEdit.Enabled = false;
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

            gbPropertyEdit.Enabled = true;

			tbParmName.Text = rp.Name;
			cbParmType.Text = rp.DataType;
			tbParmPrompt.Text = rp.Prompt;
			ckbParmAllowBlank.Checked = rp.AllowBlank;
            ckbParmMultiValue.Checked = rp.MultiValue;
            ckbParmAllowNull.Checked = rp.AllowNull;
			// Handle default values
			if (rp.Default)
			{
				this.rbDefaultValues.Checked = true;
				tbParmDefaultValue.Text = rp.DefaultValueDisplay; 			

				tbParmDefaultValue.Enabled = bDefaultValues.Enabled = true;
				this.cbDefaultDataSets.Enabled = false;
				this.cbDefaultValueField.Enabled = false;
				this.cbDefaultDataSets.SelectedIndex = -1;
				this.cbDefaultValueField.SelectedIndex = -1;
			}
			else
			{
				this.rbDefaultDataSetName.Checked = true;
				this.cbDefaultDataSets.Text = rp.DefaultDSRDataSetName;
				this.cbDefaultValueField.Text = rp.DefaultDSRValueField;

				tbParmDefaultValue.Enabled = bDefaultValues.Enabled =false;
				tbParmDefaultValue.Text = "";
				this.cbDefaultDataSets.Enabled = true;
				this.cbDefaultValueField.Enabled = true;
			}
			// Handle Valid Values
			if (rp.Valid)
			{
				this.rbValues.Checked = true;
				tbParmValidValues.Text = rp.ValidValuesDisplay;

				tbParmValidValues.Enabled = bValidValues.Enabled = true;
				this.cbValidDataSets.Enabled =
					this.cbValidFields.Enabled =
					this.cbValidDisplayField.Enabled = false;
				this.cbValidDataSets.SelectedIndex   =
					this.cbValidFields.SelectedIndex =
					this.cbValidDisplayField.SelectedIndex = -1;
			}
			else
			{
				this.rbDataSet.Checked = true;
				this.cbValidDataSets.Text = rp.ValidValuesDSRDataSetName;
				this.cbValidFields.Text = rp.ValidValuesDSRValueField;
				this.cbValidDisplayField.Text = rp.ValidValuesDSRLabelField == null? "":rp.ValidValuesDSRLabelField;

				this.cbValidDataSets.Enabled =
						this.cbValidFields.Enabled =
						this.cbValidDisplayField.Enabled = true;
				tbParmValidValues.Enabled = bValidValues.Enabled = false;
				tbParmValidValues.Text = "";
			}
		}

		private void lbParameters_MoveItem(int curloc, int newloc)
		{
			ReportParm rp = lbParameters.Items[curloc] as ReportParm;
			if (rp == null)
				return;

			lbParameters.BeginUpdate();
			lbParameters.Items.RemoveAt(curloc);
			lbParameters.Items.Insert(newloc, rp);
			lbParameters.SelectedIndex = newloc;
			lbParameters.EndUpdate();
		}

		private void tbParmName_TextChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			if (rp.Name == tbParmName.Text)
				return;

			rp.Name = tbParmName.Text;
			// text doesn't change in listbox; force change by removing and re-adding item
			lbParameters_MoveItem(cur, cur);
		}

		private void cbParmType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.DataType = cbParmType.Text;
		}

		private void tbParmPrompt_TextChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.Prompt = tbParmPrompt.Text;
		}

		private void ckbParmAllowNull_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.AllowNull = ckbParmAllowNull.Checked;
		}

		private void ckbParmAllowBlank_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.AllowBlank = ckbParmAllowBlank.Checked;
		}

		private void bParmUp_Click(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur <= 0)
				return;
		
			lbParameters_MoveItem(cur, cur-1);
		}

		private void bParmDown_Click(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur+1 >= lbParameters.Items.Count)
				return;
		
			lbParameters_MoveItem(cur, cur+1);
		}

		private void cbDefaultDataSets_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.DefaultDSRDataSetName = cbDefaultDataSets.Text;

			// Populate the fields from the selected dataset
			this.cbDefaultValueField.Items.Clear();
			string[] fields = _Draw.GetFields(cbDefaultDataSets.Text, false);
            if (fields != null)
			    this.cbDefaultValueField.Items.AddRange(fields);
		}

		private void cbValidDataSets_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.ValidValuesDSRDataSetName = cbValidDataSets.Text;

			// Populate the fields from the selected dataset
			this.cbValidFields.Items.Clear();
			string[] fields = _Draw.GetFields(cbValidDataSets.Text, false);
            if (fields != null)
			    this.cbValidFields.Items.AddRange(fields);
			this.cbValidDisplayField.Items.Clear();
			this.cbValidDisplayField.Items.Add("");
            if (fields != null)
			    this.cbValidDisplayField.Items.AddRange(fields);
		}

		private void cbDefaultValueField_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.DefaultDSRValueField = cbDefaultValueField.Text;
		}

		private void cbValidFields_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.ValidValuesDSRValueField = cbValidFields.Text;
		}

		private void cbValidDisplayField_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.ValidValuesDSRLabelField = cbValidDisplayField.Text;
		}

		private void rbDefaultValues_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.Default = rbDefaultValues.Checked;

			tbParmDefaultValue.Enabled = bDefaultValues.Enabled = rbDefaultValues.Checked;
			this.cbDefaultDataSets.Enabled = 
				this.cbDefaultValueField.Enabled = !rbDefaultValues.Checked;
		}

		private void rbDefaultDataSetName_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.Default = !rbDefaultDataSetName.Checked;

			tbParmDefaultValue.Enabled = bDefaultValues.Enabled = !rbDefaultDataSetName.Checked;
			this.cbDefaultDataSets.Enabled = 
				this.cbDefaultValueField.Enabled = rbDefaultDataSetName.Checked;
		}

		private void rbValues_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			this.tbParmValidValues.Enabled = bValidValues.Enabled =  rbValues.Checked;
			rp.Valid = rbValues.Checked;

			this.cbValidDisplayField.Enabled = 
				this.cbValidFields.Enabled =
				this.cbValidDataSets.Enabled = !rbValues.Checked;
		}

		private void rbDataSet_CheckedChanged(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

			rp.Valid = !rbDataSet.Checked;
			this.tbParmValidValues.Enabled = bValidValues.Enabled = !rbDataSet.Checked;
			this.cbValidDisplayField.Enabled = 
				this.cbValidFields.Enabled =
				this.cbValidDataSets.Enabled = rbDataSet.Checked;
		}

		private void bDefaultValues_Click(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

            using (DialogListOfStrings dlos = new DialogListOfStrings(rp.DefaultValue))
            {
                dlos.Text = Strings.ReportParameterCtl_bDefaultValues_Click_Default_Values;
                if (dlos.ShowDialog() != DialogResult.OK)
                    return;
                rp.DefaultValue = dlos.ListOfStrings;
                this.tbParmDefaultValue.Text = rp.DefaultValueDisplay;
            }
		}

		private void bValidValues_Click(object sender, System.EventArgs e)
		{
			int cur = lbParameters.SelectedIndex;
			if (cur < 0)
				return;

			ReportParm rp = lbParameters.Items[cur] as ReportParm;
			if (rp == null)
				return;

            using (DialogValidValues dvv = new DialogValidValues(rp.ValidValues))
            {
                if (dvv.ShowDialog() != DialogResult.OK)
                    return;
                rp.ValidValues = dvv.ValidValues;
                this.tbParmValidValues.Text = rp.ValidValuesDisplay;
            }
		}

        private void ckbParmMultiValue_CheckedChanged(object sender, EventArgs e)
        {
            int cur = lbParameters.SelectedIndex;
            if (cur < 0)
                return;

            ReportParm rp = lbParameters.Items[cur] as ReportParm;
            if (rp == null)
                return;

            rp.MultiValue = ckbParmMultiValue.Checked;

        }
        #endregion Event Handlers
    }
}
