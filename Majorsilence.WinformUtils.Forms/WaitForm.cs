using System.Drawing;
using Majorsilence.Forms;

namespace Majorsilence.WinformUtils
{
    // Majorsilence.Forms.Form does not derive from Control (unlike System.Windows.Forms.Form,
    // which derives from ContainerControl -> Control): it's a separate WindowBase-rooted
    // top-level-window abstraction. That means several members the original WinForms code relied
    // on don't exist here: no int Width/Height (only the settable Size struct), no SizeChanged/
    // Move events, no HandleCreated event, no DoubleBuffered property, and Refresh() is
    // Invalidate() instead. The single poll timer below now also re-syncs Bounds against the
    // parent every tick, replacing the SizeChanged/Move event subscriptions this class used to
    // need -- see MIGRATION-NOTES.md for the full writeup of this gap.
    internal class WaitForm : Form
    {
        private DateTime Started;
        private Majorsilence.Forms.ProgressBar progressBar1 = null!;
        private Majorsilence.Forms.Label lblTimeTaken = null!;
        private System.Threading.Timer? timer1;
        private readonly Func<Rectangle> _getTrackedBounds;
        public delegate bool CheckStopWaitDialog();

        /// <param name="getTrackedBounds">
        /// Computes the tracked window/control's current screen bounds. A plain delegate rather
        /// than a ContainerControl/Form parameter, since those two types share no common ancestor
        /// beyond Component in Majorsilence.Forms and each needs a different bounds calculation
        /// (Form.Location is already screen-relative; a child control needs PointToScreen).
        /// </param>
        public WaitForm(Func<Rectangle> getTrackedBounds)
        {
            _getTrackedBounds = getTrackedBounds;
            FormBorderStyle = FormBorderStyle.None;
            InitializeComponent();
            this.BackColor = Color.Black;

            Started = DateTime.Now;
            this.Shown += (_, __) =>
            {
                timer1 = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        this.BeginInvoke(new Action(async () =>
                        {
                            SyncToTracked();
                            await timer1_Tick(null, null);
                        }));
                    }
                    catch (ObjectDisposedException) { }
                }, null, 0, 50);
            };
        }

        private void SyncToTracked()
        {
            Bounds = _getTrackedBounds();
            PlaceControls();
        }

        private void PlaceControls()
        {
            progressBar1.Location = new Point((this.Size.Width - this.progressBar1.Size.Width) / 2,
                (this.Size.Height - this.progressBar1.Size.Height) / 2);
            lblTimeTaken.Location = new Point((this.Size.Width - this.lblTimeTaken.Size.Width) / 2 + 20,
                (this.Size.Height - this.lblTimeTaken.Size.Height) / 2 + 50);
        }

        private async Task timer1_Tick(object? sender, EventArgs? e)
        {
            Majorsilence.Forms.ComponentResourceManager resources =
                new Majorsilence.Forms.ComponentResourceManager(typeof(Strings));
            var time = DateTime.Now - Started;
            if (time.TotalMinutes < 1)
                lblTimeTaken.Text = string.Format("{0} {1}", time.Seconds, resources.GetString("WaitForm_Seconds"));
            else
                lblTimeTaken.Text = string.Format("{0} {1} {2} {3}", (int)time.TotalMinutes,
                    resources.GetString("WaitForm_Minutes"),
                    time.Seconds, resources.GetString("WaitForm_Seconds"));

            Application.DoEvents();
        }

        private System.ComponentModel.IContainer? components;

        protected override void Dispose(bool disposing)
        {
            timer1?.Dispose();
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Majorsilence.Forms.ComponentResourceManager resources =
                new Majorsilence.Forms.ComponentResourceManager(typeof(Strings));
            this.progressBar1 = new Majorsilence.Forms.ProgressBar();
            this.lblTimeTaken = new Majorsilence.Forms.Label();
            this.SuspendLayout();

            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Style = Majorsilence.Forms.ProgressBarStyle.Marquee;

            this.lblTimeTaken.Name = "lblTimeTaken";
            this.lblTimeTaken.ForeColor = Color.White;

            this.ControlBox = false;
            this.Controls.Add(this.lblTimeTaken);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.ResumeLayout(false);
            this.PerformLayout();

            SyncToTracked();
        }
    }
}
