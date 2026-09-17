


using System;
using System.Collections;
using System.Collections.Generic;
using Draw2 = Majorsilence.Forms.Drawing;
using System.IO;
using System.Xml;
using System.Globalization;

namespace Majorsilence.Reporting.Rdl
{
    internal class MapData
    {
        static Dictionary<string, MapData> _MapCache = new Dictionary<string, MapData>();               // cache data
        Dictionary<string, List<MapPolygon>> _DataPoints = new Dictionary<string, List<MapPolygon>>();  // all the data values
        List<MapObject> _MapObjects = new List<MapObject>();    // keep polygons to draw the outline (ie in case an entry isn't filled
        float _MaxX=0;         // Maximum x coordinate
        float _MaxY=0;         // Maximum y coordinate
        DateTime _TimeStamp;

        /// <summary>
        /// Creates MapData from XML 
        /// </summary>
        /// <param name="xr"></param>
        internal MapData(XmlReader xr)
        {
            xr.Read();
            if (xr.NodeType == XmlNodeType.Element && xr.Name == "MapData")
                ReadXmlMapData(xr);                // Read it
            return;
        }

        private void ReadXmlMapData(XmlReader xr)
        {
            // Read thru all the mapdata objects
            while (xr.Read())
            {
                if (!xr.IsStartElement())
                    continue;

                switch (xr.Name)
                {
                    case "Polygon":
                        ReadXmlPolygon(xr);
                        break;
                    case "Lines":
                        ReadXmlLines(xr);
                        break;
                    case "Text":
                        ReadXmlText(xr);
                        break;
                    default:                // just skip unknown elements
                        break;
                }
            }
        }

        private void ReadXmlLines(XmlReader xr)
        {
            System.Drawing.PointF[] pts = null;
            // Read thru all the Lines objects
            while (xr.Read())
            {
                if (!xr.IsStartElement())
                {
                    if (xr.NodeType == XmlNodeType.EndElement && xr.Name == "Lines")
                        break;
                    continue;
                }

                switch (xr.Name)
                {
                    case "Points":
                        pts = ReadXmlPoints(xr);
                        break;
                }
            }
            if (pts != null)
                AddMapObject(new MapLines(pts));
        }

        private System.Drawing.PointF[] ReadXmlPoints(XmlReader xr)
        {
            string spts = xr.ReadString();
            string[] pts = spts.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            System.Drawing.PointF[] pa = new System.Drawing.PointF[pts.Length / 2];
            int j=0;
            for (int i = 0; i < pa.Length; i++)
            {
                pa[i].X = Convert.ToSingle(pts[j++], NumberFormatInfo.InvariantInfo);
                pa[i].Y = Convert.ToSingle(pts[j++], NumberFormatInfo.InvariantInfo);
            }
            return pa;
        }
        private void ReadXmlPolygon(XmlReader xr)
        {
            // Read thru all the Polygon objects
            System.Drawing.PointF[] pts = null;
            string[] keys = null;
            System.Drawing.Color fill = System.Drawing.Color.Empty;
            while (xr.Read())
            {
                if (!xr.IsStartElement())
                {
                    if (xr.NodeType == XmlNodeType.EndElement && xr.Name == "Polygon")
                        break;
                    continue;
                }

                switch (xr.Name)
                {
                    case "Points":
                        pts = ReadXmlPoints(xr);
                        break;
                    case "Keys":
                        keys = ReadXmlKeys(xr);
                        break;
                    case "FillColor":
                        fill = ReadXmlColor(xr);
                        break;
                }

            }
            if (pts != null)
                AddPolygon(pts, keys, fill);
        }

        private string[] ReadXmlKeys(XmlReader xr)
        {
            string skeys = xr.ReadString();
            string[] keys = skeys.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return keys;
        }

        private System.Drawing.Color ReadXmlColor(XmlReader xr)
        {
            string sc = xr.ReadString();
            return XmlUtil.ColorFromHtml(sc, System.Drawing.Color.Empty);
        }

