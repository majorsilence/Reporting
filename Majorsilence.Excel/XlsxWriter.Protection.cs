using System;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Excel
{
    /// <summary>
    /// What users may still do on a protected sheet. Cells whose format has Locked = false stay editable.
    /// The defaults match Excel's Protect Sheet dialog: only selecting cells is allowed.
    /// </summary>
    public class XlsxSheetProtection
    {
        /// <summary>Optional password needed to unprotect the sheet. Protection is a convenience, not encryption.</summary>
        public string? Password;

        public bool AllowSelectLockedCells = true;
        public bool AllowSelectUnlockedCells = true;
        public bool AllowFormatCells;
        public bool AllowFormatColumns;
        public bool AllowFormatRows;
        public bool AllowInsertColumns;
        public bool AllowInsertRows;
        public bool AllowInsertHyperlinks;
        public bool AllowDeleteColumns;
        public bool AllowDeleteRows;
        public bool AllowSort;
        public bool AllowAutoFilter;
        public bool AllowPivotTables;
        public bool AllowEditObjects;
        public bool AllowEditScenarios;
    }

    public partial class XlsxWriter
    {
        private const int ProtectionSpinCount = 100000;

        public void ProtectSheet(XlsxSheetProtection? options = null, int sheetIndex = 0)
        {
            options ??= new XlsxSheetProtection();
            var sheet = _sheets[sheetIndex];
            sheet.Protection = options;
            sheet.ProtectionHash = null;
            if (!string.IsNullOrEmpty(options.Password))
            {
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(salt);
                sheet.ProtectionHash = (Convert.ToBase64String(HashPassword(options.Password!, salt, ProtectionSpinCount)),
                    Convert.ToBase64String(salt));
            }
        }

        // ECMA-376 agile password hash: H0 = SHA-512(salt + UTF-16LE password), then
        // Hn = SHA-512(Hn-1 + little-endian iteration number) for each spin.
        internal static byte[] HashPassword(string password, byte[] salt, int spinCount)
        {
            using var sha = SHA512.Create();
            byte[] pwd = Encoding.Unicode.GetBytes(password);
            byte[] buffer = new byte[salt.Length + pwd.Length];
            Buffer.BlockCopy(salt, 0, buffer, 0, salt.Length);
            Buffer.BlockCopy(pwd, 0, buffer, salt.Length, pwd.Length);
            byte[] hash = sha.ComputeHash(buffer);

            byte[] round = new byte[hash.Length + 4];
            for (int i = 0; i < spinCount; i++)
            {
                Buffer.BlockCopy(hash, 0, round, 0, hash.Length);
                round[hash.Length] = (byte)i;
                round[hash.Length + 1] = (byte)(i >> 8);
                round[hash.Length + 2] = (byte)(i >> 16);
                round[hash.Length + 3] = (byte)(i >> 24);
                hash = sha.ComputeHash(round);
            }
            return hash;
        }

        // In <sheetProtection> an attribute of 1 means the action is blocked; select* are the reverse.
        private static void AppendSheetProtection(StringBuilder sb, Sheet sheet)
        {
            var p = sheet.Protection;
            if (p == null) return;
            sb.Append("<sheetProtection");
            if (sheet.ProtectionHash.HasValue)
            {
                sb.Append($" algorithmName=\"SHA-512\" hashValue=\"{sheet.ProtectionHash.Value.Hash}\" " +
                          $"saltValue=\"{sheet.ProtectionHash.Value.Salt}\" spinCount=\"{ProtectionSpinCount}\"");
            }
            sb.Append(" sheet=\"1\"");
            sb.Append($" objects=\"{Blocked(p.AllowEditObjects)}\" scenarios=\"{Blocked(p.AllowEditScenarios)}\"");
            if (p.AllowFormatCells) sb.Append(" formatCells=\"0\"");
            if (p.AllowFormatColumns) sb.Append(" formatColumns=\"0\"");
            if (p.AllowFormatRows) sb.Append(" formatRows=\"0\"");
            if (p.AllowInsertColumns) sb.Append(" insertColumns=\"0\"");
            if (p.AllowInsertRows) sb.Append(" insertRows=\"0\"");
            if (p.AllowInsertHyperlinks) sb.Append(" insertHyperlinks=\"0\"");
            if (p.AllowDeleteColumns) sb.Append(" deleteColumns=\"0\"");
            if (p.AllowDeleteRows) sb.Append(" deleteRows=\"0\"");
            if (!p.AllowSelectLockedCells) sb.Append(" selectLockedCells=\"1\"");
            if (p.AllowSort) sb.Append(" sort=\"0\"");
            if (p.AllowAutoFilter) sb.Append(" autoFilter=\"0\"");
            if (p.AllowPivotTables) sb.Append(" pivotTables=\"0\"");
            if (!p.AllowSelectUnlockedCells) sb.Append(" selectUnlockedCells=\"1\"");
            sb.Append("/>");
        }

        private static int Blocked(bool allowed) => allowed ? 0 : 1;

        private partial class Sheet
        {
            public XlsxSheetProtection? Protection;
            public (string Hash, string Salt)? ProtectionHash;
        }
    }
}
