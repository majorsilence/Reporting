
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using RdlReader.Resources;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlViewer;

namespace Majorsilence.Reporting.RdlReader
{
    /// <summary>
    /// Summary description for ZoomTo.
    /// </summary>
    public partial class ZoomTo 
    {
        public ZoomTo(RdlViewer.RdlViewer viewer)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            _Viewer = viewer;
            // set the intial value for magnification
            if (_Viewer.ZoomMode == ZoomEnum.FitPage)
                cbMagnify.Text = "Fit Page";
            else if (_Viewer.ZoomMode == ZoomEnum.FitWidth)
                cbMagnify.Text = "Fit Width";
            else if (_Viewer.Zoom == 1)
                cbMagnify.Text = "Actual Size";
            else
            {
                string formatted = string.Format("{0:#0.##}", _Viewer.Zoom * 100);
                if (formatted[formatted.Length - 1] == '.')
                    formatted = formatted.Substring(0, formatted.Length - 2);
                formatted = formatted + "%";
                cbMagnify.Text = formatted;
            }

        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            switch (cbMagnify.Text)
            {
                case "Fit Page":
                    _Viewer.ZoomMode = ZoomEnum.FitPage;
                    break;
                case "Actual Size":
                    _Viewer.Zoom = 1;
                    break;
                case "Fit Width":
                    _Viewer.ZoomMode = ZoomEnum.FitWidth;
                    break;
                default:
                    string z = cbMagnify.Text.Replace("%", "");
                    try
                    {
                        float zfactor = (float)(Convert.ToSingle(z) / 100.0);
                        _Viewer.Zoom = zfactor;
                    }
                    catch
                    {
                        MessageBox.Show(this, Strings.ZoomTo_ShowI_MagnificationLevelInvalid);
                        return;
                    }
                    break;
            }
            DialogResult = DialogResult.OK;
        }

        private void bCancel_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
