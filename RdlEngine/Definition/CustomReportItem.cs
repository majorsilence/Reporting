

using System;
using System.Xml;
using System.Collections.Generic;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using System.Drawing.Imaging;
#endif
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    ///CustomReportItem describes a report item that is not natively defined in RDL.  The 
    /// RdlEngineConfig.xml file (loaded by RdlEngineConfig.cs) contains a list of the 
    /// extensions.   RdlCri.dll is a code module that contains the built-in CustomReportItems.
    /// However, the runtime dynamically loads this so RdlCrl.dll is not required for the
    /// report engine to function properly.
    ///</summary>
    [Serializable]
	internal class CustomReportItem : Rectangle
	{
        static readonly ImageFormat IMAGEFORMAT = ImageFormat.Png;
        string _Type;   // The type of the custom report item. Interpreted by a
						// report design tool or server.
                        private Expression _typeExpression=null;
		XmlNode xNode;
        System.Collections.Generic.List<CustomProperty> _Properties;
	
		internal CustomReportItem(ReportDefn r, ReportLink p, XmlNode xNode):base(r, p, xNode, false)
		{
			_Type=null;
			ReportItems ris=null;
            bool bVersion2 = true;
			this.xNode = xNode;
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "type":
						_Type = xNodeLoop.InnerText;
                        if (_Type.StartsWith("="))
                        {
                            _typeExpression = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        }

                        break;
					case "reportitems":         // Version 1 of the specification
						ris = new ReportItems(r, this, xNodeLoop);
                        bVersion2 = false;
						break;
                    case "altreportitem":       // Verstion 2 of the specification
                        ris = new ReportItems(r, this, xNodeLoop);
                        break;
                    case "customproperties":
                        _Properties = CustomProperties(xNodeLoop);
                        break;
                    case "source":
                        // HACK: support custom report items as embedded
                        break;
                    default:
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break; 
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown CustomReportItem element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}
			ReportItems = ris;
            if (bVersion2 && ris != null)
            {
			    if (ris.Items.Count != 1)
				    OwnerReport.rl.LogError(8, "Only one element is allowed within an AltReportItem.");
            }

			if (_Type == null)
				OwnerReport.rl.LogError(8, "CustomReportItem requires the Type element.");
		}

        async override internal Task FinalPass()
        {
            await base.FinalPass();       // this handles the finalpass of the AltReportItems

            // Handle the final pass for the Custom Properties
            if (_Properties != null)
            {
                foreach (CustomProperty cp in _Properties)
                {
                    await cp.Name.FinalPass();
                    await cp.Value.FinalPass();
                }
            }
            
            await GetCriType(null, null);

            // Find out whether the type is known
            ICustomReportItem cri = null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(_Type);
            }
            catch (Exception ex)
            {   // Not an error since we'll simply use the ReportItems
                OwnerReport.rl.LogError(4, string.Format("CustomReportItem load of {0} failed: {1}", 
                    _Type, ex.Message));
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }

            return;
        }

        override internal async Task Run(IPresent ip, Row row)
        {
            Report rpt = ip.Report();

            ICustomReportItem cri = null;
            try
            {
                await GetCriType(row, rpt);
                cri = RdlEngineConfig.CreateCustomReportItem(_Type);
				Type a = cri.GetType();
				Draw2.Bitmap bm = null;
                await SetProperties(rpt, row, cri);
				int width = WidthCalc(rpt, null) -
					(Style == null ? 0 :
						(await Style.EvalPaddingLeftPx(rpt, row) + await Style.EvalPaddingRightPx(rpt, row)));
				int height = RSize.PixelsFromPoints(this.HeightOrOwnerHeight) -
					(Style == null ? 0 :
						(await Style.EvalPaddingTopPx(rpt, row) + await Style.EvalPaddingBottomPx(rpt, row)));
				bm = new Draw2.Bitmap(width, height);
				cri.DrawImage(ref bm);

				MemoryStream ostrm = new MemoryStream();
				// 06122007AJM Changed to use high quality JPEG encoding
				//bm.Save(ostrm, IMAGEFORMAT);	// generate a jpeg   TODO: get png to work with pdf
				ImageCodecInfo[] info;
				info = ImageCodecInfo.GetImageEncoders();
				Draw2.Imaging.EncoderParameters encoderParameters;
				encoderParameters = new Draw2.Imaging.EncoderParameters(1);
				// 20022008 AJM GJL - Using centralised image quality
				encoderParameters.Param[0] = new Draw2.Imaging.EncoderParameter(Draw2.Imaging.Encoder.Quality, ImageQualityManager.CustomImageQuality);
				ImageCodecInfo codec = null;
				for(int i = 0; i < info.Length; i++) {
					if(info[i].FormatDescription == "PNG") {
						codec = info[i];
						break;
					}
				}
				bm.Save(ostrm, codec, encoderParameters);
                bm.Dispose();

                await ip.Image(new Image(rpt.ReportDefinition, this, xNode), row, null, ostrm);
				ostrm.Close();
            }
            catch (Exception ex)
            {
                int severity = IsCriNativeLibraryException(ex) ? 4 : 8;
                rpt.rl.LogError(severity, string.Format("Exception in CustomReportItem handling.\n{0}\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }

            return;
        }

        private async Task GetCriType(Row row, Report rpt)
        {
            if (_typeExpression != null)
            {
                //_Type = await exp.EvaluateString(OwnerReport, null);
                await _typeExpression.FinalPass();
                _Type = await _typeExpression.EvaluateString(rpt, row);
            }
        }

        override internal async Task RunPage(Pages pgs, Row row)
        {
            Report rpt = pgs.Report;

            if (await IsHidden(pgs.Report, row))
                return;

            SetPagePositionBegin(pgs);

            // Build the Chart bitmap, along with data regions
            Page p = pgs.CurrentPage;
            ICustomReportItem cri = null;
            try
            {
                await GetCriType(row, rpt);
                cri = RdlEngineConfig.CreateCustomReportItem(_Type);
                await SetProperties(pgs.Report, row, cri);
                
                Draw2.Imaging.EncoderParameters encoderParameters = new Draw2.Imaging.EncoderParameters(1);
                // 20022008 AJM GJL - Using centralised image quality
                encoderParameters.Param[0] = new Draw2.Imaging.EncoderParameter(Draw2.Imaging.Encoder.Quality, ImageQualityManager.CustomImageQuality);
                ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(x => x.FormatDescription == "PNG");

                PageImage pi = new PageImage(IMAGEFORMAT, 
                    ((format, width, height) => GenerateImage(codec, encoderParameters, width, height, cri)),
                    ImageSizingEnum.Clip);	// Create an image

                await SetPagePositionAndStyle(rpt, pi, row);

                if (pgs.CurrentPage.YOffset + pi.Y + pi.H >= pgs.BottomOfPage && !pgs.CurrentPage.IsEmpty())
                {	// force page break if it doesn't fit on the page
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                    if (this.YParents != null)
                        pi.Y = 0;
                }

                p = pgs.CurrentPage;

                p.AddObject(pi);	// Put image onto the current page
                SetPagePositionEnd(pgs, pi.Y + pi.H);
            }
            catch (Exception ex)
            {
                int severity = IsCriNativeLibraryException(ex) ? 4 : 8;
                rpt.rl.LogError(severity, string.Format("Exception in CustomReportItem handling: {0}", ex.Message));
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }
            return;
        }

        byte[] GenerateImage(ImageCodecInfo codec, Draw2.Imaging.EncoderParameters parameters, int width, int height, ICustomReportItem cri)
		{
            try
            {
                Draw2.Bitmap bm = new Draw2.Bitmap(width, height);
                cri.DrawImage(ref bm);

                MemoryStream ostrm = new MemoryStream();
                bm.Save(ostrm, codec, parameters);
                byte[] ba = ostrm.ToArray();
                ostrm.Close();
                bm.Dispose();
                return ba;
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
		}

        static bool IsCriNativeLibraryException(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                if (e is TypeInitializationException || e is System.DllNotFoundException)
                    return true;
            }
            return false;
        }

        async Task SetProperties(Report rpt, Row row, ICustomReportItem cri)
        {
            if (_Properties == null || _Properties.Count == 0)  // Any properties specified?
                return;

            System.Collections.Generic.Dictionary<string, object> dict =
                new Dictionary<string, object>(_Properties.Count, StringComparer.OrdinalIgnoreCase);
            foreach (CustomProperty cp in _Properties)
            {
                string name = await cp.Name.EvaluateString(rpt, row);
                object val = await cp.Value.Evaluate(rpt, row);
                try { dict.Add(name, val); }
                catch
                {
                    rpt.rl.LogError(4, string.Format("Property {0} has already been set.  New value {1} ignored.", name, val));
                }
            }
            cri.SetProperties(dict);
        }

		internal string Type
		{
			get { return  _Type; }
			set {  _Type = value; }
		}

        List<CustomProperty> CustomProperties(XmlNode xNode)
        {
            if (!xNode.HasChildNodes)
                return null;

            List<CustomProperty> cps = new List<CustomProperty>(xNode.ChildNodes.Count);
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                switch (xNodeLoop.Name.ToLowerInvariant())
                {
                    case "customproperty":
                        CustomProperty cp = CustomProperty(xNodeLoop);
                        if (cp != null)
                            cps.Add(cp);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown CustomProperties element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            return cps;
        }
        CustomProperty CustomProperty(XmlNode xNode)
        {
            Expression name=null;
            Expression val=null;
            CustomProperty cp = null;
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                switch (xNodeLoop.Name.ToLowerInvariant())
                {
                    case "name":
                        name = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "value":
                        val = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown CustomProperty element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            if (name == null || val == null)
                OwnerReport.rl.LogError(8, "CustomProperty requires the Name and Value element.");
            else
            {
                cp = new CustomProperty(name, val);
            }
            return cp;            
        }
	}

    class CustomProperty
    {
        Expression _Name;           // name of the property
        Expression _Value;          // value of the property
        internal CustomProperty(Expression name, Expression val)
        {
            _Name = name;
            _Value = val;
        }
        internal Expression Name
        {
            get { return _Name; }
        }

        internal Expression Value
        {
            get { return _Value; }
        }
    }
    
}
