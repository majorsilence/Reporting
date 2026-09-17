

using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Xsl;
using System.Text;
using System.IO;
using System.Drawing;   // Color, and its ColorTranslator: Majorsilence.Forms.Drawing deliberately
                        // does not reimplement the System.Drawing.Primitives value types
using ColorTranslator = Majorsilence.Forms.Drawing.ColorTranslator;
using System.Reflection;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Some utility classes consisting entirely of static routines.
	///</summary>
	public sealed class XmlUtil
	{
		static internal bool Boolean(string tf, ReportLog rl)
		{
			string low_tf = tf.ToLower();
			if (low_tf.CompareTo("true") == 0)
				return true;
			if (low_tf.CompareTo("false") == 0)
				return false;
			rl.LogError(4, "Unknown True/False value '" + tf + "'.  False assumed.");
			return false;
		}
		
		static internal Color ColorFromHtml(string sc, Color dc)
		{
			return ColorFromHtml(sc, dc, null);
		}

		static internal Color ColorFromHtml(string sc, Color dc, Report rpt)
		{
			Color c;
			try 
			{
				c = ColorTranslator.FromHtml(sc);
			}
			catch 
			{
				c = dc;
				if (rpt != null)
					rpt.rl.LogError(4, string.Format("'{0}' is an invalid HTML color.", sc));
			}

			return c;
		}

		static internal int Integer(string i)
		{
			return Convert.ToInt32(i);
		}

		/// <summary>
		/// Takes an arbritrary string and returns a string that can be embedded in an
		/// XML element.  For example, '&lt;' is changed to '&amp;lt;'
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		static public string XmlAnsi(string s)
		{
			StringBuilder rs = new StringBuilder(s.Length);

			foreach (char c in s)
			{
				if (c == '<')
					rs.Append("&lt;");
				else if (c == '&')
					rs.Append("&amp;");
				else if ((int) c <= 127)	// in ANSI range
					rs.Append(c);
				else
					rs.Append("&#" + ((int) c).ToString() + ";");
			}

			return rs.ToString();
		}
        /// <summary>
        /// Takes an arbritrary string and returns a string that can be handles unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static public string HtmlAnsi(string s)
        {
            StringBuilder rs = new StringBuilder(s.Length);

            foreach (char c in s)
            {
                if ((int)c <= 127)	// in ANSI range
                    rs.Append(c);
                else
                    rs.Append("&#" + ((int)c).ToString() + ";");
            }

            return rs.ToString();
        }

		static internal void XslTrans(string xslFile, string inXml, Stream outResult)
		{
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(inXml);

            XslCompiledTransform xslt = new XslCompiledTransform();

			//Load the stylesheet.
			xslt.Load(xslFile);

			xslt.Transform(xDoc,null,outResult);
           
			return;
		}

		static internal string EscapeXmlAttribute(string s)
		{
			string result;

			result = s.Replace("'", "&#39;");

			return result;
		}
		/// <summary>
		/// Loads assembly from file; tries up to 3 time; load with name, load from BaseDirectory,
		/// and load from BaseDirectory concatenated with Relative directory.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		[RequiresDynamicCode("Loading assemblies at runtime is not supported under Native AOT")]
		static internal Assembly AssemblyLoadFrom(string s)
		{
			Assembly ra=null;
			try
			{	// try 1) loading just from name
                if (System.IO.File.Exists(s))
                {
                    ra = Assembly.LoadFrom(s);
                }
                else
                {
                    string path = System.IO.Path.Combine(RdlEngineConfig.DirectoryLoadedFrom, s);
                    ra = Assembly.LoadFrom(path);
                }
			}
			catch
			{	// try 2) loading from the various directories available
                string d0 = RdlEngineConfig.DirectoryLoadedFrom;
                string d1 = AppDomain.CurrentDomain.BaseDirectory;
                string d2 = AppDomain.CurrentDomain.RelativeSearchPath;
                if (d2 == null || d2 == string.Empty)
                    ra = AssemblyLoadFromPvt(Path.GetFileName(s), d0, d1);
                else
                    ra = AssemblyLoadFromPvt(Path.GetFileName(s), d0, d1, d2);
			}

			return ra;
		}

        [RequiresDynamicCode("Loading assemblies at runtime is not supported under Native AOT")]
        static Assembly AssemblyLoadFromPvt(string file, params string[] dir)
        {
            Assembly ra = null;
            // EBN: check if the assembly is already loaded
            foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (loadedAssembly.ManifestModule.Name == file)
                {
                    ra = loadedAssembly;
                    return ra;
                }
            }


            for (int i = 0; i < dir.Length; i++)
            {
                if (dir[i] == null)
                    continue;

                string f = dir[i] + file;
                try
                {
                    ra = Assembly.LoadFile(f);
                    if (ra != null)             // don't really need this as call will throw exception when it fails
                        break;
                }
                catch 
                {
                    if (i + 1 == dir.Length)
                    {  // on last try just plain load of the file
                        ra = Assembly.Load(file);
                    }
                }
            }
            return ra;
        }

        [RequiresUnreferencedCode("Type members may be removed by the trimmer")]
        static internal MethodInfo GetMethod(Type t, string method, Type[] argTypes)
        {
            if (t == null || method == null)
                return null;

            MethodInfo mInfo = t.GetMethod(method,
               BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static, null,       // TODO: use Laxbinder class
               argTypes, null);
            if (mInfo == null)
                mInfo = t.GetMethod(method, argTypes);  // be less specific and try again (Code VB functions don't always get caught?)
            if (mInfo == null)
            {
                // Try to find method in base classes --- fix thanks to jonh
                Type b = t.BaseType;
                while (b != null)
                {
                    //                    mInfo = b.GetMethod(method, argTypes);
                    mInfo = b.GetMethod(method,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                        null, argTypes, null);
                    if (mInfo != null)
                        break;
                    b = b.BaseType;
                }
            }
            return mInfo;
        }

        static internal Type GetTypeFromTypeCode(TypeCode tc)
		{
			return tc switch
			{
				TypeCode.Boolean  => typeof(bool),
				TypeCode.Byte     => typeof(byte),
				TypeCode.Char     => typeof(char),
				TypeCode.DateTime => typeof(DateTime),
				TypeCode.Decimal  => typeof(decimal),
				TypeCode.Double   => typeof(double),
				TypeCode.Int16    => typeof(short),
				TypeCode.Int32    => typeof(int),
				TypeCode.Int64    => typeof(long),
				TypeCode.Object   => typeof(object),
				TypeCode.SByte    => typeof(sbyte),
				TypeCode.Single   => typeof(float),
				TypeCode.String   => typeof(string),
				TypeCode.UInt16   => typeof(ushort),
				TypeCode.UInt32   => typeof(uint),
				TypeCode.UInt64   => typeof(ulong),
				_                 => typeof(object),
			};
		}
    
        static internal object GetConstFromTypeCode(TypeCode tc)
        {
            object t = null;
            switch (tc)
            {
                case TypeCode.Boolean:
                    t = (object)true;
                    break;
                case TypeCode.Byte:
                    t = (object) Byte.MinValue;
                    break;
                case TypeCode.Char:
                    t = (object)Char.MinValue;
                    break;
                case TypeCode.DateTime:
                    t = (object)DateTime.MinValue;
                    break;
                case TypeCode.Decimal:
                    t = (object)Decimal.MinValue;
                    break;
                case TypeCode.Double:
                    t = (object)Double.MinValue;
                    break;
                case TypeCode.Int16:
                    t = (object)Int16.MinValue;
                    break;
                case TypeCode.Int32:
                    t = (object)Int32.MinValue;
                    break;
                case TypeCode.Int64:
                    t = (object)Int64.MinValue;
                    break;
                case TypeCode.Object:
                    t = (object) "";
                    break;
                case TypeCode.SByte:
                    t = (object)SByte.MinValue;
                    break;
                case TypeCode.Single:
                    t = (object)Single.MinValue;
                    break;
                case TypeCode.String:
                    t = (object)"";
                    break;
                case TypeCode.UInt16:
                    t = (object)UInt16.MinValue;
                    break;
                case TypeCode.UInt32:
                    t = (object)UInt32.MinValue;
                    break;
                case TypeCode.UInt64:
                    t = (object)UInt64.MinValue;
                    break;
                default:
                    t = (object)"";
                    break;
            }
            return t;
        }

        internal static string XmlFileExists(string type)
        {
            if (!type.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase))
                type += ".xml";

            string d0 = RdlEngineConfig.DirectoryLoadedFrom;
            string d1 = AppDomain.CurrentDomain.BaseDirectory;
            string d2 = AppDomain.CurrentDomain.RelativeSearchPath;
            return FileExistsFrom(type, d0, d1, d2);
		}
        
        static string FileExistsFrom(string file, params string[] dir)
        {
            for (int i = 0; i < dir.Length; i++)
            {
                if (dir[i] == null || dir[i] == string.Empty)
                    continue;

                string f = Path.Combine(dir[i], file);  // Issue #37
                if (File.Exists(f))
                    return f;
            }
            // ok check to see if we can load without any directory
            return File.Exists(file)? file: null;
        }
    }
}
