
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
#else
using Draw2 = System.Drawing;
#endif

namespace Majorsilence.Reporting.Rdl
{
    internal class DrawEllipse : DrawBase
    {
        internal DrawEllipse(Single Xin, Single Yin, Single WidthIn, Single HeightIn, System.Collections.Hashtable ObjectTableIn)
        {
            X = Xin;
            Y = Yin;
            Width = WidthIn;
            Height = HeightIn;          
            items = new List<PageItem>();
        }

        public List<PageItem> Process(int Flags, byte[] RecordData)
        {
            MemoryStream _ms = null;
            BinaryReader _br = null;
            MemoryStream _fs = null;
            BinaryReader _fr = null;
            try
            {              
                _fs = new MemoryStream(BitConverter.GetBytes(Flags));
                _fr = new BinaryReader(_fs);
                byte ObjectID = _fr.ReadByte();
                //Byte 1 will be ignored
                byte RealFlags = _fr.ReadByte();
                //Byte 2 will be brushtype
                // 0 1 2 3 4 5 6 7
                // X X X X X X C X
                // if C = 1 int16, 0 = Points are Float (ignore P)              
                //PJR20220801 - (int) Math.Pow(2,6) !=64 it's 63!!! Argh!!!
                bool Compressed = ((RealFlags >> 6) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 6)) == (int)Math.Pow(2, 6));

                _ms = new MemoryStream(RecordData);
                _br = new BinaryReader(_ms);

                EMFPen EMFp = (EMFPen)ObjectTable[ObjectID];
                Draw2.Pen p = EMFp.myPen;

                if (Compressed)
                {
                    Int16 Xp = _br.ReadInt16();
                    Int16 Yp = _br.ReadInt16();
                    Int16 Wid = _br.ReadInt16();
                    Int16 Hgt = _br.ReadInt16();
                    DoEllipse(p, Xp, Yp, Wid, Hgt);
                }
                else
                {
                    Single Xp = _br.ReadSingle();
                    Single Yp = _br.ReadSingle();
                    Single Wid = _br.ReadSingle();
                    Single Hgt = _br.ReadSingle();
                    DoEllipse(p, Xp, Yp, Wid, Hgt);
                }
                return items;
            }
            finally
            {
                if (_br != null)
                    _br.Close();
                if (_ms != null)
                    _ms.Dispose();
                if (_fr != null)
                    _fr.Close();
                if (_fs != null)
                    _fs.Dispose();
            }
        }
        private void DoEllipse(Draw2.Pen p, Single Xp, Single Yp, Single Wid, Single Hgt)
        {
            BorderStyleEnum ls = getLineStyle(p);

            System.Drawing.Color col = System.Drawing.Color.Black;

            if (p.Brush.GetType().Name.Equals("SolidBrush"))
            {
               Draw2.SolidBrush theBrush = (Draw2.SolidBrush)p.Brush;
                col = theBrush.Color;
            }

            PageEllipse pl = new PageEllipse();
            pl.X = X + (Xp * SCALEFACTOR);
            pl.Y = Y + (Yp * SCALEFACTOR);
            pl.W = Wid * SCALEFACTOR;
            pl.H = Hgt * SCALEFACTOR;

            StyleInfo SI = new StyleInfo();
            SI.Color = col;
            SI.BColorTop = col;
            SI.BStyleTop = ls;
            SI.BWidthTop = p.Width * SCALEFACTOR;
            pl.SI = SI;
            items.Add(pl);  

            //Lines.AppendFormat("\r\n"); //CrLf
            //Lines.AppendFormat("q\t"); //Push graphics state onto stack
            //Lines.AppendFormat("{0} w\t", p.Width * ScaleX); //set width of path
            //Lines.AppendFormat("{0} \t", linestyle); //line style from pen... 
            //Lines.AppendFormat("{0} {1} {2} RG\t", Math.Round(R / 255.0, 3), Math.Round(G / 255.0, 3), Math.Round(B / 255.0, 3)); //Set RGB colours
            //Lines.AppendFormat("{0} {1} {2} rg\t", Math.Round(R / 255.0, 3), Math.Round(G / 255.0, 3), Math.Round(B / 255.0, 3)); //Set RGB colours
            ////Need some bezier curves to  draw an ellipse.. we can't draw a circle, but we can get close.
            //Double k = 0.5522847498;
            //Double RadiusX = (Wid / 2.0) * ScaleX;
            //Double RadiusY = (Hgt / 2.0) * ScaleY;
            //Double Y4 = Y + Height - Yp * ScaleY;
            //Double X1 = Xp * ScaleX + X;
            //Double Y1 = Y4 - RadiusY;
            //Double X4 = X1 + RadiusX;

            //Lines.AppendFormat("{0} {1} m\t", X1, Y1);//FirstPoint..

            //Double kRy = k * RadiusY;
            //Double kRx = k * RadiusX;

            ////Control Point 1 will be on the same X as point 1 and be -kRy Y
            //Double X2 = X1;
            //Double Y2 = Y1 + kRy;

            //Double X3 = X4 - kRx;
            //Double Y3 = Y4;

            //Lines.AppendFormat("{0} {1} {2} {3} {4} {5}  c\t", X2, Y2, X3, Y3, X4, Y4); //Upper Left Quadrant

            //X1 += 2 * RadiusX;
            //X2 = X1;
            //X3 += 2 * kRx;

            //Lines.AppendFormat("{0} {1} {2} {3} {4} {5}  c\t", X3, Y3, X2, Y2, X1, Y1); //Upper Right Quadrant

            //Y2 -= 2 * kRy;
            //Y3 -= 2 * RadiusY;
            //Y4 = Y3;

            //Lines.AppendFormat("{0} {1} {2} {3} {4} {5}  c\t", X2, Y2, X3, Y3, X4, Y4); //Lower Right Quadrant

            //X1 -= 2 * RadiusX;
            //X2 = X1;
            //X3 -= 2 * kRx;

            //Lines.AppendFormat("{0} {1} {2} {3} {4} {5}  c\t", X3, Y3, X2, Y2, X1, Y1); //Lower Right Quadrant

            //Lines.AppendFormat("S\t");//Stroke path
            //Lines.AppendFormat("Q\t");//Pop graphics state from stack 

        } 
    }
}
