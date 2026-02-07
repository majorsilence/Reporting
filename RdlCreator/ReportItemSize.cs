using System;

namespace Majorsilence.Reporting.RdlCreator
{
    public class ReportItemSize : System.Xml.Serialization.IXmlSerializable
    {
        // implicitly convert from ReportItemSize to string
        public static implicit operator string(ReportItemSize size)
        {
            return size.ToString();
        }

        // implicitly convert from string to ReportItemSize
        public static implicit operator ReportItemSize(string sizeString)
        {
            ReportItemSize size = new ReportItemSize();
            if (sizeString.EndsWith("in", StringComparison.OrdinalIgnoreCase))
            {
                size.Unit = SizeUnit.Inches;
#if NET6_0_OR_GREATER
                size.Value = float.Parse(sizeString.Replace("in", "", StringComparison.OrdinalIgnoreCase));
#else
                size.Value = float.Parse(sizeString.Replace("in", ""));
#endif
            }
            else if (sizeString.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
            {
                size.Unit = SizeUnit.Centimeters;
#if NET6_0_OR_GREATER
                size.Value = float.Parse(sizeString.Replace("cm", "", StringComparison.OrdinalIgnoreCase));
#else
                size.Value = float.Parse(sizeString.Replace("cm", ""));
#endif
            }
            else if (sizeString.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                size.Unit = SizeUnit.Points;
#if NET6_0_OR_GREATER
                size.Value = float.Parse(sizeString.Replace("pt", "", StringComparison.OrdinalIgnoreCase));
#else
                size.Value = float.Parse(sizeString.Replace("pt", ""));
#endif
            }
            else if (sizeString.EndsWith("pc", StringComparison.OrdinalIgnoreCase))
            {
                size.Unit = SizeUnit.Picas;
#if NET6_0_OR_GREATER
                size.Value = float.Parse(sizeString.Replace("pc", "", StringComparison.OrdinalIgnoreCase));
#else
                size.Value = float.Parse(sizeString.Replace("pc", ""));
#endif
            }
            else if (sizeString.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            {
                size.Unit = SizeUnit.Millimeters;
#if NET6_0_OR_GREATER
                size.Value = float.Parse(sizeString.Replace("mm", "", StringComparison.OrdinalIgnoreCase));
#else
                size.Value = float.Parse(sizeString.Replace("mm", ""));
#endif
            }
            else
            {
                // default to inches
                size.Unit = SizeUnit.Inches;
                size.Value = float.Parse(sizeString);
            }

            return size;
        }

        // this class is a strongly typed object to hold size information for report items
        // that will then output to rdl text compatible strings formats

        public float Value { get; set; }
        public SizeUnit Unit { get; set; }

        public override string ToString()
        {
            return Value.ToString("0.00") +
                   (Unit == SizeUnit.Inches ? "in" :
                       Unit == SizeUnit.Centimeters ? "cm" :
                       Unit == SizeUnit.Points ? "pt" :
                       Unit == SizeUnit.Picas ? "pc" :
                       Unit == SizeUnit.Millimeters ? "mm" : "in");
        }

        public static ReportItemSize FromInches(float inches)
        {
            return new ReportItemSize() { Value = inches, Unit = SizeUnit.Inches };
        }

        public static ReportItemSize FromCentimeters(float centimeters)
        {
            return new ReportItemSize() { Value = centimeters, Unit = SizeUnit.Centimeters };
        }

        public static ReportItemSize FromPoints(float points)
        {
            return new ReportItemSize() { Value = points, Unit = SizeUnit.Points };
        }

        public static ReportItemSize FromPicas(float picas)
        {
            return new ReportItemSize() { Value = picas, Unit = SizeUnit.Picas };
        }

        public static ReportItemSize FromMillimeters(float millimeters)
        {
            return new ReportItemSize() { Value = millimeters, Unit = SizeUnit.Millimeters };
        }

        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
        {
            return null;
        }

        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            var s = reader.ReadElementContentAsString();
            var tmp = (ReportItemSize)s;
            this.Value = tmp.Value;
            this.Unit = tmp.Unit;
        }

        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteString(this.ToString());
        }
    }
}