using System.Drawing;
using Majorsilence.Forms;

namespace Majorsilence.WinformUtils
{
    // Majorsilence.Forms.Form does not derive from Control (unlike System.Windows.Forms.Form,
    // which derives from ContainerControl -> Control): it's a separate WindowBase-rooted
    // top-level-window abstraction. That means several members the original WinForms code relied
    // on don't exist here: no int Width/Height (only the settable Size struct), no SizeChanged/
    // Move events, no HandleCreated event, no DoubleBuffered property, and Refresh() is
    // Invalidate() instead.
    //
    // The elapsed-time / bounds-tracking used to run off a 50ms System.Threading.Timer that
    // BeginInvoke'd back to the UI thread, reset Bounds, built a fresh ComponentResourceManager
    // and pumped Application.DoEvents() -- every tick, 20x a second. Worse, that pool-thread
    // timer kept firing after Close()/Dispose(), and a queued Bounds= landing on a torn-down
    // window resurrected it: ReportDesigner ended up with a stack of black "N Seconds" windows
    // that never went away over a preview that had actually finished. Now a plain UI-thread
    // Majorsilence.Forms.Timer at 250ms, stopped in FormClosed, with no DoEvents.
    internal class WaitForm : Form
    {
        private DateTime Started;
        private Majorsilence.Forms.ProgressBar progressBar1 = null!;
        private Majorsilence.Forms.Label lblTimeTaken = null!;
        private readonly Majorsilence.Forms.Timer timer1 = new() { Interval = 250 };
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

            timer1.Tick += (_, __) =>
            {
                if (IsDisposed)
                    return;

                SyncToTracked();
                UpdateElapsedText();
            };

            this.Shown += (_, __) => timer1.Start();
            this.FormClosed += (_, __) => timer1.Stop();
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

        private void UpdateElapsedText()
        {
            var time = DateTime.Now - Started;
            lblTimeTaken.Text = time.TotalMinutes < 1
                ? $"{time.Seconds} {Strings.WaitForm_Seconds}"
                : $"{(int)time.TotalMinutes} {Strings.WaitForm_Minutes} {time.Seconds} {Strings.WaitForm_Seconds}";
        }

        private System.ComponentModel.IContainer? components;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer1.Stop();
                timer1.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
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
