
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if DRAWINGCOMPAT
using Drawing = Majorsilence.Forms.Drawing;
#else
using Drawing = System.Drawing;
#endif

namespace Majorsilence.Reporting.Rdl
{
     internal class EMFSetPageTransform
    {
        internal Drawing.GraphicsUnit PageUnit;
        internal bool postMultiplyTransform;
        internal Single PageScale;
        internal static EMFSetPageTransform getTransform(int flags, byte[] RecordData)
        {
            return new EMFSetPageTransform(flags, RecordData);
        }

        internal EMFSetPageTransform(int flags, byte[] RecordData)
        {
            MemoryStream _fs = null;
            BinaryReader _fr = null;
            try
            {
                _fs = new MemoryStream(BitConverter.GetBytes(flags));
                _fr = new BinaryReader(_fs);

                //PageUnit...
                UInt16 PageU = _fr.ReadByte();
                PageUnit = (Drawing.GraphicsUnit)PageU;

                UInt16 RealFlags = _fr.ReadByte();
                //XXXXXAXX - if A = 1 the transform matrix is post-multiplied else pre-multiplied...
                //01234567

                //PJR20220801 - (int) Math.Pow(2,5) !=32 it's 31!!! Argh!!!
                postMultiplyTransform = ((RealFlags >> 5) & 1) == 1;//((RealFlags & (UInt16)Math.Pow(2, 5)) == Math.Pow(2, 5));

                PageScale = BitConverter.ToSingle(RecordData, 0);
                
            }
            finally
           {
               if (_fr != null)
                   _fr.Close();
               if (_fs != null)
                   _fs.Dispose();
               
           }
        }
    }
}
