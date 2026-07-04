
using System;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;
using Majorsilence.Forms;


namespace Majorsilence.Reporting.RdlDesign
{
	internal class SimpleToggle : CheckBox
	{
		private Color _UpColor;
		private Color _DownColor;
		private Color _Transparency;
		private bool bIn=false;

		public SimpleToggle() 
		{	
			this.Appearance = Appearance.Button; 

			this.UpColor = Color.LightGray;
			this.DownColor = Color.Azure;
			this.TranparencyColor = Color.White;

			this.BackColor = this.Checked? this.DownColor: this.UpColor;
			this.ForeColor = this.Enabled? Color.Black: Color.Gray;
			this.Paint += this.DrawPanelPaint;
			this.MouseEnter += SimpleToggle_MouseEnter;
			this.MouseLeave += SimpleToggle_MouseLeave;
		}

		private void DrawPanelPaint(object sender, Majorsilence.Forms.PaintEventArgs e)
		{

			Graphics g = e.Graphics;
			Brush b = null;
			Pen p = null;

			try			// never want to die in here
			{

				b = new SolidBrush(this.BackColor);
				g.FillRectangle(b, e.ClipRectangle);
				if (this.Checked || bIn)
				{
					g.DrawRectangle(Pens.Blue, 0, 0, this.Width-1, this.Height-1);
				}
				if (this.Image != null)
				{
					int x = (this.Width - this.Image.Width) / 2;
					int y = (this.Height - this.Image.Height) / 2;

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color UpColor
		{
			get { return this._UpColor;	}
			set { this._UpColor = value; }
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color DownColor
		{
			get { return this._DownColor;	}
			set { this._DownColor = value; }
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			base.OnCheckedChanged(e);		// CheckBox OnCheckedChanged method	 
			this.BackColor = this.Checked? this.DownColor: this.UpColor;
		}

		protected override void OnClick(MouseEventArgs e)
		{
			base.OnClick(e);		// CheckBox OnClick method
			this.BackColor = this.Checked? this.DownColor: this.UpColor;
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);		// CheckBox OnEnabled method
			if (this.Enabled)
			{
				this.ForeColor = Color.Black;
				this.BackColor = this.Checked? this.DownColor: this.UpColor;
			}
			else
			{
				this.ForeColor = Color.LightGray;
				this.BackColor = Color.LightGray;
			}
		}

		private void SimpleToggle_MouseEnter(object sender, EventArgs e)
		{
			bIn = true;
		}

		private void SimpleToggle_MouseLeave(object sender, EventArgs e)
		{
			bIn = false;
		}
	}
}