        private void ReadXmlText(XmlReader xr)
        {
            // Read thru all the Text elements
            string val = null;
            System.Drawing.PointF location= new System.Drawing.PointF();
            string family = "Arial";
            float fontsize = 8;
            bool bBold = false;
            bool bItalic = false;
            bool bUnderline = false;
            bool bLineThrough = false;

            string temp;
            while (xr.Read())
            {
                if (!xr.IsStartElement())
                {
                    if (xr.NodeType == XmlNodeType.EndElement && xr.Name == "Text")
                        break;
                    continue;
                }

                switch (xr.Name)
                {
                    case "Location":
                        System.Drawing.PointF[] pfa = ReadXmlPoints(xr);
                        location = pfa[0];
                        break;
                    case "Value":
                        val = xr.ReadString();
                        break;
                    case "FontFamily":
                        family = xr.ReadString();
                        break;
                    case "FontSize":
                        temp = xr.ReadString();
                        try
                        {
                            fontsize = float.Parse(temp, NumberFormatInfo.InvariantInfo);
                        }
                        catch
                        {
                            fontsize = 8;
                        }
                        break;
                    case "FontWeight":
                        temp = xr.ReadString();
                        bBold = temp.ToLower() == "bold";
                        break;
                    case "FontStyle":
                        temp = xr.ReadString();
                        bItalic = temp.ToLower() == "italic";
                        break;
                    case "TextDecoration":
                        temp = xr.ReadString();
                        switch (temp.ToLower())
                        {
                            case "underline":
                                bUnderline = true;
                                break;
                            case "linethrough":
                                bUnderline = true;
                                break;
                        }
                        break;
                }
            }
            if (val != null)
            {
                MapText mt = new MapText(val, location);
                mt.FontFamily = family;
                mt.FontSize = fontsize;
                mt.bBold = bBold;
                mt.bItalic = bItalic;
                mt.bLineThrough = bLineThrough;
                mt.bUnderline = bUnderline;
                AddMapObject(mt);
            }
        }

