
using System;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;
using Majorsilence.Forms;


namespace Majorsilence.Reporting.RdlDesign
{
	internal class SimpleButton : Button
	{
		private Color _Transparency;
		private bool bDown=false;
		private bool bIn=false;

		public SimpleButton(Control parent) 
		{	
			this.Parent = parent;
			this.TranparencyColor = Color.White;
            this.DoubleBuffered = true;

			this.BackColor = parent.BackColor;
			this.ForeColor = this.Enabled? Color.Black: Color.Gray;
			this.MouseDown += SimpleButton_MouseDown;
			this.MouseUp += SimpleButton_MouseUp;
			this.MouseEnter += SimpleButton_MouseEnter;
			this.MouseLeave += SimpleButton_MouseLeave;
			this.Paint += this.DrawPanelPaint;
		}

		private void DrawPanelPaint(object sender, Majorsilence.Forms.PaintEventArgs e)
		{

			Graphics g = e.Graphics;
			Brush b = null;
			Pen p = null;

			try			// never want to die in here
			{
				b = new SolidBrush(this.Enabled? this.BackColor: Color.LightGray);
				g.FillRectangle(b, e.ClipRectangle);
				if (bIn && this.Enabled)
					g.DrawRectangle(Pens.Blue, 0, 0, this.Width-1, this.Height-1);

				if (this.Image != null)
				{
					int x = (this.Width - this.Image.Width) / 2;
					int y = (this.Height - this.Image.Height) / 2;
					if (bDown && bIn)
					{
						x += 1;
						y += 1;
					}

					// Draw Image using the transparency color
					ImageAttributes imageAttr = new ImageAttributes();
					imageAttr.SetColorKey(_Transparency, _Transparency,
						ColorAdjustType.Default);

					g.DrawImage(this.Image,         // Image
						new Rectangle(x, y, this.Image.Width, this.Image.Height),    // Dest. rect.
						0,							// srcX
						0,							// srcY
						this.Image.Width,           // srcWidth
						this.Image.Height,          // srcHeight
						GraphicsUnit.Pixel,			// srcUnit
						imageAttr);					// ImageAttributes
				}
				else
				{
					StringFormat format = new StringFormat(StringFormatFlags.NoWrap);
					g.DrawString(this.Text, this.Font, Brushes.Black, new Rectangle(2, 2, this.Width, this.Height), format);
				}
			}
			catch {}	// todo draw the error message
			finally
			{
				if (b != null)
					b.Dispose();
				if (p != null)
					p.Dispose();
			}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color TranparencyColor
		{
			get { return this._Transparency;	}
			set { this._Transparency = value; }
		}

		private void SimpleButton_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				bDown = true;
		}

		private void SimpleButton_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				bDown = false;
		}

		private void SimpleButton_MouseEnter(object sender, EventArgs e)
		{
			bIn = true;
		}

		private void SimpleButton_MouseLeave(object sender, EventArgs e)
		{
			bIn = false;
		}
	}
}
