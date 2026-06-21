using SkiaSharp;
using System.Collections.Generic;


namespace Majorsilence.Drawing
{
    public class Color
    {
        public static Color Black => new Color(0, 0, 0);
        public static Color White => new Color(255, 255, 255);
        public static Color Red => new Color(255, 0, 0);
        public static Color Green => new Color(0, 128, 0);
        public static Color Blue => new Color(0, 0, 255);
        public static Color Yellow => new Color(255, 255, 0);
        public static Color Cyan => new Color(0, 255, 255);
        public static Color Aqua => new Color(0, 255, 255);
        public static Color Magenta => new Color(255, 0, 255);
        public static Color Fuchsia => new Color(255, 0, 255);
        public static Color Transparent => new Color(0, 0, 0, 0);
        public static Color Orange => new Color(255, 165, 0);
        public static Color Purple => new Color(128, 0, 128);
        public static Color Brown => new Color(165, 42, 42);
        public static Color Pink => new Color(255, 192, 203);
        public static Color Lime => new Color(0, 255, 0);
        public static Color Gray => new Color(128, 128, 128);
        public static Color Grey => new Color(128, 128, 128);
        public static Color Navy => new Color(0, 0, 128);
        public static Color Olive => new Color(128, 128, 0);
        public static Color Teal => new Color(0, 128, 128);
        public static Color Silver => new Color(192, 192, 192);
        public static Color Empty => new Color(0, 0, 0, 0);
        public static Color Maroon => new Color(128, 0, 0);
        public static Color Chocolate => new Color(210, 105, 30);
        public static Color IndianRed => new Color(205, 92, 92);
        public static Color Peru => new Color(205, 133, 63);
        public static Color BurlyWood => new Color(222, 184, 135);
        public static Color AntiqueWhite => new Color(250, 235, 215);
        public static Color FloralWhite => new Color(255, 250, 240);
        public static Color Ivory => new Color(255, 255, 240);
        public static Color LightCoral => new Color(240, 128, 128);
        public static Color DarkSalmon => new Color(233, 150, 122);
        public static Color LightSalmon => new Color(255, 160, 122);
        public static Color Salmon => new Color(250, 128, 114);
        public static Color PeachPuff => new Color(255, 218, 185);
        public static Color NavajoWhite => new Color(255, 222, 173);
        public static Color Moccasin => new Color(255, 228, 181);
        public static Color PapayaWhip => new Color(255, 239, 213);
        public static Color Goldenrod => new Color(218, 165, 32);
        public static Color DarkGoldenrod => new Color(184, 134, 11);
        public static Color DarkKhaki => new Color(189, 183, 107);
        public static Color Khaki => new Color(240, 230, 140);
        public static Color Beige => new Color(245, 245, 220);
        public static Color Cornsilk => new Color(255, 248, 220);
        public static Color DeepSkyBlue => new Color(0, 191, 255);
        public static Color DodgerBlue => new Color(30, 144, 255);
        public static Color RoyalBlue => new Color(65, 105, 225);
        public static Color SteelBlue => new Color(70, 130, 180);
        public static Color Gold => new Color(255, 215, 0);
        public static Color SlateGray => new Color(112, 128, 144);
        public static Color DarkGray => new Color(169, 169, 169);
        public static Color LightGray => new Color(211, 211, 211);
        public static Color DarkSlateGray => new Color(47, 79, 79);
        public static Color DimGray => new Color(105, 105, 105);
        public static Color LightSlateGray => new Color(119, 136, 153);
        public static Color Gainsboro => new Color(220, 220, 220);
        public static Color LightBlue => new Color(173, 216, 230);
        public static Color LightCyan => new Color(224, 255, 255);
        public static Color LightGoldenrodYellow => new Color(250, 250, 210);
        public static Color LightGreen => new Color(144, 238, 144);
        public static Color LightPink => new Color(255, 182, 193);
        public static Color LightSeaGreen => new Color(32, 178, 170);
        public static Color LightSkyBlue => new Color(135, 206, 250);
        public static Color LightSteelBlue => new Color(176, 196, 222);
        public static Color LightYellow => new Color(255, 255, 224);
        public static Color CadetBlue => new Color(95, 158, 160);
        public static Color MediumTurquoise => new Color(72, 209, 204);
        public static Color Turquoise => new Color(64, 224, 208);
        public static Color Aquamarine => new Color(127, 255, 212);
        public static Color Azure => new Color(240, 255, 255);
        public static Color AliceBlue => new Color(240, 248, 255);
        public static Color MintCream => new Color(245, 255, 250);
        public static Color DarkSeaGreen => new Color(143, 188, 143);
        public static Color SeaGreen => new Color(46, 139, 87);
        public static Color ForestGreen => new Color(34, 139, 34);
        public static Color MediumSeaGreen => new Color(60, 179, 113);
        public static Color YellowGreen => new Color(154, 205, 50);
        public static Color OliveDrab => new Color(107, 142, 35);
        public static Color GreenYellow => new Color(173, 255, 47);
        public static Color Chartreuse => new Color(127, 255, 0);
        public static Color LawnGreen => new Color(124, 252, 0);
        public static Color PaleGreen => new Color(152, 251, 152);
        public static Color SpringGreen => new Color(0, 255, 127);
        public static Color MediumSpringGreen => new Color(0, 250, 154);
        public static Color MediumPurple => new Color(147, 112, 219);
        public static Color DarkOrchid => new Color(153, 50, 204);
        public static Color DarkViolet => new Color(148, 0, 211);
        public static Color BlueViolet => new Color(138, 43, 226);
        public static Color MediumOrchid => new Color(186, 85, 211);
        public static Color Violet => new Color(238, 130, 238);
        public static Color CornflowerBlue => new Color(100, 149, 237);
        public static Color Lavender => new Color(230, 230, 250);
        public static Color GhostWhite => new Color(248, 248, 255);
        public static Color PaleGoldenrod => new Color(238, 232, 170);
        public static Color LemonChiffon => new Color(255, 250, 205);
        public static Color Orchid => new Color(218, 112, 214);
        public static Color Plum => new Color(221, 160, 221);
        public static Color LavenderBlush => new Color(255, 240, 245);
        public static Color Linen => new Color(250, 240, 230);
        public static Color PaleTurquoise => new Color(175, 238, 238);
        public static Color OldLace => new Color(253, 245, 230);
        public static Color Bisque => new Color(255, 228, 196);
        public static Color Crimson => new Color(220, 20, 60);
        public static Color FireBrick => new Color(178, 34, 34);
        public static Color DarkRed => new Color(139, 0, 0);
        public static Color HotPink => new Color(255, 105, 180);
        public static Color DeepPink => new Color(255, 20, 147);
        public static Color MediumVioletRed => new Color(199, 21, 133);
        public static Color PaleVioletRed => new Color(219, 112, 147);
        public static Color Coral => new Color(255, 127, 80);
        public static Color Tomato => new Color(255, 99, 71);
        public static Color OrangeRed => new Color(255, 69, 0);
        public static Color DarkOrange => new Color(255, 140, 0);
        public static Color Wheat => new Color(245, 222, 179);
        public static Color SandyBrown => new Color(244, 164, 96);
        public static Color Tan => new Color(210, 180, 140);
        public static Color RosyBrown => new Color(188, 143, 143);
        public static Color SaddleBrown => new Color(139, 69, 19);
        public static Color Sienna => new Color(160, 82, 45);
        public static Color Snow => new Color(255, 250, 250);
        public static Color SeaShell => new Color(255, 245, 238);
        public static Color HoneyDew => new Color(240, 255, 240);
        public static Color Thistle => new Color(216, 191, 216);
        public static Color Indigo => new Color(75, 0, 130);
        public static Color SlateBlue => new Color(106, 90, 205);
        public static Color MediumSlateBlue => new Color(123, 104, 238);
        public static Color DarkSlateBlue => new Color(72, 61, 139);
        public static Color MidnightBlue => new Color(25, 25, 112);
        public static Color DarkBlue => new Color(0, 0, 139);
        public static Color MediumBlue => new Color(0, 0, 205);
        public static Color PowderBlue => new Color(176, 224, 230);
        public static Color SkyBlue => new Color(135, 206, 235);
        public static Color DarkCyan => new Color(0, 139, 139);
        public static Color DarkMagenta => new Color(139, 0, 139);
        public static Color DarkGreen => new Color(0, 100, 0);
        public static Color WhiteSmoke => new Color(245, 245, 245);
        public static Color MistyRose => new Color(255, 228, 225);
        public static Color PapayaWhip2 => new Color(255, 239, 213);

