using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives

namespace Majorsilence.Reporting.Cri
{
    internal class PixelConversions
    {
        static public int MmXFromPixel(Graphics g, float x)
        {
            int mm = (int)(x / g.DpiX * 25.4f);	// convert to pixels

            return mm;
        }

        static public int MmYFromPixel(Graphics g, float y)
        {
            int mm = (int)(y / g.DpiY * 25.4f);	// convert to pixels

            return mm;
        }

        static public int PixelXFromMm(Graphics g, float x)
        {
            int pixels = (int)((x * g.DpiX) / 25.4f);	// convert to pixels

            return pixels;
        }

        static public int PixelYFromMm(Graphics g, float y)
        {
            int pixel = (int)((y * g.DpiY) / 25.4f);	// convert to pixels

            return pixel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="OptimalHeight"></param>
        /// <param name="OptimalWidth"></param>
        /// <returns></returns>
        static public float GetMagnification(Graphics g, int width, int height, float OptimalHeight, float OptimalWidth)
        {

            float AspectRatio = OptimalHeight / OptimalWidth;
            float r = (float)height / (float)width;
            if (r <= AspectRatio)
            {   // height is the limiting value
                return MmYFromPixel(g, height) / OptimalHeight;
            }
            else
            {   // width is the limiting value
                return MmXFromPixel(g, width) / OptimalWidth;
            }
        }

    }
}
