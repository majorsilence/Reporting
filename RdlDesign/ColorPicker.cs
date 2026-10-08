using System.Drawing;
using Majorsilence.Forms.Drawing;

using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// It's very crazy control. Need replace it. TODO
	/// </summary>
    public class ColorPicker : ComboBox
    { 
        private const int RECTCOLOR_LEFT = 4;
        private const int RECTCOLOR_TOP = 2;
        private const int RECTCOLOR_WIDTH = 10;
        ColorPickerPopup _DropListBox;

        public ColorPicker()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList; // DropDownList
            DropDownHeight = 1;
            Font = new Font("Arial", 8, FontStyle.Bold | FontStyle.Italic);

            _DropListBox = new ColorPickerPopup(this);

			if (!DesignMode)
			{
				Items.AddRange(StaticLists.ColorList);
			}

            // Majorsilence.Forms.ComboBox has no OnDrawItem virtual method to override -- only a
            // DrawItem event, which is itself a no-op (`add { } remove { }`) since ComboBox
            // doesn't support owner-draw rendering at all. Subscribing preserves the code's shape
            // for whenever that gets real support; the swatch/fx rendering below simply won't
            // paint until then (documented gap, same class the file's own header already flags:
            // "It's very crazy control. Need replace it. TODO").
            DrawItem += ColorPicker_DrawItem;
        }

        private void ColorPicker_DrawItem(object sender, DrawItemEventArgs e) => OnDrawItem(e);

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                string v = value == null ? "" : value;
                if (!this.Items.Contains(v))    // make sure item is always in the list
                    this.Items.Add(v);
                base.Text = v;
            }
        }
        private void OnDrawItem(DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Color BlockColor = Color.Empty;
            int left = RECTCOLOR_LEFT;
            if (e.State == DrawItemState.Selected || e.State == DrawItemState.None)
                e.DrawBackground();
            if (e.Index == -1)
            {
                BlockColor = SelectedIndex < 0 ? BackColor : DesignerUtility.ColorFromHtml(this.Text, Color.Empty);
            }
            else
                BlockColor = DesignerUtility.ColorFromHtml((string)this.Items[e.Index], Color.Empty);
            // Fill rectangle
            if (BlockColor.IsEmpty && this.Text.StartsWith("="))
            {
                g.DrawString("fx", this.Font, Brushes.Black, e.Bounds);
            }
            else
            {
                g.FillRectangle(new SolidBrush(BlockColor), left, e.Bounds.Top + RECTCOLOR_TOP, RECTCOLOR_WIDTH,
                    ItemHeight - 2 * RECTCOLOR_TOP);
            }
        }

        protected override void OnDropDownOpened(System.EventArgs e)
        {
            base.OnDropDownOpened(e);
            _DropListBox.Location = this.PointToScreen(new Point(0, this.Height));
            _DropListBox.Show();
        }
    }
}
