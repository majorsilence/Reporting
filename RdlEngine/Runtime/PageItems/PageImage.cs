using System;
using System.Diagnostics;
#if DRAWINGCOMPAT
using Majorsilence.Forms.Drawing.Imaging;
#else
using System.Drawing.Imaging;
#endif

namespace Majorsilence.Reporting.Rdl
{
	public class PageImage : PageItem, ICloneable
	{
		string name;                // name of object if constant image
		ImageFormat imf;            // type of image; png, jpeg are supported
		byte[] imageData;
		int samplesW;
		int samplesH;
		ImageRepeat repeat;
		ImageSizingEnum sizing;
		private Func<ImageFormat, int, int, byte[]> imageGenerator;

		public PageImage(ImageFormat im, byte[] image, int w, int h)
		{
#if DRAWINGCOMPAT
            Debug.Assert(im == ImageFormat.Jpeg || im == ImageFormat.Png || im == ImageFormat.Gif,
                "PageImage only supports Jpeg, Gif and Png and WMF image formats (Thanks HYNE!).");
#else
Debug.Assert(im == ImageFormat.Jpeg || im == ImageFormat.Png || im == ImageFormat.Gif || im == ImageFormat.Wmf,
				"PageImage only supports Jpeg, Gif and Png and WMF image formats (Thanks HYNE!).");
#endif

            imf = im;
			imageData = image;
			samplesW = w;
			samplesH = h;
			repeat = ImageRepeat.NoRepeat;
			sizing = ImageSizingEnum.AutoSize;
		}
		
		public PageImage(ImageFormat im, Func<ImageFormat, int, int, byte[]> imageGenerator, ImageSizingEnum sizing = ImageSizingEnum.AutoSize)
		{
#if DRAWINGCOMPAT
            Debug.Assert(im == ImageFormat.Jpeg || im == ImageFormat.Png || im == ImageFormat.Gif,
                "PageImage only supports Jpeg, Gif and Png and WMF image formats (Thanks HYNE!).");
#else
Debug.Assert(im == ImageFormat.Jpeg || im == ImageFormat.Png || im == ImageFormat.Gif || im == ImageFormat.Wmf,
				"PageImage only supports Jpeg, Gif and Png and WMF image formats (Thanks HYNE!).");
#endif

            imf = im;
			this.imageGenerator = imageGenerator;
			repeat = ImageRepeat.NoRepeat;
			this.sizing = sizing;
		}

		/// <summary>
		/// if PageImage contain static image, method return original ImageData.
		/// if PageImage created by CustomReportItem, method regenerate image for wanted resolution.
		/// </summary>
		/// <param name="wantedWidth">Desired width for image generation</param>
		/// <param name="wantedHeight">Desired height for image generation</param>
		public byte[] GetImageData(int wantedWidth, int wantedHeight)
		{
			if (imageGenerator == null)
				return imageData;
			if (imageData == null || wantedHeight != SamplesH || wantedWidth != SamplesW) {
				imageData = imageGenerator(ImgFormat, wantedWidth, wantedHeight);
				samplesW = wantedWidth;
				samplesH = wantedHeight;
			}
			return imageData;
		}

		public byte[] GetImageData() => imageData ?? GetImageData(SamplesW, samplesH);

		public ImageFormat ImgFormat
		{
			get { return imf; }
		}

		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		public ImageRepeat Repeat
		{
			get { return repeat; }
			set { repeat = value; }
		}

		public ImageSizingEnum Sizing
		{
			get { return sizing; }
			set { sizing = value; }
		}

		public int SamplesW
		{
			get { return samplesW; }
		}

		public int SamplesH
		{
			get { return samplesH; }
		}
		#region ICloneable Members

		new public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion
	}
	
	public enum ImageRepeat
	{
		Repeat,         // repeat image in both x and y directions
		NoRepeat,       // don't repeat
		RepeatX,        // repeat image in x direction
		RepeatY         // repeat image in y direction
	}
}