        private static readonly Dictionary<string, Color> s_namedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["Black"] = new Color(0, 0, 0),
            ["White"] = new Color(255, 255, 255),
            ["Red"] = new Color(255, 0, 0),
            ["Green"] = new Color(0, 128, 0),
            ["Blue"] = new Color(0, 0, 255),
            ["Yellow"] = new Color(255, 255, 0),
            ["Cyan"] = new Color(0, 255, 255),
            ["Aqua"] = new Color(0, 255, 255),
            ["Magenta"] = new Color(255, 0, 255),
            ["Fuchsia"] = new Color(255, 0, 255),
            ["Transparent"] = new Color(0, 0, 0, 0),
            ["Orange"] = new Color(255, 165, 0),
            ["Purple"] = new Color(128, 0, 128),
            ["Brown"] = new Color(165, 42, 42),
            ["Pink"] = new Color(255, 192, 203),
            ["Lime"] = new Color(0, 255, 0),
            ["Gray"] = new Color(128, 128, 128),
            ["Grey"] = new Color(128, 128, 128),
            ["Navy"] = new Color(0, 0, 128),
            ["Olive"] = new Color(128, 128, 0),
            ["Teal"] = new Color(0, 128, 128),
            ["Silver"] = new Color(192, 192, 192),
            ["Empty"] = new Color(0, 0, 0, 0),
            ["Maroon"] = new Color(128, 0, 0),
            ["Chocolate"] = new Color(210, 105, 30),
            ["IndianRed"] = new Color(205, 92, 92),
            ["Peru"] = new Color(205, 133, 63),
            ["BurlyWood"] = new Color(222, 184, 135),
            ["AntiqueWhite"] = new Color(250, 235, 215),
            ["FloralWhite"] = new Color(255, 250, 240),
            ["Ivory"] = new Color(255, 255, 240),
            ["LightCoral"] = new Color(240, 128, 128),
            ["DarkSalmon"] = new Color(233, 150, 122),
            ["LightSalmon"] = new Color(255, 160, 122),
            ["Salmon"] = new Color(250, 128, 114),
            ["PeachPuff"] = new Color(255, 218, 185),
            ["NavajoWhite"] = new Color(255, 222, 173),
            ["Moccasin"] = new Color(255, 228, 181),
            ["PapayaWhip"] = new Color(255, 239, 213),
            ["Goldenrod"] = new Color(218, 165, 32),
            ["DarkGoldenrod"] = new Color(184, 134, 11),
            ["DarkKhaki"] = new Color(189, 183, 107),
            ["Khaki"] = new Color(240, 230, 140),
            ["Beige"] = new Color(245, 245, 220),
            ["Cornsilk"] = new Color(255, 248, 220),
            ["DeepSkyBlue"] = new Color(0, 191, 255),
            ["DodgerBlue"] = new Color(30, 144, 255),
            ["RoyalBlue"] = new Color(65, 105, 225),
            ["SteelBlue"] = new Color(70, 130, 180),
            ["Gold"] = new Color(255, 215, 0),
            ["SlateGray"] = new Color(112, 128, 144),
            ["DarkGray"] = new Color(169, 169, 169),
            ["LightGray"] = new Color(211, 211, 211),
            ["DarkSlateGray"] = new Color(47, 79, 79),
            ["DimGray"] = new Color(105, 105, 105),
            ["LightSlateGray"] = new Color(119, 136, 153),
            ["Gainsboro"] = new Color(220, 220, 220),
            ["LightBlue"] = new Color(173, 216, 230),
            ["LightCyan"] = new Color(224, 255, 255),
            ["LightGoldenrodYellow"] = new Color(250, 250, 210),
            ["LightGreen"] = new Color(144, 238, 144),
            ["LightPink"] = new Color(255, 182, 193),
            ["LightSeaGreen"] = new Color(32, 178, 170),
            ["LightSkyBlue"] = new Color(135, 206, 250),
            ["LightSteelBlue"] = new Color(176, 196, 222),
            ["LightYellow"] = new Color(255, 255, 224),
            ["CadetBlue"] = new Color(95, 158, 160),
            ["MediumTurquoise"] = new Color(72, 209, 204),
            ["Turquoise"] = new Color(64, 224, 208),
            ["Aquamarine"] = new Color(127, 255, 212),
            ["Azure"] = new Color(240, 255, 255),
            ["AliceBlue"] = new Color(240, 248, 255),
            ["MintCream"] = new Color(245, 255, 250),
            ["DarkSeaGreen"] = new Color(143, 188, 143),
            ["SeaGreen"] = new Color(46, 139, 87),
            ["ForestGreen"] = new Color(34, 139, 34),
            ["MediumSeaGreen"] = new Color(60, 179, 113),
            ["YellowGreen"] = new Color(154, 205, 50),
            ["OliveDrab"] = new Color(107, 142, 35),
            ["GreenYellow"] = new Color(173, 255, 47),
            ["Chartreuse"] = new Color(127, 255, 0),
            ["LawnGreen"] = new Color(124, 252, 0),
            ["PaleGreen"] = new Color(152, 251, 152),
            ["SpringGreen"] = new Color(0, 255, 127),
            ["MediumSpringGreen"] = new Color(0, 250, 154),
            ["MediumPurple"] = new Color(147, 112, 219),
            ["DarkOrchid"] = new Color(153, 50, 204),
            ["DarkViolet"] = new Color(148, 0, 211),
            ["BlueViolet"] = new Color(138, 43, 226),
            ["MediumOrchid"] = new Color(186, 85, 211),
            ["Violet"] = new Color(238, 130, 238),
            ["CornflowerBlue"] = new Color(100, 149, 237),
            ["Lavender"] = new Color(230, 230, 250),
            ["GhostWhite"] = new Color(248, 248, 255),
            ["PaleGoldenrod"] = new Color(238, 232, 170),
            ["LemonChiffon"] = new Color(255, 250, 205),
            ["Orchid"] = new Color(218, 112, 214),
            ["Plum"] = new Color(221, 160, 221),
            ["LavenderBlush"] = new Color(255, 240, 245),
            ["Linen"] = new Color(250, 240, 230),
            ["PaleTurquoise"] = new Color(175, 238, 238),
            ["OldLace"] = new Color(253, 245, 230),
            ["Bisque"] = new Color(255, 228, 196),
            ["Crimson"] = new Color(220, 20, 60),
            ["FireBrick"] = new Color(178, 34, 34),
            ["DarkRed"] = new Color(139, 0, 0),
            ["HotPink"] = new Color(255, 105, 180),
            ["DeepPink"] = new Color(255, 20, 147),
            ["MediumVioletRed"] = new Color(199, 21, 133),
            ["PaleVioletRed"] = new Color(219, 112, 147),
            ["Coral"] = new Color(255, 127, 80),
            ["Tomato"] = new Color(255, 99, 71),
            ["OrangeRed"] = new Color(255, 69, 0),
            ["DarkOrange"] = new Color(255, 140, 0),
            ["Wheat"] = new Color(245, 222, 179),
            ["SandyBrown"] = new Color(244, 164, 96),
            ["Tan"] = new Color(210, 180, 140),
            ["RosyBrown"] = new Color(188, 143, 143),
            ["SaddleBrown"] = new Color(139, 69, 19),
            ["Sienna"] = new Color(160, 82, 45),
            ["Snow"] = new Color(255, 250, 250),
            ["SeaShell"] = new Color(255, 245, 238),
            ["HoneyDew"] = new Color(240, 255, 240),
            ["Thistle"] = new Color(216, 191, 216),
            ["Indigo"] = new Color(75, 0, 130),
            ["SlateBlue"] = new Color(106, 90, 205),
            ["MediumSlateBlue"] = new Color(123, 104, 238),
            ["DarkSlateBlue"] = new Color(72, 61, 139),
            ["MidnightBlue"] = new Color(25, 25, 112),
            ["DarkBlue"] = new Color(0, 0, 139),
            ["MediumBlue"] = new Color(0, 0, 205),
            ["PowderBlue"] = new Color(176, 224, 230),
            ["SkyBlue"] = new Color(135, 206, 235),
            ["DarkCyan"] = new Color(0, 139, 139),
            ["DarkMagenta"] = new Color(139, 0, 139),
            ["DarkGreen"] = new Color(0, 100, 0),
            ["WhiteSmoke"] = new Color(245, 245, 245),
            ["MistyRose"] = new Color(255, 228, 225),
            ["PapayaWhip2"] = new Color(255, 239, 213),
        };

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        private string _name;
        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;
                _name = GetColorName(ToSkColor());
                return _name;
            }
        }

        public bool IsEmpty => A == 0 && R == 0 && G == 0 && B == 0;

        public Color(int r, int g, int b, int a = 255)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color FromRgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }

        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }

        public static Color FromArgb(int argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return new Color(r, g, b, a);
        }

        public static Color FromArgb(int alpha, Color baseColor)
        {
            return new Color(baseColor.R, baseColor.G, baseColor.B, (byte)alpha);
        }

        public int ToArgb()
        {
            return (A << 24) | (R << 16) | (G << 8) | B;
        }

        public static Color FromName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Empty;

            if (s_namedColors.TryGetValue(name, out Color color))
                return color;

            if (name.StartsWith("#"))
                return ColorTranslator.FromHtml(name);

            return Empty;
        }

        public SKColor ToSkColor()
        {
            return new SKColor(R, G, B, A);
        }

        private static string GetColorName(SKColor color)
        {
            foreach (var kvp in s_namedColors)
            {
                Color c = kvp.Value;
                if (c.R == color.Red && c.G == color.Green && c.B == color.Blue && c.A == color.Alpha)
                    return kvp.Key;
            }
            return string.Empty;
        }

        public static bool operator ==(Color left, Color right)
        {
            if (left is null) return right is null;
            if (right is null) return false;
            return left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
        }

        public static bool operator !=(Color left, Color right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Color other && this == other;
        }

        public override int GetHashCode()
        {
            return ToArgb();
        }

        public override string ToString()
        {
            return $"Color [A={A}, R={R}, G={G}, B={B}]";
        }

#if !DRAWINGCOMPAT
        public static implicit operator System.Drawing.Color(Color p)
        {
            return System.Drawing.Color.FromArgb(p.A, p.R, p.G, p.B);
        }
#endif
    }
}
