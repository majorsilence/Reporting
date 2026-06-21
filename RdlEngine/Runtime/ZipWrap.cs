
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Thin wrapper over ICSharpCode.SharpZipLib.Zip for use by the Excel renderer.
    /// Previously loaded SharpZipLib dynamically; it is now a direct NuGet reference.
    /// </summary>
    public class ZipWrap
    {
        public static void Init() { } // retained for API compatibility

        public static string ZipError => string.Empty;

        [RequiresUnreferencedCode("PropertySettingByEnum uses reflection to set enum properties by name")]
        public static void PropertySettingByEnum(object classInstance, Type classType, string propertyName, string desiredValue)
        {
            PropertyInfo? pi = classType.GetProperty(propertyName);
            if (pi != null)
            {
                object value2change = Enum.Parse(pi.PropertyType, desiredValue);
                pi.SetValue(classInstance, value2change, null);
            }
        }
    }

    public class ZipOutputStream
    {
        private readonly ICSharpCode.SharpZipLib.Zip.ZipOutputStream _inner;

        public ZipOutputStream(Stream baseOutputStream)
        {
            _inner = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(baseOutputStream);
            _inner.UseZip64 = UseZip64.Off;
        }

        public Stream ZipStream => _inner;

        public void PutNextEntry(ZipEntry ze) => _inner.PutNextEntry(ze.Inner);

        public void Write(string str)
        {
            byte[] ubuf = Encoding.Unicode.GetBytes(str);
            byte[] abuf = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, ubuf);
            Write(abuf, 0, abuf.Length);
        }

        public void Write(byte[] buffer, int offset, int count) =>
            _inner.Write(buffer, offset, count);

        public void Finish() => _inner.Finish();

        public void Close() => _inner.Close();
    }

    public class ZipEntry
    {
        internal readonly ICSharpCode.SharpZipLib.Zip.ZipEntry Inner;

        public ZipEntry(string name)
        {
            Inner = new ICSharpCode.SharpZipLib.Zip.ZipEntry(name);
        }

        public object Value => Inner;
    }
}
