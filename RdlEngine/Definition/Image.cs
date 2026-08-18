

using System;
using System.Xml;
using System.IO;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using Majorsilence.Reporting.Rdl.Utility;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Represents an image.  Source of image can from database, external or embedded. 
    ///</summary>
    [Serializable]
    internal class Image : ReportItem
    {
        ImageSourceEnum _ImageSource;   // Identifies the source of the image:
        Expression _Value;      // See Source. Expected datatype is string or
                                // binary, depending on Source. If the Value is
                                // null, no image is displayed.
        Expression _MIMEType;   // (string) An expression, the value of which is the
                                //	MIMEType for the image.
                                //	Valid values are: image/bmp, image/jpeg,
                                //	image/gif, image/png, image/x-png
                                // Required if Source = Database. Ignored otherwise.
        ImageSizingEnum _Sizing;    // Defines the behavior if the image does not fit within the specified size.

        bool _ConstantImage;	// true if Image is a constant at runtime

        string _EmbeddedImageData; // only for RenderHtml and embeddedImage. we need the embedded image code for html.

        private string imageUrl;         

        /// <summary>
        /// Only gets set for Images which contain urls rather than coming from the database etc..
        /// </summary>
        public string ImageUrl
        {
            get { return imageUrl; }
            private set { imageUrl = value; }
        }

        internal Image(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode)
        {
            _ImageSource = ImageSourceEnum.Unknown;
            _Value = null;
            _MIMEType = null;
            _Sizing = ImageSizingEnum.AutoSize;
            _ConstantImage = false;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name.ToLowerInvariant())
                {
                    case "source":
                        _ImageSource = Majorsilence.Reporting.Rdl.ImageSource.GetStyle(xNodeLoop.InnerText);
                        break;
                    case "value":
                        _Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    case "mimetype":
                        _MIMEType = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "sizing":
                        _Sizing = ImageSizing.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        if (ReportItemElement(xNodeLoop))   // try at ReportItem level
                            break;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Image element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            if (_ImageSource == ImageSourceEnum.Unknown)
                OwnerReport.rl.LogError(8, "Image requires a Source element.");
            if (_Value == null)
                OwnerReport.rl.LogError(8, "Image requires the Value element.");
        }

        // Handle parsing of function in final pass
        async override internal Task FinalPass()
        {
            await base.FinalPass();

            await _Value.FinalPass();
            if (_MIMEType != null)
                await _MIMEType.FinalPass();

            _ConstantImage = await this.IsConstant();

            return;
        }

        // Returns true if the image and style remain constant at runtime
        async Task<bool> IsConstant()
        {

            if (await _Value.IsConstant())
            {
                if (_MIMEType == null || await _MIMEType.IsConstant())
                {
                    //					if (this.Style == null || this.Style.ConstantStyle)
                    //						return true;
                    return true;    // ok if style changes
                }
            }
            return false;
        }

        async override internal Task Run(IPresent ip, Row row)
        {
            await base.Run(ip, row);

            string mtype = null;
            Stream strm = null;
            try
            {
                (strm, mtype) = await GetImageStream(ip.Report(), row);

                await ip.Image(this, row, mtype, strm);
            }
            catch
            {
                // image failed to load;  continue processing
            }
            finally
            {
                if (strm != null)
                    strm.Close();
            }
            return;
        }

        override internal async Task RunPage(Pages pgs, Row row)
        {
            Report r = pgs.Report;
            bool bHidden = await IsHidden(r, row);

            WorkClass wc = GetWC(r);
            string mtype = null;
            Stream strm = null;
            Draw2.Image im = null;

            SetPagePositionBegin(pgs);
            if (bHidden)
            {
                PageImage pi = new PageImage(ImageFormat.Jpeg, (byte[])null, 0, 0);
                await this.SetPagePositionAndStyle(r, pi, row);
                SetPagePositionEnd(pgs, pi.Y + pi.H);
                return;
            }

            if (wc.PgImage != null)
            {   // have we already generated this one
                // reuse most of the work; only position will likely change
                PageImage pi = new PageImage(wc.PgImage.ImgFormat, wc.PgImage.GetImageData(), wc.PgImage.SamplesW, wc.PgImage.SamplesH);
                pi.Name = wc.PgImage.Name;              // this is name it will be shared under
                pi.Sizing = this._Sizing;
                await this.SetPagePositionAndStyle(r, pi, row);
                pgs.CurrentPage.AddObject(pi);
                SetPagePositionEnd(pgs, pi.Y + pi.H);
                return;
            }

            try
            {
                (strm, mtype) = await GetImageStream(r, row);
                if (strm == null)
                {
                    r.rl.LogError(4, string.Format("Unable to load image {0}.", this.Name.Nm));
                    return;
                }
                im = Draw2.Image.FromStream(strm);
                int height = im.Height;
                int width = im.Width;
                MemoryStream ostrm = new MemoryStream();
                ImageFormat imf;
                
                switch (mtype.ToLower())
                {
                    case "image/jpeg":
                        imf = ImageFormat.Jpeg;
                        im.Save(ostrm, imf);
                        break;
                    case "image/png":
                        imf = ImageFormat.Png;
                        im.Save(ostrm, imf);
                        break;
                    default: // from old code where all images convert to jpeg, i don't know why. May be need delete it and add all support formats.
                        imf = ImageFormat.Jpeg;
                        ImageCodecInfo[] info;
                        info = ImageCodecInfo.GetImageEncoders();
                        Draw2.Imaging.EncoderParameters encoderParameters;
                        encoderParameters = new Draw2.Imaging.EncoderParameters(1);
                        encoderParameters.Param[0] = new Draw2.Imaging.EncoderParameter(Draw2.Imaging.Encoder.Quality, ImageQualityManager.EmbeddedImageQuality);
                        ImageCodecInfo codec = null;
                        for (int i = 0; i < info.Length; i++)
                        {
                            if (info[i].FormatDescription == "JPEG")
                            {
                                codec = info[i];
                                break;
                            }
                        }
                        im.Save(ostrm, codec, encoderParameters);
                        break;
                }

                byte[] ba = ostrm.ToArray();
                ostrm.Close();
                PageImage pi = new PageImage(imf, ba, width, height);
                pi.Sizing = this._Sizing;
                await this.SetPagePositionAndStyle(r, pi, row);

                pgs.CurrentPage.AddObject(pi);
                if (_ConstantImage)
                {
                    wc.PgImage = pi;
                    // create unique name; PDF generation uses this to optimize the saving of the image only once
                    pi.Name = "pi" + Interlocked.Increment(ref Parser.Counter).ToString();  // create unique name
                }

                SetPagePositionEnd(pgs, pi.Y + pi.H);
            }
            catch (Exception e)
            {
                // image failed to load, continue processing
                r.rl.LogError(4, "Image load failed.  " + e.Message);
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }
            return;
        }

        async Task<(Stream stream, string mtype)> GetImageStream(Report rpt, Row row)
        {
            string mtype = null;
            Stream strm = null;
            try
            {
                switch (this.ImageSource)
                {
                    case ImageSourceEnum.Database:
                        if (_MIMEType == null)
                            return (null, mtype);
                        mtype = await _MIMEType.EvaluateString(rpt, row);
                        object o = await _Value.Evaluate(rpt, row);
                        strm = new MemoryStream((byte[])o);
                        break;
                    case ImageSourceEnum.Embedded:
                        string name = await _Value.EvaluateString(rpt, row);
                        EmbeddedImage ei = (EmbeddedImage)OwnerReport.LUEmbeddedImages[name];
                        mtype = ei.MIMEType;
                        byte[] ba = Convert.FromBase64String(ei.ImageData);
                        _EmbeddedImageData = ei.ImageData; // we need this for html embedded image
                        strm = new MemoryStream(ba);
                        break;
                    case ImageSourceEnum.External:
                        string fname = this.ImageUrl = await _Value.EvaluateString(rpt, row);
                        mtype = GetMimeType(fname);
                        if (fname.StartsWith("http:") ||
                            fname.StartsWith("file:") ||
                            fname.StartsWith("https:"))
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                client.AddMajorsilenceReportingUserAgent();
                                HttpResponseMessage response = await client.GetAsync(fname);
                                response.EnsureSuccessStatusCode();
                                strm = await response.Content.ReadAsStreamAsync();
                            }
                        }
                        else
                            strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);
                        break;
                    default:
                        return (null, mtype);
                }
            }
            catch (Exception e)
            {
                if (strm != null)
                {
                    strm.Close();
                    strm = null;
                }
                rpt.rl.LogError(4, string.Format("Unable to load image. {0}", e.Message));
            }

            return (strm, mtype);
        }

        internal ImageSourceEnum ImageSource
        {
            get { return _ImageSource; }
            set { _ImageSource = value; }
        }

        internal Expression Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        internal Expression MIMEType
        {
            get { return _MIMEType; }
            set { _MIMEType = value; }
        }

        internal ImageSizingEnum Sizing
        {
            get { return _Sizing; }
            set { _Sizing = value; }
        }

        internal bool ConstantImage
        {
            get { return _ConstantImage; }
        }

        internal string EmbeddedImageData
        {
            get { return _EmbeddedImageData; }
        }


        static internal string GetMimeType(string file)
        {
            String fileExt;

            int startPos = file.LastIndexOf(".") + 1;

            fileExt = file.Substring(startPos).ToLower();

            switch (fileExt)
            {
                case "bmp":
                    return "image/bmp";
                case "jpeg":
                case "jpe":
                case "jpg":
                case "jfif":
                    return "image/jpeg";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "tif":
                case "tiff":
                    return "image/tiff";
                default:
                    return null;
            }
        }

        private WorkClass GetWC(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveImageWC(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

        class WorkClass
        {
            internal PageImage PgImage; // When ConstantImage is true this will save the PageImage for reuse
            internal WorkClass()
            {
                PgImage = null;
            }
        }

    }
}