        /// <summary>
        /// Given a file name constructs a MapData
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        static internal MapData Create(string file)
        {

            lock (_MapCache)
            {
                DateTime filetime = File.GetLastWriteTimeUtc(file);
                MapData mp = null;
                if (_MapCache.TryGetValue(file, out mp))
                {
                    if (mp._TimeStamp.Ticks == filetime.Ticks)
                        return mp;
                    _MapCache.Remove(file);
                    mp = null;
                }

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(file, settings);
                try
                {
                    mp = new MapData(reader);
                    mp._TimeStamp = filetime;
                    _MapCache.Add(file, mp);        // add this to the cache
                }
                finally
                {
                    reader.Close();
                }
                return mp;
            }
        }
        /// <summary>
        /// this constructor for testing purposes it creates a rough 48 state US map
        /// </summary>
        internal MapData()
		{
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(338, 104), new System.Drawing.PointF(339, 111), new System.Drawing.PointF(344, 111), new System.Drawing.PointF(344, 109) }, new string[] { "de" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(312, 106), new System.Drawing.PointF(335, 102), new System.Drawing.PointF(332, 112), new System.Drawing.PointF(323, 105) }, new string[] { "md" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(355, 46), new System.Drawing.PointF(358, 29), new System.Drawing.PointF(364, 26), new System.Drawing.PointF(367, 25), new System.Drawing.PointF(371, 35), new System.Drawing.PointF(378, 44), new System.Drawing.PointF(372, 50), new System.Drawing.PointF(369, 48), new System.Drawing.PointF(368, 54), new System.Drawing.PointF(360, 64) }, new string[] { "me" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(352, 49), new System.Drawing.PointF(360, 65), new System.Drawing.PointF(359, 67), new System.Drawing.PointF(351, 67) }, new string[] { "nh" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(358, 76), new System.Drawing.PointF(360, 77), new System.Drawing.PointF(360, 80), new System.Drawing.PointF(359, 80) }, new string[] { "ri" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(348, 78), new System.Drawing.PointF(361, 77), new System.Drawing.PointF(357, 81), new System.Drawing.PointF(349, 85) }, new string[] { "ct" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(348, 72), new System.Drawing.PointF(360, 68), new System.Drawing.PointF(361, 72), new System.Drawing.PointF(364, 74), new System.Drawing.PointF(367, 73), new System.Drawing.PointF(367, 76), new System.Drawing.PointF(365, 77), new System.Drawing.PointF(362, 78), new System.Drawing.PointF(361, 74), new System.Drawing.PointF(348, 77) }, new string[] { "ma" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(343, 53), new System.Drawing.PointF(352, 50), new System.Drawing.PointF(350, 70), new System.Drawing.PointF(347, 69) }, new string[] { "vt" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(308, 86), new System.Drawing.PointF(312, 80), new System.Drawing.PointF(312, 75), new System.Drawing.PointF(324, 72), new System.Drawing.PointF(326, 68), new System.Drawing.PointF(326, 63), new System.Drawing.PointF(334, 54), new System.Drawing.PointF(341, 54), new System.Drawing.PointF(347, 71), new System.Drawing.PointF(347, 85), new System.Drawing.PointF(341, 85), new System.Drawing.PointF(336, 81) }, new string[] { "ny" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(340, 86), new System.Drawing.PointF(346, 86), new System.Drawing.PointF(347, 96), new System.Drawing.PointF(344, 104), new System.Drawing.PointF(338, 100), new System.Drawing.PointF(343, 96), new System.Drawing.PointF(340, 92) }, new string[] { "nj" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(304, 88), new System.Drawing.PointF(336, 81), new System.Drawing.PointF(339, 86), new System.Drawing.PointF(339, 91), new System.Drawing.PointF(341, 94), new System.Drawing.PointF(339, 100), new System.Drawing.PointF(306, 106) }, new string[] { "pa" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(305, 107), new System.Drawing.PointF(312, 107), new System.Drawing.PointF(315, 109), new System.Drawing.PointF(320, 106), new System.Drawing.PointF(321, 107), new System.Drawing.PointF(310, 124), new System.Drawing.PointF(302, 127), new System.Drawing.PointF(300, 125), new System.Drawing.PointF(295, 119) }, new string[] { "wv" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(295, 136), new System.Drawing.PointF(338, 126), new System.Drawing.PointF(339, 119), new System.Drawing.PointF(328, 110), new System.Drawing.PointF(323, 107), new System.Drawing.PointF(316, 115), new System.Drawing.PointF(309, 126), new System.Drawing.PointF(302, 129), new System.Drawing.PointF(295, 132) }, new string[] { "va" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(304, 135), new System.Drawing.PointF(334, 129), new System.Drawing.PointF(343, 131), new System.Drawing.PointF(345, 137), new System.Drawing.PointF(330, 152), new System.Drawing.PointF(319, 147), new System.Drawing.PointF(312, 146), new System.Drawing.PointF(305, 145), new System.Drawing.PointF(298, 147), new System.Drawing.PointF(289, 150) }, new string[] { "nc" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(296, 150), new System.Drawing.PointF(303, 147), new System.Drawing.PointF(310, 146), new System.Drawing.PointF(312, 148), new System.Drawing.PointF(319, 148), new System.Drawing.PointF(328, 154), new System.Drawing.PointF(315, 170) }, new string[] { "sc" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(280, 94), new System.Drawing.PointF(287, 93), new System.Drawing.PointF(290, 94), new System.Drawing.PointF(303, 88), new System.Drawing.PointF(305, 105), new System.Drawing.PointF(295, 119), new System.Drawing.PointF(281, 115), new System.Drawing.PointF(277, 94), new System.Drawing.PointF(283, 93) }, new string[] { "oh" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(267, 188), new System.Drawing.PointF(313, 188), new System.Drawing.PointF(331, 219), new System.Drawing.PointF(328, 230), new System.Drawing.PointF(321, 231), new System.Drawing.PointF(313, 219), new System.Drawing.PointF(305, 201), new System.Drawing.PointF(295, 192), new System.Drawing.PointF(283, 195), new System.Drawing.PointF(280, 192), new System.Drawing.PointF(270, 191) }, new string[] { "fl" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(279, 152), new System.Drawing.PointF(294, 150), new System.Drawing.PointF(313, 170), new System.Drawing.PointF(311, 186), new System.Drawing.PointF(307, 186), new System.Drawing.PointF(287, 183) }, new string[] { "ga" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(261, 154), new System.Drawing.PointF(277, 152), new System.Drawing.PointF(285, 185), new System.Drawing.PointF(267, 187), new System.Drawing.PointF(269, 192), new System.Drawing.PointF(263, 192) }, new string[] { "al" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(246, 156), new System.Drawing.PointF(260, 155), new System.Drawing.PointF(260, 191), new System.Drawing.PointF(251, 192), new System.Drawing.PointF(250, 188), new System.Drawing.PointF(237, 187), new System.Drawing.PointF(242, 175), new System.Drawing.PointF(240, 169) }, new string[] { "ms" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(250, 143), new System.Drawing.PointF(302, 135), new System.Drawing.PointF(286, 150), new System.Drawing.PointF(246, 155) }, new string[] { "tn" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(250, 140), new System.Drawing.PointF(291, 135), new System.Drawing.PointF(298, 126), new System.Drawing.PointF(295, 120), new System.Drawing.PointF(279, 116), new System.Drawing.PointF(271, 126), new System.Drawing.PointF(262, 129) }, new string[] { "ky" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(260, 96), new System.Drawing.PointF(275, 95), new System.Drawing.PointF(278, 118), new System.Drawing.PointF(259, 129) }, new string[] { "in" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(240, 91), new System.Drawing.PointF(257, 91), new System.Drawing.PointF(258, 97), new System.Drawing.PointF(258, 127), new System.Drawing.PointF(252, 136), new System.Drawing.PointF(235, 110) }, new string[] { "il" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(239, 56), new System.Drawing.PointF(251, 48), new System.Drawing.PointF(258, 53), new System.Drawing.PointF(269, 53), new System.Drawing.PointF(276, 56), new System.Drawing.PointF(280, 61), new System.Drawing.PointF(281, 77), new System.Drawing.PointF(287, 75), new System.Drawing.PointF(288, 83), new System.Drawing.PointF(284, 91), new System.Drawing.PointF(264, 94), new System.Drawing.PointF(268, 85), new System.Drawing.PointF(265, 73), new System.Drawing.PointF(269, 67), new System.Drawing.PointF(272, 60), new System.Drawing.PointF(272, 57), new System.Drawing.PointF(260, 60), new System.Drawing.PointF(256, 64) }, new string[] { "mi" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(229, 56), new System.Drawing.PointF(235, 54), new System.Drawing.PointF(253, 63), new System.Drawing.PointF(256, 70), new System.Drawing.PointF(255, 90), new System.Drawing.PointF(238, 87), new System.Drawing.PointF(235, 81), new System.Drawing.PointF(227, 72), new System.Drawing.PointF(224, 64) }, new string[] { "wi" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(219, 173), new System.Drawing.PointF(239, 174), new System.Drawing.PointF(236, 188), new System.Drawing.PointF(251, 190), new System.Drawing.PointF(251, 194), new System.Drawing.PointF(251, 200), new System.Drawing.PointF(246, 203), new System.Drawing.PointF(235, 199), new System.Drawing.PointF(232, 201), new System.Drawing.PointF(223, 199), new System.Drawing.PointF(223, 189) }, new string[] { "la" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(216, 145), new System.Drawing.PointF(244, 143), new System.Drawing.PointF(245, 149), new System.Drawing.PointF(238, 172), new System.Drawing.PointF(219, 168) }, new string[] { "ar" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(208, 110), new System.Drawing.PointF(233, 108), new System.Drawing.PointF(250, 136), new System.Drawing.PointF(247, 145), new System.Drawing.PointF(243, 141), new System.Drawing.PointF(215, 144), new System.Drawing.PointF(216, 121) }, new string[] { "mo" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(202, 84), new System.Drawing.PointF(236, 84), new System.Drawing.PointF(237, 91), new System.Drawing.PointF(240, 95), new System.Drawing.PointF(235, 107), new System.Drawing.PointF(209, 108) }, new string[] { "ia" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(200, 37), new System.Drawing.PointF(210, 35), new System.Drawing.PointF(241, 43), new System.Drawing.PointF(227, 56), new System.Drawing.PointF(225, 64), new System.Drawing.PointF(224, 72), new System.Drawing.PointF(234, 82), new System.Drawing.PointF(203, 83) }, new string[] { "mn" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(159, 142), new System.Drawing.PointF(177, 144), new System.Drawing.PointF(175, 160), new System.Drawing.PointF(190, 166), new System.Drawing.PointF(217, 170), new System.Drawing.PointF(222, 199), new System.Drawing.PointF(195, 216), new System.Drawing.PointF(194, 233), new System.Drawing.PointF(179, 227), new System.Drawing.PointF(166, 202), new System.Drawing.PointF(156, 199), new System.Drawing.PointF(150, 206), new System.Drawing.PointF(140, 199), new System.Drawing.PointF(139, 192), new System.Drawing.PointF(128, 178), new System.Drawing.PointF(153, 181) }, new string[] { "tx" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(157, 138), new System.Drawing.PointF(213, 139), new System.Drawing.PointF(215, 169), new System.Drawing.PointF(206, 165), new System.Drawing.PointF(186, 163), new System.Drawing.PointF(176, 159), new System.Drawing.PointF(176, 143), new System.Drawing.PointF(155, 142) }, new string[] { "ok" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(166, 112), new System.Drawing.PointF(209, 112), new System.Drawing.PointF(214, 122), new System.Drawing.PointF(214, 138), new System.Drawing.PointF(162, 137) }, new string[] { "ks" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(155, 86), new System.Drawing.PointF(202, 90), new System.Drawing.PointF(209, 113), new System.Drawing.PointF(165, 112), new System.Drawing.PointF(165, 103), new System.Drawing.PointF(151, 102) }, new string[] { "ne" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(156, 61), new System.Drawing.PointF(200, 62), new System.Drawing.PointF(202, 90), new System.Drawing.PointF(188, 88), new System.Drawing.PointF(154, 85) }, new string[] { "sd" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(159, 34), new System.Drawing.PointF(197, 36), new System.Drawing.PointF(200, 62), new System.Drawing.PointF(158, 60) }, new string[] { "nd" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(121, 100), new System.Drawing.PointF(165, 103), new System.Drawing.PointF(163, 137), new System.Drawing.PointF(116, 134) }, new string[] { "co" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(117, 134), new System.Drawing.PointF(157, 137), new System.Drawing.PointF(153, 180), new System.Drawing.PointF(128, 177), new System.Drawing.PointF(125, 180), new System.Drawing.PointF(114, 177), new System.Drawing.PointF(113, 181), new System.Drawing.PointF(111, 181) }, new string[] { "nm" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(83, 128), new System.Drawing.PointF(116, 134), new System.Drawing.PointF(110, 181), new System.Drawing.PointF(70, 163) }, new string[] { "az" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(92, 86), new System.Drawing.PointF(109, 88), new System.Drawing.PointF(107, 97), new System.Drawing.PointF(121, 100), new System.Drawing.PointF(115, 134), new System.Drawing.PointF(84, 127) }, new string[] { "ut" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(113, 63), new System.Drawing.PointF(155, 69), new System.Drawing.PointF(151, 102), new System.Drawing.PointF(108, 97) }, new string[] { "wy" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(91, 24), new System.Drawing.PointF(159, 35), new System.Drawing.PointF(155, 68), new System.Drawing.PointF(114, 63), new System.Drawing.PointF(112, 65), new System.Drawing.PointF(102, 66) }, new string[] { "mt" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(53, 77), new System.Drawing.PointF(91, 86), new System.Drawing.PointF(81, 133), new System.Drawing.PointF(75, 132), new System.Drawing.PointF(73, 141), new System.Drawing.PointF(48, 101) }, new string[] { "nv" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(86, 23), new System.Drawing.PointF(90, 23), new System.Drawing.PointF(89, 34), new System.Drawing.PointF(95, 46), new System.Drawing.PointF(93, 55), new System.Drawing.PointF(97, 55), new System.Drawing.PointF(101, 66), new System.Drawing.PointF(112, 65), new System.Drawing.PointF(108, 88), new System.Drawing.PointF(73, 81) }, new string[] { "id" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(29, 69), new System.Drawing.PointF(53, 76), new System.Drawing.PointF(47, 101), new System.Drawing.PointF(75, 144), new System.Drawing.PointF(76, 149), new System.Drawing.PointF(69, 162), new System.Drawing.PointF(51, 159), new System.Drawing.PointF(48, 147), new System.Drawing.PointF(42, 143), new System.Drawing.PointF(32, 136), new System.Drawing.PointF(32, 131), new System.Drawing.PointF(29, 117), new System.Drawing.PointF(28, 108), new System.Drawing.PointF(33, 105), new System.Drawing.PointF(27, 103), new System.Drawing.PointF(22, 91), new System.Drawing.PointF(22, 81) }, new string[] { "ca" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(39, 36), new System.Drawing.PointF(27, 69), new System.Drawing.PointF(72, 80), new System.Drawing.PointF(76, 62), new System.Drawing.PointF(73, 62), new System.Drawing.PointF(81, 52), new System.Drawing.PointF(79, 47), new System.Drawing.PointF(48, 44) }, new string[] { "or" });
            AddPolygon(new System.Drawing.PointF[] { new System.Drawing.PointF(53, 14), new System.Drawing.PointF(54, 24), new System.Drawing.PointF(49, 29), new System.Drawing.PointF(46, 27), new System.Drawing.PointF(48, 24), new System.Drawing.PointF(50, 21), new System.Drawing.PointF(43, 16), new System.Drawing.PointF(40, 34), new System.Drawing.PointF(45, 37), new System.Drawing.PointF(47, 43), new System.Drawing.PointF(79, 46), new System.Drawing.PointF(85, 22), new System.Drawing.PointF(53,14) }, new string[] { "wa" });
		    AddMapObject(new MapText("Map data failed to load.", new System.Drawing.PointF(0,0)));
        }

        internal void AddMapObject(MapObject mo)
        {
            _MapObjects.Add(mo);
            _MaxX = Math.Max(_MaxX, mo.MaxX);
            _MaxY = Math.Max(_MaxY, mo.MaxY);
        }

        internal void AddPolygon(System.Drawing.PointF[] polygon, string[] entries)
        {
            AddPolygon(polygon, entries, System.Drawing.Color.Empty);
        }

        internal void AddPolygon(System.Drawing.PointF[] polygon, string[] entries, System.Drawing.Color fill)
        {
            MapPolygon mp = new MapPolygon(polygon);
            mp.Fill = fill;

            AddMapObject(mp);

            if (entries == null)
                return;

            // add the MapPolygon to the entries
            foreach (string key in entries)
            {
                List<MapPolygon> pl;
                if (!_DataPoints.TryGetValue(key, out pl))
                {
                    pl = new List<MapPolygon>();
                    _DataPoints.Add(key, pl);
                }
                pl.Add(mp);
            }
        }


        internal List<MapPolygon> GetPolygon(string key)
        {
            List<MapPolygon> mp = null;
            if (_DataPoints.TryGetValue(key, out mp))
                return mp;
            return null;
        }

        internal float GetScale(float width, float height)
        {
            return Math.Min(width / _MaxX , height / _MaxY);
        }

        internal float MaxX
        {
            get { return _MaxX; }
        }

        internal float MaxY
        {
            get { return _MaxY; }
        }

        internal List<MapObject> MapObjects
        {
            get { return _MapObjects; }
        }
    }

    internal abstract class MapObject
    {
        internal abstract float MaxX { get;}
        internal abstract float MaxY { get;}
        internal abstract void Draw(Draw2.Graphics g, float scale, float offX, float offY);
    }

    internal class MapLines : MapObject
    {
        System.Drawing.PointF[] _Lines = null;
        float _MaxX=0;
        float _MaxY=0;

        internal MapLines(System.Drawing.PointF[] lines) : base()
        {
            _Lines = lines;
            foreach (System.Drawing.PointF p in lines)
            {
                if (p.X > _MaxX)
                    _MaxX = p.X;
                if (p.Y > _MaxY)
                    _MaxY = p.Y;
            }
        }

        internal System.Drawing.PointF[] Lines
        {
            get { return _Lines; }
        }

        internal override float MaxX
        {
            get { return _MaxX; }
        }
        internal override float MaxY
        {
            get { return _MaxY; }
        }
        internal override void Draw(Draw2.Graphics g, float scale, float offX, float offY)
        {
            System.Drawing.PointF[] poly = this.Lines;
            System.Drawing.PointF[] drawpoly = new System.Drawing.PointF[poly.Length];
            // make points relative to plot area --- need to scale this as well
            for (int ip = 0; ip < drawpoly.Length; ip++)
            {
                drawpoly[ip] = new System.Drawing.PointF(offX + (poly[ip].X * scale), offY + (poly[ip].Y * scale));
            }
            g.DrawPolygon(Draw2.Pens.Black, drawpoly);
        }
    }

    internal class MapPolygon : MapObject
    {
        System.Drawing.PointF[] _Polygon = null;
        float _MaxX=0;
        float _MaxY=0;
        System.Drawing.Color _Fill = System.Drawing.Color.Empty;
        internal MapPolygon(System.Drawing.PointF[] poly) : base()
        {
            System.Drawing.PointF[] fpoly;
            if (poly[0].X == poly[poly.Length - 1].X &&
                poly[0].Y == poly[poly.Length - 1].Y)
                fpoly = poly;
            else
            {   // close the polygon;
                fpoly = new System.Drawing.PointF[poly.Length + 1];
                for (int pi = 0; pi < poly.Length; pi++)
                    fpoly[pi] = poly[pi];
                fpoly[poly.Length] = fpoly[0];
            }
            _Polygon = fpoly;
            foreach (System.Drawing.PointF p in _Polygon)
            {
                if (p.X > _MaxX)
                    _MaxX = p.X;
                if (p.Y > _MaxY)
                    _MaxY = p.Y;
            }

        }
        internal System.Drawing.Color Fill
        {
            get { return _Fill; }
            set { _Fill = value; }
        }
        internal System.Drawing.PointF[] Polygon
        {
            get { return _Polygon; }
        }
        internal override float MaxX
        {
            get { return _MaxX; }
        }
        internal override float MaxY
        {
            get { return _MaxY; }
        }
        internal override void Draw(Draw2.Graphics g, float scale, float offX, float offY)
        {
            System.Drawing.PointF[] poly = this.Polygon;
            System.Drawing.PointF[] drawpoly = new System.Drawing.PointF[poly.Length];
            // make points relative to plotarea --- need to scale this as well
            for (int ip = 0; ip < drawpoly.Length; ip++)
            {
                drawpoly[ip] = new System.Drawing.PointF(offX + (poly[ip].X * scale), offY + (poly[ip].Y * scale));
            }
            
            g.DrawPolygon(Draw2.Pens.Black, drawpoly);
            if (!Fill.IsEmpty)
            {
                Draw2.Brush brush = new Draw2.SolidBrush(Fill);
                g.FillPolygon(brush, drawpoly);
                brush.Dispose();
            }
        }
    }

    internal class MapText : MapObject
    {
        string _Text;              // text value
        System.Drawing.PointF _Location;           // location of text
        public string FontFamily = "Arial";
        public float FontSize = 8;
        public bool bBold = false;
        public bool bItalic = false;
        public bool bUnderline = false;
        public bool bLineThrough = false;

        internal MapText(string val, System.Drawing.PointF xy) : base()
        {
            _Text = val;
            _Location = xy;
        }

        internal string Text
        {
            get { return _Text; }
        }

        internal System.Drawing.PointF Location
        {
            get { return _Location; }
        }

        internal override float MaxX
        {
            get { return _Location.X; }
        }
        internal override float MaxY
        {
            get { return _Location.Y; }
        }
        internal Draw2.Font GetFont(float scale)
        {
            Draw2.FontStyle fs = 0;

            if (bItalic)
                fs |= Draw2.FontStyle.Italic;
            if (bBold)
                fs |= Draw2.FontStyle.Bold;
            if (bUnderline)
                fs |= Draw2.FontStyle.Underline;
            if (bLineThrough)
                fs |= Draw2.FontStyle.Strikeout;
            float size = (float)Math.Max(FontSize * scale, .25);

            Draw2.Font font;
            try     // when fontfamily is bad then sometimes there is an exception 
            {
                font = new Draw2.Font(FontFamily, size, fs);
            }
            catch
            {
                font = new Draw2.Font("Arial", size, fs);
            }
            return font;
        }

        internal override void Draw(Draw2.Graphics g, float scale, float offX, float offY)
        {
            Draw2.Font f = this.GetFont(scale);
            g.DrawString(this.Text, f, Draw2.Brushes.Black, new System.Drawing.PointF(offX + (this.Location.X * scale), offY + (this.Location.Y * scale)));
            f.Dispose();
        }
    }
}
