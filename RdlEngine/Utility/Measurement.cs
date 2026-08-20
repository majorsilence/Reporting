/*
 * This file is part of reportFU, based on the work of 
 * Kim Sheffield and the fyiReporting project. 
 *
 * Copyright (c) 2010 devFU Pty Ltd, Josh Wilson and Others (http://reportfu.org)
 * 
 * Prior Copyrights:
 * _________________________________________________________
 * |Copyright (C) 2004-2008  fyiReporting Software, LLC     |
 * |For additional information, email info@fyireporting.com |
 * |or visit the website www.fyiReporting.com.              |
 * =========================================================
 *
 * License:
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Text;
#if DRAWINGCOMPAT
using Drawing = Majorsilence.Forms.Drawing;
using Drawing2D = Majorsilence.Forms.Drawing.Drawing2D;
#else
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
#endif

namespace Majorsilence.Reporting.Rdl.Utility
{
    /// <summary>
    /// A utility class that contains additional 
    /// methods for drawing and unit conversion (points/pixels).
    /// </summary>
    public sealed class Measurement
    {
        /// <summary>
        /// A method used to obtain a rectangle from the screen coordinates supplied.
        /// </summary>
        public static System.Drawing.Rectangle RectFromPoints(System.Drawing.Point p1, System.Drawing.Point p2)
        {
            System.Drawing.Rectangle r = new System.Drawing.Rectangle();
            // set the width and x of rectangle
            if (p1.X < p2.X)
            {
                r.X = p1.X;
                r.Width = p2.X - p1.X;
            }
            else
            {
                r.X = p2.X;
                r.Width = p1.X - p2.X;
            }
            // set the height and y of rectangle
            if (p1.Y < p2.Y)
            {
                r.Y = p1.Y;
                r.Height = p2.Y - p1.Y;
            }
            else
            {
                r.Y = p2.Y;
                r.Height = p1.Y - p2.Y;
            }
            return r;
        }

        /// <summary>
        /// The constant value used to calculate the conversion of pixels into points, as a float.
        /// </summary>
        public const float POINTSIZE_F = 72.27f;
        /// <summary>
        /// The constant value used to calculate the conversion of pixels into points, as a decimal.
        /// </summary>
        public const decimal POINTSIZE_M = 72.27m;

        public const float STANDARD_DPI_X = 96f;
        public const float STANDARD_DPI_Y = 96f;

        /// <summary>
        /// A method used to convert pixels into points.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        public static float PointsFromPixels(float pixels, float dpi)
        {
            return (pixels * POINTSIZE_F) / dpi;
        }
        /// <summary>
        /// A method used to convert pixels into points.
        /// </summary>
        /// <returns>A PointF containing the point X and Y values for the pixel X and Y values that were supplied.</returns>
        public static System.Drawing.PointF PointsFromPixels(float pixelsX, float pixelsY, System.Drawing.PointF Dpi)
        {
            return new System.Drawing.PointF(PointsFromPixels(pixelsX, Dpi.X), PointsFromPixels(pixelsY, Dpi.Y));
        }
        /// <summary>
        /// A method used to convert points into pixels.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        public static int PixelsFromPoints(float points, float dpi)
        {
            int r = (int)(((double)points * dpi) / POINTSIZE_F);
            if (r == 0 && points > .0001f)
                r = 1;
            return r;
        }
        /// <summary>
        /// A method used to convert points into pixels.
        /// </summary>
        /// <returns>A PointF containing the pixel X and Y values for the point X and Y values that were supplied.</returns>
        public static System.Drawing.PointF PixelsFromPoints(float pointsX, float pointsY, System.Drawing.PointF Dpi)
        {
            return new System.Drawing.PointF(PixelsFromPoints(pointsX, Dpi.X), PixelsFromPoints(pointsY, Dpi.Y));
        }
        /// <summary>
        /// A method used to convert points into twips.
        /// </summary>
        /// <returns>An int containing the twips for the number of points that were supplied.</returns>
        public static int TwipsFromPoints(float points)
        {
            return (int)Math.Round(points * 20, 0);
        }
        /// <summary>
        /// A method used to convert pixels into twips.
        /// </summary>
        /// <returns>An int containing the twips for the number of pixels that were supplied.</returns>
        public static int TwipsFromPixels(float pixels, float dpi)
        {
            return TwipsFromPoints(PointsFromPixels(pixels, dpi));
        }

        #region Obsolete Methods
        /// <summary>
        /// A method used to convert Pixels into Points. Obsolete. Use PointsFromPixels instead.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        [System.Obsolete("This method has been deprecated. Use PointsFromPixels() instead.")]
        public static float PointsX(float pixelX, float dpi)// pixels to points
        {
            return PointsFromPixels(pixelX, dpi);
        }
        /// <summary>
        /// A method used to convert Pixels into Points. Obsolete. Use PointsFromPixels instead.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        [System.Obsolete("This method has been deprecated. Use PointsFromPixels() instead.")]
        public static float PointsY(float pixelY, float dpi)
        {
            return PointsFromPixels(pixelY, dpi);
        }

        /// <summary>
        /// A method used to convert Points into Pixels. Obsolete. Use PixelsFromPoints instead.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        [System.Obsolete("This method has been deprecated. Use PixelsFromPoints() instead.")]
        public static int PixelsX(float pointX, float dpi)// points to pixels
        {
            return PixelsFromPoints(pointX, dpi);
        }
        /// <summary>
        /// A method used to convert Points into Pixels. Obsolete. Use PixelsFromPoints instead.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        [System.Obsolete("This method has been deprecated. Use PixelsFromPoints() instead.")]
        public static int PixelsY(float pointY, float dpi)
        {
            return PixelsFromPoints(pointY, dpi);
        }
        #endregion
    }
}