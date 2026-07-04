using System;
using Majorsilence.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class ColorPickerPopup : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		ColorPicker _ColorPicker;
private Label lStatus;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            this.lStatus = new Majorsilence.Forms.Label();
            this.SuspendLayout();
            // 
            // lStatus
            // 
            this.lStatus.Dock = Majorsilence.Forms.DockStyle.Bottom;
            this.lStatus.Location = new System.Drawing.Point(0, 174);
            this.lStatus.Name = "lStatus";
            this.lStatus.Size = new System.Drawing.Size(233, 13);
            this.lStatus.TabIndex = 0;
            this.lStatus.Text = "Color";
            // 
            // ColorPickerPopup
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(233, 187);
            this.ControlBox = false;
            this.Controls.Add(this.lStatus);
            this.DoubleBuffered = true;
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ColorPickerPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = Majorsilence.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.Deactivate += this.lbColors_Hide;
            this.Load += this.ColorPickerPopup_Load; 
            this.Shown += this.ColorPickerPopup_Shown;
            this.KeyPress += this.ColorPickerPopup_KeyPress;
            this.MouseMove += this.ColorPickerPopup_MouseMove;
            this.Leave += this.lbColors_Hide;
            this.MouseDown += this.ColorPickerPopup_MouseDown;
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
