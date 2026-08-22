using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlViewer
{
	public partial class RdlViewer
	{
		#region Windows Form Designer generated code

private ToolTip _vScrollToolTip;
private PageDrawing _DrawPanel;
private Button _RunButton;
private PictureBox _WarningButton;
private ScrollableControl _ParameterPanel;
private RdlViewerFind _FindCtl;

		
		#endregion
private HScrollBar _hScroll;
private VScrollBar _vScroll;


private void InitializeComponent()
{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(RdlViewer));
            this.DoubleBuffered = true;
			this._RunButton = new Majorsilence.Forms.Button();
			this._hScroll = new Majorsilence.Forms.HScrollBar();
			this._vScroll = new Majorsilence.Forms.VScrollBar();
			this._DrawPanel = new Majorsilence.Reporting.RdlViewer.PageDrawing();
			this.SuspendLayout();
			// 
			// _RunButton
			// 
			resources.ApplyResources(this._RunButton, "_RunButton");
			this._RunButton.Name = "_RunButton";
			this._RunButton.UseVisualStyleBackColor = true;
			this._RunButton.Click += new System.EventHandler(this.ParametersViewClick);
			// 
			// _hScroll
			// 
			resources.ApplyResources(this._hScroll, "_hScroll");
			this._hScroll.Name = "_hScroll";
			this._hScroll.Scroll += this.OnHScroll;
			// 
			// _vScroll
			// 
			resources.ApplyResources(this._vScroll, "_vScroll");
			this._vScroll.Name = "_vScroll";
			this._vScroll.Scroll += this.OnVScroll;
			// 
			// _DrawPanel
			// 
			resources.ApplyResources(this._DrawPanel, "_DrawPanel");
			this._DrawPanel.BorderStyle = Majorsilence.Forms.BorderStyle.FixedSingle;
			this._DrawPanel.Name = "_DrawPanel";
			this._DrawPanel.Paint += this.DrawPanelPaint;
			this._DrawPanel.KeyDown += this.DrawPanelKeyDown;
			this._DrawPanel.Resize += new System.EventHandler(this.DrawPanelResize);
			// 
			// RdlViewer
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this._vScroll);
			this.Controls.Add(this._hScroll);
			this.Controls.Add(this._RunButton);
			this.Controls.Add(this._DrawPanel);
			this.Name = "RdlViewer";
			this.ResumeLayout(false);

}
		
	}
}
