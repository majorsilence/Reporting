
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if DRAWINGCOMPAT
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using System.DrawingCore.Imaging;
#else
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Runtime.InteropServices;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Rdl
{
    public class EMF
    {
        public List<PageItem> PageItems;
        private System.Collections.Hashtable ObjectTable;        
        private Metafile _mf;
        private Bitmap _bm;
        private MemoryStream _ms;
        private Graphics g;
        private Graphics.EnumerateMetafileProc m_delegate;
        private Single X, Y, Width, Height;
        //private Single XScale, YScale;
        internal EMF(Single Xin, Single Yin, Single Widthin, Single Heightin)
        {
            X = Xin;
            Y = Yin;
            Width = Widthin;
            Height = Heightin;           
            ObjectTable = new System.Collections.Hashtable();
            PageItems = new List<PageItem>();
           
        }

        public void ProcessEMF(byte[] emf)
        {            
            try
            {              
                _ms = new MemoryStream(emf);
                _mf = new Metafile(_ms);                 
                _bm = new Bitmap(1,1);
                g = Graphics.FromImage(_bm);
                //XScale = Width / _mf.Width;
                //YScale = Height/ _mf.Height;
                m_delegate = new Graphics.EnumerateMetafileProc(MetafileCallback);
                g.EnumerateMetafile(_mf, new Point(0, 0), m_delegate);
            }
            finally 
            {
                if (g != null)
                    g.Dispose();
                if (_bm != null)
                    _bm.Dispose();
                if (_ms != null)
                {
                    _ms.Close();
                    _ms.Dispose();
                }
            }       
         }


        internal bool MetafileCallback(EmfPlusRecordType recordType, int flags, int dataSize, IntPtr data, PlayRecordCallback callbackData)
        {

            if (!data.Equals(IntPtr.Zero))
            {
                byte[] RecordData = new byte[dataSize];
                Marshal.Copy(data, RecordData, 0, dataSize);
                ProcessRecord(flags,recordType,RecordData);
            }
            return true;
        }


        internal void ProcessRecord(int flags, EmfPlusRecordType recordType,byte[] RecordData)
        {
            switch (recordType)
            {
                case EmfPlusRecordType.Header:                       
                    break;      
                case EmfPlusRecordType.SetPageTransform:
                    EMFSetPageTransform P = EMFSetPageTransform.getTransform(flags, RecordData);
                    break;
                case EmfPlusRecordType.Object:
                    EMFRecordObject O = EMFRecordObject.getObject(flags,RecordData);
                    if (O != null)
                    {                    
                        if (ObjectTable.Contains(O.ObjectID))
                        {
                            ObjectTable[O.ObjectID] = O;
                        }
                        else
                        {
                            ObjectTable.Add(O.ObjectID,O);
                        }
                    }                    
                    break;
                case EmfPlusRecordType.DrawLines:
                    //After each instruction we must do something, as the object table is constantly being changed...
                    //and we need to use what is currently in the table!
                    DrawLines DL = new DrawLines(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(DL.Process(flags,RecordData));                  
                    break;
                case EmfPlusRecordType.DrawString:
                    DrawString DS = new DrawString(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(DS.Process(flags, RecordData));                  
                    break;
                case EmfPlusRecordType.FillRects:
                    FillRects FR = new FillRects(X, Y, Width, Height,ObjectTable);
                    PageItems.AddRange(FR.Process(flags, RecordData));                  
                    break;
                case EmfPlusRecordType.DrawRects:
                    DrawRects DR = new DrawRects(X, Y, Width, Height,ObjectTable);
                    PageItems.AddRange(DR.Process(flags, RecordData));                   
                    break;
                case EmfPlusRecordType.FillPolygon:
                    FillPolygon FPo = new FillPolygon(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(FPo.Process(flags, RecordData));  
                    break;
                case EmfPlusRecordType.DrawEllipse:
                   DrawEllipse DE = new DrawEllipse(X, Y, Width, Height,ObjectTable);
                   PageItems.AddRange(DE.Process(flags, RecordData));                   
                    break;
                case EmfPlusRecordType.FillEllipse:
                   FillEllipse FE = new FillEllipse(X, Y, Width, Height,ObjectTable);
                   PageItems.AddRange(FE.Process(flags, RecordData));                   
                    break;
                case EmfPlusRecordType.FillPie:
                    FillPie FP = new FillPie(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(FP.Process(flags, RecordData));               
                    break;
                case EmfPlusRecordType.DrawPie:
                    DrawPie DP = new DrawPie(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(DP.Process(flags, RecordData));
                    break;
                case EmfPlusRecordType.DrawCurve:
                    DrawCurve DC = new DrawCurve(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(DC.Process(flags, RecordData));     
                    break;
                case EmfPlusRecordType.Comment:
                    Comment CM = new Comment(X, Y, Width, Height, ObjectTable);
                    PageItems.AddRange(CM.Process(flags, RecordData));
                    break;
                default:
                    break;
            }
        }

       
    }
}
    

