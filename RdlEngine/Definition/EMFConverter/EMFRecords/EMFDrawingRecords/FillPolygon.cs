
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Drawing2D = Majorsilence.Forms.Drawing.Drawing2D;
#else
using Draw2 = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
#endif

namespace Majorsilence.Reporting.Rdl
{
    internal class FillPolygon : DrawBase
    {
        internal FillPolygon(Single Xin, Single Yin, Single WidthIn, Single HeightIn, System.Collections.Hashtable ObjectTableIn)
        {
            X = Xin;
            Y = Yin;
            Width = WidthIn;
            Height = HeightIn;
            ObjectTable = ObjectTableIn;
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
                _fr.ReadByte();
                //Byte 2 is the real flags
                byte RealFlags = _fr.ReadByte();
                // 0 1 2 3 4 5 6 7
                // X X X X X X C S                
                // if C = 1 Data int16 else float!
                //PJR20220801 - (int) Math.Pow(2,6) !=64 it's 63!!! Argh!!!
                bool Compressed = ((RealFlags >> 6) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 6)) == (int)Math.Pow(2, 6));
                bool BrushIsARGB = ((RealFlags >> 7) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 7)) == (int)Math.Pow(2, 7));

                _ms = new MemoryStream(RecordData);
                _br = new BinaryReader(_ms);
                Draw2.Brush b;
                if (BrushIsARGB)
                {
                    byte A, R, G, B;
                    B = _br.ReadByte();
                    G = _br.ReadByte();
                    R = _br.ReadByte();
                    A = _br.ReadByte();
                    b = new Draw2.SolidBrush(System.Drawing.Color.FromArgb(A, R, G, B));
                }
                else
                {
                    UInt32 BrushID = _br.ReadUInt32();
                    EMFBrush EMFb = (EMFBrush)ObjectTable[(byte)BrushID];
                    b = EMFb.myBrush;
                }
                UInt32 NumberOfPoints = _br.ReadUInt32();
                if (Compressed)
                {
                    DoCompressed(NumberOfPoints, _br, b);
                }
                else
                {
                    DoFloat(NumberOfPoints, _br, b);
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

        private void DoFloat(UInt32 NumberOfPoints, BinaryReader _br, Draw2.Brush b)
        {
            System.Drawing.PointF[] Ps = new System.Drawing.PointF[NumberOfPoints];
            for (int i = 0; i < NumberOfPoints; i++)
            {
                  
                Ps[i].X = (float)(X +(_br.ReadSingle() * SCALEFACTOR));
                Ps[i].Y = (float)(Y + (_br.ReadSingle() * SCALEFACTOR));  
            }
            DoInstructions(Ps, b);
        }

        private void DoCompressed(UInt32 NumberOfPoints, BinaryReader _br, Draw2.Brush b)
        {
            System.Drawing.PointF[] Ps = new System.Drawing.PointF[NumberOfPoints];
            for (int i = 0; i < NumberOfPoints; i++)
            { 
                Int16 px = _br.ReadInt16();
                Int16 py = _br.ReadInt16();
                Ps[i].X = X + (px * SCALEFACTOR);//X + px * SCALEFACTOR;
                Ps[i].Y = Y + (py * SCALEFACTOR);//Y + py * SCALEFACTOR;
            } 
            DoInstructions(Ps, b);
        }

        private void DoInstructions(System.Drawing.PointF[] Ps, Draw2.Brush b)
        {
            PagePolygon pl = new PagePolygon();
            //pl.X = X * SCALEFACTOR;
            //pl.Y = Y * SCALEFACTOR;
            pl.Points = Ps;
            StyleInfo SI = new StyleInfo();           

            switch (b.GetType().Name)
            {
                case "SolidBrush":
                    Draw2.SolidBrush theBrush = (Draw2.SolidBrush)b;
                    SI.Color = theBrush.Color;
                    SI.BackgroundColor = theBrush.Color;
                    break;
                case "LinearGradientBrush":
                    Drawing2D.LinearGradientBrush linBrush = (Drawing2D.LinearGradientBrush)b;
                    SI.BackgroundGradientType = BackgroundGradientTypeEnum.LeftRight;
                    SI.BackgroundColor = linBrush.LinearColors[0];
                    SI.BackgroundGradientEndColor = linBrush.LinearColors[1];
                    break;
                case "HatchBrush":
                    Drawing2D.HatchBrush hatBrush = (Drawing2D.HatchBrush)b;
                    SI.BackgroundColor = hatBrush.BackgroundColor;
                    SI.Color = hatBrush.ForegroundColor;
                    SI.PatternType = StyleInfo.GetPatternType(hatBrush.HatchStyle);
                    break;
                default:
                    break;
            }

            pl.SI = SI;
            items.Add(pl);
        }
    }
}


