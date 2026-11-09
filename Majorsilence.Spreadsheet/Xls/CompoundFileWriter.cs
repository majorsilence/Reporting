using System;
using System.IO;
using System.Text;

namespace Majorsilence.Spreadsheet.Xls
{
    /// <summary>
    /// Writes a version 3 compound file (OLE2 structured storage, MS-CFB) holding a single stream.
    /// The stream is padded to the 4096-byte mini-stream cutoff so it always lives in regular
    /// 512-byte sectors and no mini stream is needed; readers stop at the data's own end marker.
    /// </summary>
    internal static class CompoundFileWriter
    {
        private const int SectorSize = 512;
        private const int EntriesPerFatSector = SectorSize / 4;
        private const int HeaderDifatEntries = 109;
        private const int MiniStreamCutoff = 4096;

        private const uint FreeSect = 0xFFFFFFFF;
        private const uint EndOfChain = 0xFFFFFFFE;
        private const uint FatSect = 0xFFFFFFFD;
        private const uint DifSect = 0xFFFFFFFC;
        private const uint NoStream = 0xFFFFFFFF;

        public static void Write(Stream output, string streamName, byte[] data)
        {
            int length = Math.Max(data.Length, MiniStreamCutoff);
            int dataSectors = (length + SectorSize - 1) / SectorSize;
            const int dirSectors = 1;

            // FAT sectors cover every sector including themselves and the DIFAT sectors; iterate to a fixed point.
            int fatSectors = 1, difatSectors = 0;
            while (true)
            {
                int total = dataSectors + dirSectors + fatSectors + difatSectors;
                int needFat = (total + EntriesPerFatSector - 1) / EntriesPerFatSector;
                int needDifat = needFat > HeaderDifatEntries
                    ? (needFat - HeaderDifatEntries + EntriesPerFatSector - 2) / (EntriesPerFatSector - 1)
                    : 0;
                if (needFat == fatSectors && needDifat == difatSectors) break;
                fatSectors = needFat;
                difatSectors = needDifat;
            }

            int dirStart = dataSectors;
            int fatStart = dirStart + dirSectors;
            int difatStart = fatStart + fatSectors;
            int totalSectors = difatStart + difatSectors;

            var fat = new uint[fatSectors * EntriesPerFatSector];
            for (int i = 0; i < fat.Length; i++) fat[i] = FreeSect;
            for (int i = 0; i < dataSectors; i++) fat[i] = i == dataSectors - 1 ? EndOfChain : (uint)(i + 1);
            fat[dirStart] = EndOfChain;
            for (int i = 0; i < fatSectors; i++) fat[fatStart + i] = FatSect;
            for (int i = 0; i < difatSectors; i++) fat[difatStart + i] = DifSect;

            var w = new BinaryWriter(output, Encoding.Unicode, leaveOpen: true);
            WriteHeader(w, fatSectors, dirStart, fatStart, difatStart, difatSectors);

            w.Write(data);
            int padding = dataSectors * SectorSize - data.Length;
            w.Write(new byte[padding]);

            WriteDirectory(w, streamName, length);

            foreach (uint entry in fat) w.Write(entry);

            // DIFAT sectors: FAT sector locations beyond the 109 in the header, last slot chains to the next.
            int next = HeaderDifatEntries;
            for (int d = 0; d < difatSectors; d++)
            {
                for (int i = 0; i < EntriesPerFatSector - 1; i++, next++)
                    w.Write(next < fatSectors ? (uint)(fatStart + next) : FreeSect);
                w.Write(d == difatSectors - 1 ? EndOfChain : (uint)(difatStart + d + 1));
            }
            w.Flush();

            if (totalSectors != dataSectors + dirSectors + fatSectors + difatSectors)
                throw new InvalidOperationException("Compound file layout mismatch.");
        }

        private static void WriteHeader(BinaryWriter w, int fatSectors, int dirStart, int fatStart, int difatStart, int difatSectors)
        {
            w.Write(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
            w.Write(new byte[16]);              // CLSID
            w.Write((ushort)0x003E);            // minor version
            w.Write((ushort)0x0003);            // major version 3 (512-byte sectors)
            w.Write((ushort)0xFFFE);            // little-endian
            w.Write((ushort)9);                 // sector shift: 512
            w.Write((ushort)6);                 // mini sector shift: 64
            w.Write(new byte[6]);
            w.Write(0u);                        // directory sectors (must be 0 for v3)
            w.Write((uint)fatSectors);
            w.Write((uint)dirStart);
            w.Write(0u);                        // transaction signature
            w.Write((uint)MiniStreamCutoff);
            w.Write(EndOfChain);                // first mini FAT sector
            w.Write(0u);                        // mini FAT sectors
            w.Write(difatSectors > 0 ? (uint)difatStart : EndOfChain);
            w.Write((uint)difatSectors);
            for (int i = 0; i < HeaderDifatEntries; i++)
                w.Write(i < fatSectors ? (uint)(fatStart + i) : FreeSect);
        }

        private static void WriteDirectory(BinaryWriter w, string streamName, int length)
        {
            WriteEntry(w, "Root Entry", type: 5, child: 1, start: EndOfChain, size: 0);
            WriteEntry(w, streamName, type: 2, child: NoStream, start: 0, size: (uint)length);
            WriteEntry(w, null, 0, NoStream, 0, 0);
            WriteEntry(w, null, 0, NoStream, 0, 0);
        }

        private static void WriteEntry(BinaryWriter w, string? name, byte type, uint child, uint start, uint size)
        {
            var nameBytes = new byte[64];
            ushort nameLength = 0;
            if (name != null)
            {
                byte[] encoded = Encoding.Unicode.GetBytes(name);
                Buffer.BlockCopy(encoded, 0, nameBytes, 0, encoded.Length);
                nameLength = (ushort)(encoded.Length + 2); // includes the terminating null
            }
            w.Write(nameBytes);
            w.Write(nameLength);
            w.Write(type);
            w.Write((byte)(name == null ? 0 : 1)); // colour: black
            w.Write(NoStream);                      // left sibling
            w.Write(NoStream);                      // right sibling
            w.Write(child);
            w.Write(new byte[16]);                  // CLSID
            w.Write(0u);                            // state bits
            w.Write(0L);                            // creation time
            w.Write(0L);                            // modified time
            w.Write(start);
            w.Write(size);
            w.Write(0u);                            // high 32 bits of size (v3)
        }
    }
}
