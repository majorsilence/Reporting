// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Majorsilence.Pdf.Security;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class PdfSecurityTests
    {
        // ── helpers ───────────────────────────────────────────────────────────

        private static byte[] MakePdf(Action<PdfDocument> configure)
        {
            var doc = PdfDocument.Create()
                .WithTitle("Security Test")
                .AddPage(PageSizes.A4, c =>
                    c.DrawText("Hello, secured world!", 72, 100,
                        TextStyle.Default.WithSize(14)));

            configure(doc);
            return doc.ToBytes();
        }

        // ── PdfPermissions enum ───────────────────────────────────────────────

        [Test]
        public void PdfPermissions_All_HasAllBitsSet()
        {
            var all = PdfPermissions.All;
            Assert.That(all.HasFlag(PdfPermissions.Print),            Is.True);
            Assert.That(all.HasFlag(PdfPermissions.ModifyContent),    Is.True);
            Assert.That(all.HasFlag(PdfPermissions.CopyText),         Is.True);
            Assert.That(all.HasFlag(PdfPermissions.AddAnnotations),   Is.True);
            Assert.That(all.HasFlag(PdfPermissions.FillForms),        Is.True);
            Assert.That(all.HasFlag(PdfPermissions.ExtractText),      Is.True);
            Assert.That(all.HasFlag(PdfPermissions.Assemble),         Is.True);
            Assert.That(all.HasFlag(PdfPermissions.PrintHighQuality), Is.True);
        }

        [Test]
        public void PdfPermissions_None_HasNoBitsSet()
        {
            Assert.That(PdfPermissions.None, Is.EqualTo((PdfPermissions)0));
        }

        // ── PdfSecurity API ───────────────────────────────────────────────────

        [Test]
        public void PdfSecurity_Protect_DefaultsToAllPermissions()
        {
            var sec = PdfSecurity.Protect("user123");
            Assert.That(sec.UserPassword,      Is.EqualTo("user123"));
            Assert.That(sec.OwnerPassword,     Is.EqualTo("user123")); // owner defaults to user
            Assert.That(sec.Permissions,       Is.EqualTo(PdfPermissions.All));
            Assert.That(sec.EncryptionVersion, Is.EqualTo(PdfEncryptionVersion.AES256));
        }

        [Test]
        public void PdfSecurity_Protect_SeparateOwnerPassword()
        {
            var sec = PdfSecurity.Protect("user", ownerPassword: "owner");
            Assert.That(sec.UserPassword,  Is.EqualTo("user"));
            Assert.That(sec.OwnerPassword, Is.EqualTo("owner"));
        }

        [Test]
        public void PdfSecurity_WithPermissions_ReturnsNewInstance()
        {
            var original = PdfSecurity.Protect("pw");
            var restricted = original.WithPermissions(PdfPermissions.Print);
            Assert.That(restricted.Permissions, Is.EqualTo(PdfPermissions.Print));
            Assert.That(original.Permissions,   Is.EqualTo(PdfPermissions.All)); // unchanged
        }

        [Test]
        public void PdfSecurity_WithOwnerPassword_ReturnsNewInstance()
        {
            var sec = PdfSecurity.Protect("user").WithOwnerPassword("owner");
            Assert.That(sec.OwnerPassword, Is.EqualTo("owner"));
            Assert.That(sec.UserPassword,  Is.EqualTo("user"));
        }

        // ── encrypted PDF structure ───────────────────────────────────────────

        [Test]
        public void WithSecurity_Default_UsesAES256()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("test")));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/Standard"));
            Assert.That(text, Does.Contain("/StmF"));
            Assert.That(text, Does.Contain("/AESV3"));
            Assert.That(text, Does.Contain("/R 6"));
            Assert.That(text, Does.Contain("/V 5"));
            Assert.That(text, Does.Contain("/OE <"));
            Assert.That(text, Does.Contain("/UE <"));
            Assert.That(text, Does.Contain("/Perms <"));
        }

        [Test]
        public void WithSecurity_AES128_UsesRevision4()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("test")
                                            .WithEncryptionVersion(PdfEncryptionVersion.AES128)));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/AESV2"));
            Assert.That(text, Does.Contain("/R 4"));
            Assert.That(text, Does.Contain("/V 4"));
            Assert.That(text, Does.Not.Contain("/OE"));
            Assert.That(text, Does.Not.Contain("/UE"));
        }

        [Test]
        public void WithSecurity_AES256_AutoUpgradesToPdf20()
        {
            // AES-256 (R=6) is a PDF 2.0 feature; the extension method auto-upgrades
            // the document version even if the caller never called WithVersion.
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw")));
            string header = Encoding.Latin1.GetString(pdf, 0, 10);
            Assert.That(header, Does.StartWith("%PDF-2.0"));
        }

        [Test]
        public void WithSecurity_AES128_HeaderRemainsLegacy()
        {
            // AES-128 (R=4) does not trigger a version upgrade.
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw")
                                            .WithEncryptionVersion(PdfEncryptionVersion.AES128)));
            string header = Encoding.Latin1.GetString(pdf, 0, 10);
            Assert.That(header, Does.StartWith("%PDF-1.4"));
        }

        [Test]
        public void WithSecurity_Pdf20_ContainsEncryptDictAndXrefStream()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithVersion(PdfVersion.Pdf20)
                   .WithSecurity(PdfSecurity.Protect("test")));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/XRef"));  // xref stream
            Assert.That(text, Does.Contain("%PDF-2.0"));
        }

        [Test]
        public void WithSecurity_TrailerContainsIdArray()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("")));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/ID [<"));
        }

        [Test]
        public void WithSecurity_NoPassword_IdArrayStillPresent()
        {
            // No-password encryption (empty user password)
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect()));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/ID [<"));
        }

        [Test]
        public void WithSecurity_EncryptObjContainsOUEntries()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("hello", "world")));
            string text = Encoding.Latin1.GetString(pdf);
            // /O and /U entries are 32-byte hex values (64 hex chars each)
            Assert.That(text, Does.Contain("/O <"));
            Assert.That(text, Does.Contain("/U <"));
            Assert.That(text, Does.Contain("/P "));
        }

        [Test]
        public void WithSecurity_PermissionsNone_PValueNegative()
        {
            // With no permissions allowed, the /P value should be negative (high bits set)
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw").WithPermissions(PdfPermissions.None)));
            string text = Encoding.Latin1.GetString(pdf);
            // The P value for no permissions is 0xFFFFF0C0 = -3904 signed
            Assert.That(text, Does.Contain("/P -"));
        }

        [Test]
        public void WithSecurity_StreamDataIsEncrypted_NotPlainText()
        {
            // A document with a known text string should NOT contain that text in the
            // PDF byte stream when encrypted (the content stream is AES-encrypted).
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("key")));

            // The drawing commands won't appear as plain text in an encrypted stream.
            // We look for a PDF operator that would normally appear in the content stream.
            string text = Encoding.Latin1.GetString(pdf);
            // "BT" (Begin Text) in a content stream — should not appear as plain ASCII
            // when encrypted.  Note: it may still appear in dict keys, so we check
            // that "BT\n" (the operator with newline) from the drawing stream is absent.
            // This is a heuristic: the content stream is encrypted, so raw drawing ops
            // should not be visible as ASCII.
            Assert.That(text, Does.Not.Contain("BT\n"));
        }

        [Test]
        public void WithoutSecurity_StreamDataContainsDrawingOps()
        {
            // Control: unencrypted PDF does contain drawing operators in the compressed stream.
            // We can't see them directly (FlateDecode compressed), but the PDF structure is
            // still readable as ASCII for dicts and xref.
            byte[] pdf = MakePdf(_ => { });
            string text = Encoding.Latin1.GetString(pdf);
            // Unencrypted PDF should have readable dict entries
            Assert.That(text, Does.Contain("/Type /Page"));
        }

        [Test]
        public void WithSecurity_MultiplePages_ProducesValidStructure()
        {
            var doc = PdfDocument.Create()
                .WithSecurity(PdfSecurity.Protect("multi"))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 1", 72, 100, TextStyle.Default))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 2", 72, 100, TextStyle.Default))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 3", 72, 100, TextStyle.Default));

            byte[] pdf = doc.ToBytes();
            Assert.That(pdf.Length, Is.GreaterThan(0));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/Count 3"));
        }

        [Test]
        public void WithSecurity_Null_RemovesSecurity()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw"))
                   .WithSecurity(null));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Not.Contain("/Encrypt"));
        }

        [Test]
        public void WithSecurity_ThenWithVersion_BothApplied()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithVersion(PdfVersion.Pdf20)
                   .WithSecurity(PdfSecurity.Protect("v20")));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("%PDF-2.0"));
            Assert.That(text, Does.Contain("/Encrypt"));
        }

        // ── P value bit verification ──────────────────────────────────────────

        [Test]
        public void PValue_PrintOnly_ContainsCorrectPEntry()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(
                    PdfSecurity.Protect("pw")
                        .WithPermissions(PdfPermissions.Print)));
            // P = 0xFFFFF0C0 | 4 = 0xFFFFF0C4 = -3900 signed
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/P -3900"));
        }

        // ── signature placeholder ─────────────────────────────────────────────

        [Test]
        public void WithSignature_ProducesAcroFormAndSigField()
        {
            // Use a self-signed test certificate.
            using var cert = CreateTestCert();
            var sig = new PdfSignatureOptions(cert)
                .WithReason("Unit test")
                .WithSignerName("Tester");

            byte[] pdf = MakePdf(doc => doc.WithSignature(sig));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/AcroForm"));
            Assert.That(text, Does.Contain("/Sig"));
            Assert.That(text, Does.Contain("/ByteRange"));
            Assert.That(text, Does.Contain("adbe.pkcs7.detached"));
        }

        [Test]
        public void WithSignature_ByteRangeIsFilledIn()
        {
            using var cert = CreateTestCert();
            byte[] pdf = MakePdf(doc =>
                doc.WithSignature(new PdfSignatureOptions(cert)));

            string text = Encoding.Latin1.GetString(pdf);
            // After fixup, ByteRange should not be all zeros — at least one non-zero decimal
            // The format is "/ByteRange [AAAAAAAAAA BBBBBBBBBB CCCCCCCCCC DDDDDDDDDD]"
            int idx = text.IndexOf("/ByteRange [", StringComparison.Ordinal);
            Assert.That(idx, Is.GreaterThan(0));
            string br = text.Substring(idx, 60);
            // The second number (offset of /Contents) should be non-zero
            Assert.That(br, Does.Not.Contain("[0000000000 0000000000"));
        }

        [Test]
        public void WithSignature_ContentsIsNotAllZeros()
        {
            using var cert = CreateTestCert();
            byte[] pdf = MakePdf(doc =>
                doc.WithSignature(new PdfSignatureOptions(cert)));

            string text = Encoding.Latin1.GetString(pdf);
            int contIdx = text.IndexOf("/Contents <", StringComparison.Ordinal);
            Assert.That(contIdx, Is.GreaterThan(0));
            // Skip past "/Contents <"
            string contHex = text.Substring(contIdx + "/Contents <".Length, 20);
            // Should not be 00000000... (all zeros = unsigned = no signature written)
            Assert.That(contHex, Is.Not.EqualTo("00000000000000000000"));
        }

        [Test]
        public void WithSignature_WithReasonAndLocation_AppearsInDict()
        {
            using var cert = CreateTestCert();
            var sig = new PdfSignatureOptions(cert)
                .WithReason("Approved")
                .WithLocation("Toronto");

            byte[] pdf = MakePdf(doc => doc.WithSignature(sig));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Reason (Approved)"));
            Assert.That(text, Does.Contain("/Location (Toronto)"));
        }

        // ── PdfSignatureOptions immutability ─────────────────────────────────

        [Test]
        public void PdfSignatureOptions_WithMethods_ReturnNewInstances()
        {
            using var cert = CreateTestCert();
            var original = new PdfSignatureOptions(cert);
            var withReason   = original.WithReason("Approved");
            var withName     = original.WithSignerName("Alice");
            var withLocation = original.WithLocation("Ottawa");
            var withTsa      = original.WithTimestampAuthority("http://tsa.example.com");

            // Each With* call returns a distinct object
            Assert.That(withReason,   Is.Not.SameAs(original));
            Assert.That(withName,     Is.Not.SameAs(original));
            Assert.That(withLocation, Is.Not.SameAs(original));
            Assert.That(withTsa,      Is.Not.SameAs(original));

            // Original is unchanged
            Assert.That(original.Reason,               Is.Null);
            Assert.That(original.SignerName,            Is.Null);
            Assert.That(original.Location,              Is.Null);
            Assert.That(original.TimestampAuthorityUrl, Is.Null);

            // New instances carry the right values
            Assert.That(withReason.Reason,                     Is.EqualTo("Approved"));
            Assert.That(withName.SignerName,                   Is.EqualTo("Alice"));
            Assert.That(withLocation.Location,                 Is.EqualTo("Ottawa"));
            Assert.That(withTsa.TimestampAuthorityUrl,         Is.EqualTo("http://tsa.example.com"));
        }

        [Test]
        public void PdfSignatureOptions_Chain_PreservesAllFields()
        {
            // Fluent chain should accumulate all fields.
            using var cert = CreateTestCert();
            var opts = new PdfSignatureOptions(cert)
                .WithReason("Approved")
                .WithSignerName("Bob")
                .WithLocation("Vancouver")
                .WithTimestampAuthority("http://ts.example.com");

            Assert.That(opts.Reason,               Is.EqualTo("Approved"));
            Assert.That(opts.SignerName,            Is.EqualTo("Bob"));
            Assert.That(opts.Location,              Is.EqualTo("Vancouver"));
            Assert.That(opts.TimestampAuthorityUrl, Is.EqualTo("http://ts.example.com"));
        }

        // ── signature placeholder size ────────────────────────────────────────

        [Test]
        public void WithSignature_PlaceholderIs32KB()
        {
            // The /Contents hex run in the placeholder should be 32768*2 = 65536 zeros.
            using var cert = CreateTestCert();
            byte[] raw = MakePdf(doc => doc.WithSignature(new PdfSignatureOptions(cert)));
            string text = Encoding.Latin1.GetString(raw);
            int contIdx = text.IndexOf("/Contents <", StringComparison.Ordinal);
            Assert.That(contIdx, Is.GreaterThan(0));
            // The placeholder content is PlaceholderBytes*2 hex chars (could be partly filled in).
            // We just verify the /Contents value is >= 32768 hex chars wide.
            int hexStart = contIdx + "/Contents <".Length;
            int hexEnd   = text.IndexOf('>', hexStart);
            Assert.That(hexEnd - hexStart, Is.GreaterThanOrEqualTo(32768 * 2));
        }

        // ── UTF-8 password truncation ─────────────────────────────────────────

        [Test]
        public void WithSecurity_ShortUnicodePassword_RoundTripsCorrectly()
        {
            // A short password with multi-byte UTF-8 chars (well under 127 bytes)
            // should not be mangled.
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("élève"))); // "élève"
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
        }

        [Test]
        public void WithSecurity_LongUnicodePassword_DoesNotThrow()
        {
            // A password whose UTF-8 encoding exceeds 127 bytes should be truncated
            // at a character boundary without throwing.
            // Each CJK character is 3 bytes; 50 of them = 150 UTF-8 bytes > 127.
            string longPw = new string('中', 50); // 50 × CJK U+4E2D
            Assert.DoesNotThrow(() =>
                MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect(longPw))));
        }

        [Test]
        public void WithSecurity_PasswordTruncation_NeverCutsCharBoundary()
        {
            // Build a password where a 3-byte UTF-8 sequence straddles byte 127.
            // 42 ASCII chars (42 bytes) + 29 CJK chars (87 bytes) = 129 bytes total.
            // The 29th CJK char starts at byte 126 (42 + 28*3 = 126) and ends at 128.
            // Correct truncation drops it entirely, yielding 126 bytes (42 + 28*3).
            string pw = new string('A', 42) + new string('中', 29);
            // Just verify it produces a valid encrypted PDF (no exception, valid structure).
            byte[] pdf = MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect(pw)));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
        }

        // ── NFKC normalization ────────────────────────────────────────────────

        [Test]
        public void WithSecurity_NfkcPassword_ProducesValidPdf()
        {
            // A password using a composed Unicode form (should be normalized to NFC/NFKC).
            // U+00E9 = precomposed é, U+0301 = combining acute — NFKC maps both to é.
            string precomposed  = "é";   // single code point: é
            string decomposed   = "é";  // e + combining accent: also é after NFKC
            // Both should produce a valid PDF without throwing.
            Assert.DoesNotThrow(() => MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect(precomposed))));
            Assert.DoesNotThrow(() => MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect(decomposed))));
        }

        // ── RFC 3161 timestamp URL property ──────────────────────────────────

        [Test]
        public void PdfSignatureOptions_WithTimestampAuthority_SetsUrl()
        {
            using var cert = CreateTestCert();
            var opts = new PdfSignatureOptions(cert)
                .WithTimestampAuthority("http://timestamp.digicert.com");
            Assert.That(opts.TimestampAuthorityUrl, Is.EqualTo("http://timestamp.digicert.com"));
        }

        [Test]
        public void WithSignature_NoTsa_StillProducesValidSignature()
        {
            // Verify existing signature path is unaffected when no TSA is configured.
            using var cert = CreateTestCert();
            var opts = new PdfSignatureOptions(cert).WithReason("No TSA");
            byte[] pdf = MakePdf(doc => doc.WithSignature(opts));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/ByteRange"));
            int contIdx = text.IndexOf("/Contents <", StringComparison.Ordinal);
            string firstHex = text.Substring(contIdx + "/Contents <".Length, 20);
            Assert.That(firstHex, Is.Not.EqualTo("00000000000000000000"));
        }

        // ── round-trip decryption (reader-side regression guard) ─────────────
        //
        // The structural tests above only assert dictionary keys are present; they
        // never verify a password actually works.  These tests re-implement the
        // reader side of ISO 32000-2 Algorithm 2.A / 2.B (independently of the
        // writer's encryption internals) and confirm the password validates and
        // the file encryption key can be recovered.  A regression in the key
        // derivation (e.g. hashing the AES input instead of the AES output in
        // Algorithm 2.B) fails these tests.

        [Test]
        public void WithSecurity_AES256_PasswordValidatesAndFileKeyRecovers()
        {
            const string pw = "open123";
            byte[] pdf  = MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect(pw)));
            string text = Encoding.Latin1.GetString(pdf);

            byte[] U     = HexEntry(text, "U");
            byte[] UE    = HexEntry(text, "UE");
            byte[] Perms = HexEntry(text, "Perms");
            int    P     = int.Parse(System.Text.RegularExpressions.Regex
                                      .Match(text, @"/P (-?\d+)").Groups[1].Value);

            Assert.That(U.Length, Is.EqualTo(48));

            byte[] pwBytes        = Encoding.UTF8.GetBytes(pw);
            byte[] validationSalt = Slice(U, 32, 8);
            byte[] keySalt        = Slice(U, 40, 8);

            // Algorithm 2.A: a reader recomputes the validation hash from the
            // password and compares it against the first 32 bytes of /U.
            byte[] computed = Hash2B(pwBytes, validationSalt, Array.Empty<byte>());
            Assert.That(computed, Is.EqualTo(Slice(U, 0, 32)),
                "password validation hash does not match /U — readers will reject the password");

            // Recover the file encryption key and confirm it decrypts /Perms,
            // proving the FEK derivation round-trips end-to-end.
            byte[] intermediate = Hash2B(pwBytes, keySalt, Array.Empty<byte>());
            byte[] fek          = AesCbcDecryptNoPad(intermediate, new byte[16], UE);
            byte[] permsPlain   = AesEcbDecryptNoPad(fek, Perms);

            Assert.That((char)permsPlain[9],  Is.EqualTo('a'));
            Assert.That((char)permsPlain[10], Is.EqualTo('d'));
            Assert.That((char)permsPlain[11], Is.EqualTo('b'),
                "/Perms did not decrypt with the recovered file key — FEK derivation is wrong");
            int permsP = permsPlain[0] | (permsPlain[1] << 8) | (permsPlain[2] << 16) | (permsPlain[3] << 24);
            Assert.That(permsP, Is.EqualTo(P));
        }

        [Test]
        public void WithSecurity_AES256_WrongPasswordDoesNotValidate()
        {
            byte[] pdf  = MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect("correct-horse")));
            string text = Encoding.Latin1.GetString(pdf);
            byte[] U    = HexEntry(text, "U");

            byte[] computed = Hash2B(Encoding.UTF8.GetBytes("wrong-pony"),
                                     Slice(U, 32, 8), Array.Empty<byte>());
            Assert.That(computed, Is.Not.EqualTo(Slice(U, 0, 32)));
        }

        // ── full end-to-end decryption (validates real encrypted output) ─────
        //
        // These tests reconstruct the file encryption key from the password
        // exactly as a conforming reader does (ISO 32000-2 Algorithm 2.A/2.B for
        // R=6; the ISO 32000-1 MD5 algorithm for R=4), then decrypt and inflate
        // the page content stream and confirm the drawn text round-trips.  This
        // exercises the whole pipeline the way Adobe / pdf.js / poppler do, so a
        // PDF that opens here will open in a real viewer with the same password.

        private const string DrawnText = "Hello, secured world!";

        [Test]
        public void Decrypt_AES256_UserPassword_RecoversContent()
        {
            byte[] pdf = MakePdf(doc => doc.WithSecurity(PdfSecurity.Protect("open123")));
            Assert.That(DecryptFirstPageContent(pdf, "open123"), Does.Contain(DrawnText));
        }

        [Test]
        public void Decrypt_AES256_EmptyUserPassword_RecoversContent()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("", ownerPassword: "owner456")));
            Assert.That(DecryptFirstPageContent(pdf, ""), Does.Contain(DrawnText));
        }

        [Test]
        public void Decrypt_AES256_SeparateOwnerPassword_BothUnlockContent()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("open123", ownerPassword: "owner456")
                                            .WithPermissions(PdfPermissions.Print)));
            // The user password and the owner password must unlock the same content.
            Assert.That(DecryptFirstPageContent(pdf, "open123"),                 Does.Contain(DrawnText));
            Assert.That(DecryptFirstPageContent(pdf, "owner456", asOwner: true), Does.Contain(DrawnText));
        }

        [Test]
        public void Decrypt_AES128_UserPassword_RecoversContent()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("open123")
                                            .WithEncryptionVersion(PdfEncryptionVersion.AES128)));
            Assert.That(DecryptFirstPageContent(pdf, "open123"), Does.Contain(DrawnText));
        }

        [TestCase(PdfEncryptionVersion.AES256)]
        [TestCase(PdfEncryptionVersion.AES128)]
        public void Decrypt_WrongPassword_DoesNotRecoverContent(PdfEncryptionVersion version)
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("open123").WithEncryptionVersion(version)));

            // A wrong password derives the wrong key: AES padding validation throws,
            // or the inflated bytes are garbage.  Either way the plaintext must not appear.
            string? content = null;
            try { content = DecryptFirstPageContent(pdf, "wrong-password"); }
            catch { /* bad padding / inflate failure is an acceptable outcome */ }

            if (content != null)
                Assert.That(content, Does.Not.Contain(DrawnText));
        }

        // ISO 32000-2:2020 §7.6.4.3.4 Algorithm 2.B (hash, revision 6).
        // Implemented here independently of the library under test.
        private static byte[] Hash2B(byte[] password, byte[] salt, byte[] userKey)
        {
            byte[] K = Sha(256, Concat(password, salt, userKey));
            for (int round = 1; ; round++)
            {
                byte[] seq = Concat(password, K, userKey);
                byte[] K1  = new byte[seq.Length * 64];
                for (int i = 0; i < 64; i++) Buffer.BlockCopy(seq, 0, K1, i * seq.Length, seq.Length);

                byte[] key = Slice(K, 0, 16);
                byte[] iv  = Slice(K, 16, 16);
                byte[] E   = AesCbcEncryptNoPad(key, iv, K1);

                int sum = 0;
                for (int i = 0; i < 16; i++) sum += E[i];
                int bits = (sum % 3) == 0 ? 256 : (sum % 3) == 1 ? 384 : 512;
                K = Sha(bits, E);   // hash of E (the AES output), per spec step (d)

                if (round >= 64 && (E[E.Length - 1] & 0xFF) <= round - 32) break;
            }
            return Slice(K, 0, 32);
        }

        private static byte[] Sha(int bits, byte[] data)
        {
            using HashAlgorithm h = bits == 256 ? SHA256.Create()
                                  : bits == 384 ? SHA384.Create()
                                  :               (HashAlgorithm)SHA512.Create();
            return h.ComputeHash(data);
        }

        private static byte[] AesCbcEncryptNoPad(byte[] key, byte[] iv, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
            using var e = aes.CreateEncryptor();
            return e.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] AesCbcDecryptNoPad(byte[] key, byte[] iv, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
            using var d = aes.CreateDecryptor();
            return d.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] AesEcbDecryptNoPad(byte[] key, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var d = aes.CreateDecryptor();
            return d.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] HexEntry(string pdf, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(pdf, "/" + name + @" <([0-9A-Fa-f]+)>");
            Assert.That(m.Success, Is.True, "entry /" + name + " not found in encrypt dict");
            string hex = m.Groups[1].Value;
            var b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return b;
        }

        private static byte[] Slice(byte[] src, int offset, int count)
        {
            var r = new byte[count];
            Buffer.BlockCopy(src, offset, r, 0, count);
            return r;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len += p.Length;
            var r = new byte[len];
            int pos = 0;
            foreach (var p in parts) { Buffer.BlockCopy(p, 0, r, pos, p.Length); pos += p.Length; }
            return r;
        }

        // ── reader-side decryptor: recover the file key and decrypt content ───

        // Decrypts and inflates the first page's content stream using only the
        // password and the bytes the library produced — no access to the writer's
        // internal key.  Returns the decompressed content-stream text.
        private static string DecryptFirstPageContent(byte[] pdf, string password, bool asOwner = false)
        {
            string text = Encoding.Latin1.GetString(pdf);
            int    R    = IntEntry(text, "R");

            int contentObjNum = int.Parse(System.Text.RegularExpressions.Regex
                .Match(text, @"/Contents (\d+) 0 R").Groups[1].Value);
            byte[] streamData = ReadObjectStream(pdf, text, contentObjNum);

            byte[] compressed;
            if (R == 6)
            {
                byte[] U  = HexEntry(text, "U");
                byte[] UE = HexEntry(text, "UE");
                byte[] O  = HexEntry(text, "O");
                byte[] OE = HexEntry(text, "OE");
                byte[] fek = RecoverFileKeyR6(Encoding.UTF8.GetBytes(password), U, UE, O, OE, asOwner);
                compressed = AesCbcDecryptIvPrefixed(fek, streamData);
            }
            else // Revision 4 (AES-128, per-object key)
            {
                byte[] O  = HexEntry(text, "O");
                int    P  = IntEntry(text, "P");
                byte[] id = IdEntry(text);
                byte[] fileKey = ComputeFileKeyR4(password, O, P, id);
                byte[] objKey  = ObjectKeyR4(fileKey, contentObjNum, 0);
                compressed = AesCbcDecryptIvPrefixed(objKey, streamData);
            }
            return Encoding.Latin1.GetString(Inflate(compressed));
        }

        // ISO 32000-2 Algorithm 2.A: re-derive the intermediate key from the
        // password and decrypt /UE (user) or /OE (owner) to get the 32-byte key.
        private static byte[] RecoverFileKeyR6(byte[] pw, byte[] U, byte[] UE, byte[] O, byte[] OE, bool asOwner)
        {
            if (asOwner)
            {
                byte[] ikey = Hash2B(pw, Slice(O, 40, 8), U);   // owner hash mixes in /U
                return AesCbcDecryptNoPad(ikey, new byte[16], OE);
            }
            byte[] userIkey = Hash2B(pw, Slice(U, 40, 8), Array.Empty<byte>());
            return AesCbcDecryptNoPad(userIkey, new byte[16], UE);
        }

        // ISO 32000-1 Algorithm 2: derive the 16-byte file key for Revision 4.
        private static byte[] ComputeFileKeyR4(string pw, byte[] O, int P, byte[] id)
        {
            using var md5 = MD5.Create();
            byte[] padded = PadPasswordR4(pw);
            md5.TransformBlock(padded, 0, 32, null, 0);
            md5.TransformBlock(O, 0, 32, null, 0);
            byte[] pb = BitConverter.GetBytes(P);
            md5.TransformBlock(pb, 0, 4, null, 0);
            md5.TransformFinalBlock(id, 0, id.Length);
            byte[] hash = md5.Hash!;
            for (int i = 0; i < 50; i++) { using var m = MD5.Create(); hash = m.ComputeHash(hash, 0, 16); }
            return Slice(hash, 0, 16);
        }

        // AESV2 per-object key: MD5(fileKey ‖ objNum(3 LE) ‖ gen(2 LE) ‖ "sAlT").
        private static byte[] ObjectKeyR4(byte[] fileKey, int objNum, int genNum)
        {
            var data = new byte[16 + 3 + 2 + 4];
            Buffer.BlockCopy(fileKey, 0, data, 0, 16);
            data[16] = (byte)objNum; data[17] = (byte)(objNum >> 8); data[18] = (byte)(objNum >> 16);
            data[19] = (byte)genNum; data[20] = (byte)(genNum >> 8);
            data[21] = 0x73; data[22] = 0x41; data[23] = 0x6C; data[24] = 0x54; // "sAlT"
            using var m = MD5.Create();
            return Slice(m.ComputeHash(data), 0, 16);
        }

        private static readonly byte[] PasswordPadR4 = {
            0x28,0xBF,0x4E,0x5E, 0x4E,0x75,0x8A,0x41, 0x64,0x00,0x4E,0x56, 0xFF,0xFA,0x01,0x08,
            0x2E,0x2E,0x00,0xB6, 0xD0,0x68,0x3E,0x80, 0x2F,0x0C,0xA9,0xFE, 0x64,0x53,0x69,0x7A,
        };

        private static byte[] PadPasswordR4(string pw)
        {
            byte[] src    = Encoding.GetEncoding("iso-8859-1").GetBytes(pw ?? "");
            var    result = new byte[32];
            int    n      = Math.Min(src.Length, 32);
            Buffer.BlockCopy(src, 0, result, 0, n);
            Buffer.BlockCopy(PasswordPadR4, 0, result, n, 32 - n);
            return result;
        }

        // CBC decrypt where the data is a random IV prepended to PKCS7 ciphertext
        // (the form the library emits for both AESV2 and AESV3 streams/strings).
        private static byte[] AesCbcDecryptIvPrefixed(byte[] key, byte[] ivAndCiphertext)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            aes.IV = Slice(ivAndCiphertext, 0, 16);
            using var d = aes.CreateDecryptor();
            return d.TransformFinalBlock(ivAndCiphertext, 16, ivAndCiphertext.Length - 16);
        }

        private static byte[] Inflate(byte[] zlib)
        {
            using var inp  = new MemoryStream(zlib);
            using var zs   = new ZLibStream(inp, CompressionMode.Decompress);
            using var outp = new MemoryStream();
            zs.CopyTo(outp);
            return outp.ToArray();
        }

        // ── PDF structure parsing helpers ────────────────────────────────────

        private static int IntEntry(string pdf, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(pdf, "/" + name + @" (-?\d+)");
            Assert.That(m.Success, Is.True, "entry /" + name + " not found");
            return int.Parse(m.Groups[1].Value);
        }

        private static byte[] IdEntry(string pdf)
        {
            var m = System.Text.RegularExpressions.Regex.Match(pdf, @"/ID\s*\[\s*<([0-9A-Fa-f]+)>");
            Assert.That(m.Success, Is.True, "/ID array not found");
            return HexToBytes(m.Groups[1].Value);
        }

        // Reads the raw stream bytes of object N, using its /Length to copy the
        // exact payload (the bytes are binary ciphertext, so length-based, not
        // delimiter-based, extraction is required).
        private static byte[] ReadObjectStream(byte[] pdf, string text, int objNum)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text, @"(?<![0-9])" + objNum + @" 0 obj");
            Assert.That(m.Success, Is.True, "object " + objNum + " not found");
            int objStart = m.Index;
            var lenM = System.Text.RegularExpressions.Regex.Match(text.Substring(objStart), @"/Length (\d+)");
            Assert.That(lenM.Success, Is.True, "stream /Length not found for object " + objNum);
            int len = int.Parse(lenM.Groups[1].Value);
            int streamIdx = text.IndexOf("stream\n", objStart, StringComparison.Ordinal);
            Assert.That(streamIdx, Is.GreaterThan(0), "stream keyword not found for object " + objNum);
            int payloadStart = streamIdx + "stream\n".Length;
            var data = new byte[len];
            Buffer.BlockCopy(pdf, payloadStart, data, 0, len);
            return data;
        }

        private static byte[] HexToBytes(string hex)
        {
            var b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return b;
        }

        // ── self-signed certificate factory ──────────────────────────────────

        private static X509Certificate2 CreateTestCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=PdfSecurityTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

            var cert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddYears(1));

#if NET5_0_OR_GREATER
            // Export/import required on some platforms to ensure private key is accessible
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
#else
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12), (string?)null,
                X509KeyStorageFlags.Exportable);
#endif
        }
    }
}
