
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
    internal class DrawCurve : DrawBase
    {
        internal DrawCurve(Single Xin, Single Yin, Single WidthIn, Single HeightIn, System.Collections.Hashtable ObjectTableIn)
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
                //Byte 1 will be ObjectID
                byte ObjectID = _fr.ReadByte();
                //Byte 2 is the real flags
                byte RealFlags = _fr.ReadByte();
                // 0 1 2 3 4 5 6 7
                // X X X X X X C X            
                // if C = 1 Points are int16, 0 = Points are Float

                //PJR20220801 - (int) Math.Pow(2,6) !=64 it's 63!!! Argh!!!
                bool Compressed = ((RealFlags >> 6) & 1) == 1; //((RealFlags & (int)Math.Pow(2, 6)) == (int)Math.Pow(2, 6));                             

                _ms = new MemoryStream(RecordData);
                _br = new BinaryReader(_ms);

                float  Tension = _br.ReadSingle();// _br.ReadUInt32(); //20081110 PJR & GJL EMF+ Definition is WRONG!!!
                UInt32 Offset = _br.ReadUInt32();
                UInt32 NumberOfPoints = _br.ReadUInt32();
                UInt32 NumberOfSegments = _br.ReadUInt32();
                EMFPen Emfp = (EMFPen)ObjectTable[ObjectID];
                Draw2.Pen p = Emfp.myPen;
                //EMFBrush b = p.myBrush;                
                if (Compressed)
                {
                    DoCompressed(NumberOfPoints, _br, p,Offset,NumberOfSegments, Tension);
                }               
                else
                {
                    DoFloat(NumberOfPoints, _br, p, Offset, NumberOfSegments, Tension);
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

        private void DoFloat(UInt32 NumberOfSegments, BinaryReader _br, Draw2.Pen p, UInt32 Offset, UInt32 NumberOfPoints, float Tension)
        {
            System.Drawing.PointF[] Points = null;
            //bool first = true;
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Points[i].X = _br.ReadSingle();
                Points[i].Y = _br.ReadSingle();
            }
            DoInstructions(Points, p, Offset, NumberOfPoints, Tension);
        }


        private void DoCompressed(UInt32 NumberOfSegments, BinaryReader _br, Draw2.Pen p, UInt32 Offset, UInt32 NumberOfPoints, float Tension)
        {
            //bool first = true;
            System.Drawing.PointF[] Points = new System.Drawing.PointF[NumberOfPoints];
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Points[i].X = (float) _br.ReadInt16();
                Points[i].Y = (float) _br.ReadInt16();
            }
            DoInstructions(Points, p, Offset, NumberOfSegments, Tension);
        }

        private void DoInstructions(System.Drawing.PointF[] points, Draw2.Pen p, UInt32 Offset, UInt32 NumberOfPoints, float Tension)
        {
            BorderStyleEnum ls = getLineStyle(p);
            //Well we only draw lines at the moment.... 

            switch (p.Brush.GetType().Name)
            {
                case "SolidBrush":
                   Draw2.SolidBrush theBrush = (Draw2.SolidBrush)p.Brush;
                    PageCurve pc = new PageCurve();
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i].X = X + (points[i].X * SCALEFACTOR);
                        points[i].Y = Y + (points[i].Y * SCALEFACTOR);
                    }
                    pc.Points = points;
                    pc.Offset = (int) Offset;
                    pc.Tension = Tension;
                    

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
                    pc.SI = SI;
                    items.Add(pc);
                    break;
                default:
                    break;
            }
        }
    }
}
