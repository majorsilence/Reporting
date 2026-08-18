
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
    //takes the record data and returns the instructions for Draw2...I guess.    
    internal class DrawLines : DrawBase
    {
        internal DrawLines(Single Xin, Single Yin, Single WidthIn, Single HeightIn,System.Collections.Hashtable ObjectTableIn)
        {
            X = Xin;
            Y = Yin;
            Width = WidthIn;
            Height = HeightIn;          
            ObjectTable = ObjectTableIn;
            items = new List<PageItem>();
        }

        public List<PageItem> Process(int Flags,byte[] RecordData)
        {
            MemoryStream _ms = null;
            BinaryReader _br = null;
            MemoryStream _fs = null;
            BinaryReader _fr = null;

            try
            {              
                _fs = new MemoryStream(BitConverter.GetBytes(Flags));
                _fr = new BinaryReader(_fs);
                //Byte 1 will be ObjectID
                byte ObjectID = _fr.ReadByte();
                //Byte 2 is the real flags
                byte RealFlags = _fr.ReadByte();
                // 0 1 2 3 4 5 6 7
                // X X X P X L C X
                // if P = 1 Points are Relative to previous point (Ignore C)
                // if C = 1 Points are int16, 0 = Points are Float (ignore P)
                // if L = 1 Draw a line between last and first points
                //PJR20220801 - (int) Math.Pow(2,6) !=64 it's 63!!! Argh!!!
                bool Compressed = ((RealFlags >> 6) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 6)) == (int)Math.Pow(2, 6));
                bool Relative = false;
                if (!Compressed)                
                    Relative = ((RealFlags >> 3) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 3)) == (int)Math.Pow(2, 3));
                bool CloseShape = ((RealFlags >> 5) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 5)) == (int)Math.Pow(2, 5)); 
                _ms = new MemoryStream(RecordData);
                _br = new BinaryReader(_ms);
                UInt32 NumberOfPoints = _br.ReadUInt32(); 
                EMFPen Emfp = (EMFPen)ObjectTable[ObjectID];
                Draw2.Pen p = Emfp.myPen;               
                //EMFBrush b = p.myBrush;                
                if (Compressed)
                {
                    DoCompressed(NumberOfPoints, _br,p);
                }
                else if (Relative)
                {
                    DoRelative(NumberOfPoints, _br, p);
                }
                else
                {
                    DoFloat(NumberOfPoints, _br, p);
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

        private void DoFloat(UInt32 NumberOfPoints, BinaryReader _br, Draw2.Pen p)
        {
            System.Drawing.PointF[] Points = new System.Drawing.PointF[NumberOfPoints];
            bool first = true;
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Points[i].X = _br.ReadSingle();
                Points[i].Y = _br.ReadSingle();
            }
            System.Drawing.PointF PointA = new System.Drawing.PointF();
            System.Drawing.PointF PointB = new System.Drawing.PointF();
            for (int i = 0; i < NumberOfPoints; i++)
            {
                if (first)
                {
                    PointA = Points[i];
                    first = false;
                }
                else
                {
                    PointB = Points[i];
                    DoInstructions(PointA.X, PointB.X, PointA.Y, PointB.Y,p);
                    PointA = PointB;
                }
            }
        }

        private void DoRelative(UInt32 NumberOfPoints, BinaryReader _br, Draw2.Pen p)
        {
            throw new NotSupportedException("Relative Points are not supported");
        }

        private void DoCompressed(UInt32 NumberOfPoints, BinaryReader _br, Draw2.Pen p)
        {
            bool first = true;
            System.Drawing.Point[] Points = new System.Drawing.Point[NumberOfPoints];            
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Points[i].X = _br.ReadInt16();
                Points[i].Y = _br.ReadInt16();
            }
            System.Drawing.Point PointA= new System.Drawing.Point();
            System.Drawing.Point PointB = new System.Drawing.Point();
            for (int i = 0; i < NumberOfPoints; i++)
            {
                if (first)
                {
                    PointA = Points[i];
                    first = false;
                }
                else
                {
                    PointB = Points[i];
                    DoInstructions(PointA.X, PointB.X, PointA.Y, PointB.Y, p);
                    PointA = PointB;
                }
            }
        }

        private void DoInstructions(Single Xa,Single Xb, Single Ya, Single Yb,Draw2.Pen p)
        {
            BorderStyleEnum ls = getLineStyle(p);
           
          
            switch (p.Brush.GetType().Name)
            {
                case "SolidBrush":
                   Draw2.SolidBrush theBrush = (Draw2.SolidBrush)p.Brush;
                    PageLine pl = new PageLine();
                    pl.X = X + (Xa * SCALEFACTOR);
                    pl.Y = Y + (Ya * SCALEFACTOR);
                    pl.X2 = X + (Xb * SCALEFACTOR);
                    pl.Y2 = Y + (Yb * SCALEFACTOR);                    
                    
                    StyleInfo SI = new StyleInfo();
                    SI.Color = theBrush.Color;
                    SI.BColorTop = theBrush.Color;
                    SI.BStyleTop = ls;
                    SI.BWidthTop = p.Width * SCALEFACTOR;
                    SI.BColorBottom = theBrush.Color;
                    SI.BStyleBottom = ls;
                    SI.BWidthBottom = p.Width * SCALEFACTOR;
                    SI.BColorLeft = theBrush.Color;
                    SI.BStyleLeft = ls;
                    SI.BWidthLeft = p.Width * SCALEFACTOR;
                    SI.BColorRight = theBrush.Color;
                    SI.BStyleRight = ls;
                    SI.BWidthRight = p.Width * SCALEFACTOR; 
                    pl.SI = SI;
                    items.Add(pl);
                    break;
                default:
                    break;
            }
        }
    }
}
