
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Drawing2D;
using Majorsilence.Forms.Drawing.Imaging;
using Majorsilence.Forms;
using System.Globalization;
using System.Net;

namespace Majorsilence.Reporting.RdlDesign
{

	/// <summary>
	/// Control for providing a designer image of RDL.   Works directly off the RDL XML.
	/// </summary>
    public class DesignEditLines : UserControl, System.ComponentModel.ISupportInitialize
    {
        Majorsilence.Forms.RichTextBox editor=null;
        int saveTbEditorLines = -1;
        int _LineHeight = -1;

        public DesignEditLines()
            : base()
        {
            // force to double buffering for smoother drawing
            this.DoubleBuffered = true;

            this.Paint += DesignEditLinesPaint;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Majorsilence.Forms.RichTextBox Editor
        {
            get { return editor; }
            set 
            { 
                editor = value;
                editor.TextChanged += editor_TextChanged;
                editor.Resize += editor_Resize;
                editor.VScroll += editor_VScroll;
            }
        }

		private void DesignEditLinesPaint(object sender, Majorsilence.Forms.PaintEventArgs e)
        {
            Lines_Draw(e.Graphics);
        } 
        
        private void Lines_Draw(Graphics g)
        {
            if (!this.Visible || editor == null)
                return;
            try
            {  // its possible that there are less than 2 lines; so trap the error
                if (_LineHeight <= 0)
                    _LineHeight = editor.GetPositionFromCharIndex(editor.GetFirstCharIndexFromLine(2)).Y -
                          editor.GetPositionFromCharIndex(editor.GetFirstCharIndexFromLine(1)).Y;
            }
            catch { return; }
            if (_LineHeight <= 0)
                return;

            // Get the first line index and location
            int first_index;
            int first_line;
            int first_line_y;
            first_index = editor.GetCharIndexFromPosition(new
                     Point(0, (int)(g.VisibleClipBounds.Y + _LineHeight / 3)));
            first_line = editor.GetLineFromCharIndex(first_index);
            first_line_y = editor.GetPositionFromCharIndex(first_index).Y;

            //  Draw the lines
            SolidBrush sb = new SolidBrush(Control.DefaultBackColor);
            g.FillRectangle(sb, g.VisibleClipBounds);
            sb.Dispose();

            int i = first_line;
            float y = first_line_y + _LineHeight * (i - first_line - 1);
            int lCount = this.saveTbEditorLines < 0? editor.Lines.Length:
                                        this.saveTbEditorLines;   // calc lines if not initialized
            Font eFont = editor.Font;
            float maxHeight = g.VisibleClipBounds.Y + g.VisibleClipBounds.Height;
            while (y < maxHeight)
            {
                string l = i.ToString();
                g.DrawString(l, editor.Font, Brushes.DarkBlue,
                    this.Width - (g.MeasureString(l, eFont).Width + 4), y);
                i += 1;
                if (i > lCount)
                    break;
                y = first_line_y + _LineHeight * (i - first_line - 1);
            }
        }

        private void editor_Resize(object sender, EventArgs e)
        {
            using (Graphics g = this.CreateGraphics())
            {
                Lines_Draw(g);
            }
        }

        private void editor_VScroll(object sender, EventArgs e)
        {
            using (Graphics g = this.CreateGraphics())
            {
                Lines_Draw(g);
            }
        }

		private void editor_TextChanged(object sender, System.EventArgs e)
        {
            if (editor == null)
                return;

            // when # of lines change we may need to repaint the line #s
            int eLines = editor.Lines.Length;
            if (saveTbEditorLines != eLines)
            {
                saveTbEditorLines = eLines;
                using (Graphics g = this.CreateGraphics())
                {
                    Lines_Draw(g);
                }
            }
        }

        #region ISupportInitialize Members

        void System.ComponentModel.ISupportInitialize.BeginInit()
        {
            return;
        }

        void System.ComponentModel.ISupportInitialize.EndInit()
        {
            return;
        }

        #endregion
    }
}
