
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
    internal enum EMFBrushType
    {
        SolidColor = 0x00000000,
        HatchFill = 0x00000001,
        TextureFill = 0x00000002,
        PathGradient = 0x00000003,
        LinearGradient = 0x00000004
    }

    class EMFBrush : EMFRecordObject
    {
        //public EMFBrushType BrushType;
        public Draw2.Brush myBrush;
        
        internal EMFBrush()
        {
            ObjectType = EmfObjectType.brush;
        }

        internal static EMFBrush getEMFBrush(byte[] RecordData)
        {
            return ProcessBrush(RecordData);
        }

        internal static EMFBrush ProcessBrush(byte[] RecordData)
        {
            //put the Data into a stream and use a binary reader to read the data
            MemoryStream _ms = null;
            BinaryReader _br = null;
            try
            {
                _ms = new MemoryStream(RecordData);
                _br = new BinaryReader(_ms);
                UInt32 Version = _br.ReadUInt32();
                UInt32 BrushType = _br.ReadUInt32();

                switch (BrushType)
                {
                    case (UInt32)EMFBrushType.SolidColor:                       
                        return new EmfSolidBrush(_br);
                       
                    case (UInt32)EMFBrushType.HatchFill:
                        return new EmfHatchFillBrush(_br);                      
                      
                    case (UInt32)EMFBrushType.TextureFill:
                        throw new NotSupportedException("TextureFill brush Not Supported Yet!");
                        
                    case (UInt32)EMFBrushType.PathGradient:
                        throw new NotSupportedException("PathGradient brush Not Supported Yet!");
                       
                    case (UInt32)EMFBrushType.LinearGradient:
                        return new EmfLinearGradientBrush(_br);
                       
                }
                return null;
            }
            finally
            {
                if (_br != null)
                    _br.Close();
                if (_ms != null)
                    _ms.Dispose();            
            }
        }
    }   
    
    internal class EmfLinearGradientBrush : EMFBrush
    {
        public EmfLinearGradientBrush(BinaryReader _br)
        {
            UInt32 BrushFlags = _br.ReadUInt32();
            Int32 WrapMode = _br.ReadInt32();
            Single X = _br.ReadSingle();
            Single Y = _br.ReadSingle();
            Single Width = _br.ReadSingle();
            Single Height = _br.ReadSingle();
            System.Drawing.RectangleF rf = new System.Drawing.RectangleF(X,Y,Width,Height);

            byte sA, sR, sG, sB;
            sB = _br.ReadByte();
            sG = _br.ReadByte();
            sR = _br.ReadByte();
            sA = _br.ReadByte();

            byte eA, eR, eG, eB;
            eB = _br.ReadByte();
            eG = _br.ReadByte();
            eR = _br.ReadByte();
            eA = _br.ReadByte();

            _br.ReadBytes(8);            
           Draw2.Drawing2D.LinearGradientBrush tmpB = new Draw2.Drawing2D.LinearGradientBrush(rf, 
               System.Drawing.Color.FromArgb(sA, sR, sG, sB), System.Drawing.Color.FromArgb(eA, eR, eG, eB), 0f);
            tmpB.WrapMode = (Draw2.Drawing2D.WrapMode)WrapMode;
            

            bool BrushDataTransform = ((BrushFlags & 0x00000002) == 0x00000002);
            bool BrushDataPresetColors = ((BrushFlags & 0x00000002) == 0x00000004);
            bool BrushDataBlendFactorsH = ((BrushFlags & 0x00000002) == 0x00000008);
            bool BrushDataBlendFactorsV = ((BrushFlags & 0x00000002) == 0x00000010);
            bool BrushDataIsGammaCorrected = ((BrushFlags & 0x00000002) == 0x00000080);

            tmpB.GammaCorrection = BrushDataIsGammaCorrected;

            if (BrushDataTransform)
            {
                _br.ReadBytes(24); //Transform matrix (ignored for now)  
            }

            if (BrushDataPresetColors || BrushDataBlendFactorsH || BrushDataBlendFactorsV)
            {
               //there must be a blend pattern
                if (BrushDataPresetColors)
                {
                    //blend colors object
                }
                else
                {
                    if (!BrushDataBlendFactorsV)
                    {
                        //BlendFactors object on vertical (???)
                    }
                    else 
                    {
                        if (!BrushDataBlendFactorsH)
                        {
                            //Blendfactors on horizontal (???)
                        }
                        else
                        {
                            //BlendFactors on vertical and horizontal
                        }
                    }
                }
                
            }
            myBrush = tmpB;
        }
    }
    
    internal class EmfHatchFillBrush: EMFBrush
    {
    	
    	public EmfHatchFillBrush(BinaryReader _br)
    	{
    		Int32 HatchStyle = _br.ReadInt32();
    		
    		byte fA,fR,fG,fB;
            fB = _br.ReadByte();
            fG = _br.ReadByte();
            fR = _br.ReadByte();
            fA = _br.ReadByte(); 
            
            byte bA,bR,bG,bB;       
            bB = _br.ReadByte();
            bG = _br.ReadByte();
            bR = _br.ReadByte();
            bA = _br.ReadByte(); 
            
            myBrush = new Draw2.Drawing2D.HatchBrush((Draw2.Drawing2D.HatchStyle) HatchStyle,
                System.Drawing.Color.FromArgb(fA,fR,fG,fB),System.Drawing.Color.FromArgb(bA,bR,bG,bB));
            
            
    		
    	}
    }

    internal class EmfSolidBrush : EMFBrush
    {        
        public EmfSolidBrush(byte A, byte R, byte G, byte B)
        {            
            myBrush = new Draw2.SolidBrush(System.Drawing.Color.FromArgb(A, R, G, B));
        }
        public EmfSolidBrush(BinaryReader _br)
        {            
            byte A,R,G,B;       
            B = _br.ReadByte();
            G = _br.ReadByte();
            R = _br.ReadByte();
            A = _br.ReadByte();   
            myBrush = new Draw2.SolidBrush(System.Drawing.Color.FromArgb(A,R,G,B));
        }
    }
}
