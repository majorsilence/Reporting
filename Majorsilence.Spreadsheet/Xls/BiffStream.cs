using System;
using System.IO;
using System.Text;

namespace Majorsilence.Spreadsheet.Xls
{
    /// <summary>Builds a BIFF8 record stream: each record is a 2-byte type, 2-byte length and its data.</summary>
    internal sealed class BiffStream
    {
        public const int MaxRecordData = 8224;

        private readonly MemoryStream _stream = new MemoryStream();
        private readonly BinaryWriter _out;
        private readonly MemoryStream _record = new MemoryStream();
        private readonly BinaryWriter _rec;
        private ushort _type;

        public BiffStream()
        {
            _out = new BinaryWriter(_stream);
            _rec = new BinaryWriter(_record);
        }

        public long Position => _stream.Position;

        public BinaryWriter Begin(ushort type)
        {
            _type = type;
            _record.SetLength(0);
            return _rec;
        }

        /// <summary>Bytes written to the open record so far.</summary>
        public int RecordLength => (int)_record.Length;

        public void End()
        {
            _rec.Flush();
            if (_record.Length > MaxRecordData)
                throw new InvalidOperationException($"BIFF record 0x{_type:X4} is {_record.Length} bytes; the limit is {MaxRecordData}.");
            _out.Write(_type);
            _out.Write((ushort)_record.Length);
            _record.Position = 0;
            _record.CopyTo(_stream);
        }

        public void Record(ushort type, Action<BinaryWriter> body)
        {
            body(Begin(type));
            End();
        }

        public void PatchUInt32(long position, uint value)
        {
            long saved = _stream.Position;
            _stream.Position = position;
            _out.Write(value);
            _stream.Position = saved;
        }

        public void Append(BiffStream other)
        {
            _out.Flush();
            other._out.Flush();
            other._stream.Position = 0;
            other._stream.CopyTo(_stream);
        }

        public byte[] ToArray()
        {
            _out.Flush();
            return _stream.ToArray();
        }

        /// <summary>ShortXLUnicodeString: 1-byte length, flags, UTF-16LE characters.</summary>
        public static void WriteShortString(BinaryWriter w, string s)
        {
            if (s.Length > 255) s = s.Substring(0, 255);
            w.Write((byte)s.Length);
            w.Write((byte)0x01);
            w.Write(Encoding.Unicode.GetBytes(s));
        }

        /// <summary>XLUnicodeString: 2-byte length, flags, UTF-16LE characters.</summary>
        public static void WriteString(BinaryWriter w, string s)
        {
            w.Write((ushort)s.Length);
            w.Write((byte)0x01);
            w.Write(Encoding.Unicode.GetBytes(s));
        }
    }
}
