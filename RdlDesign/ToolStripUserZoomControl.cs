using System;
using System.ComponentModel;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
    //Declare a class that inherits from ToolStripControlHost.
    public class ToolStripUserZoomControl : ToolStripControlHost
    {
        // Call the base constructor passing in a MonthCalendar instance.
        // Majorsilence.Forms.ToolStripControlHost has no OnSubscribeControlEvents/
        // OnUnsubscribeControlEvents virtual hooks (or any Dispose override point at all --
        // ToolStripItem -> MenuItem : ILayoutable, not Component-derived), so subscribe directly
        // here instead of via that pattern. No matching unsubscribe: this control lives as long
        // as the toolbar itself, so there's nothing meaningful to leak in practice.
        public ToolStripUserZoomControl() : base(new UserZoomControl())
        {
            ZoomControl!.ZoomChanged += new EventHandler<UserZoomControl.CambiaValori>(ZoomControl1_ValueChanged);
        }

        public UserZoomControl ZoomControl
        {
            get
            {
                return Control as UserZoomControl;
            }
        }

        [Browsable(true)]
        public event EventHandler<UserZoomControl.CambiaValori> ZoomChanged;


        // Raise the DateChanged event.
        private void ZoomControl1_ValueChanged(object sender, UserZoomControl.CambiaValori e)
        {
            if (this.ZoomChanged != null)
                this.ZoomChanged(this, e);
        }

    }
}
