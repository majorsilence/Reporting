// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Password-based security settings for a PDF document.
    /// Defaults to Standard Security Handler Revision 6 with AES-256-CBC encryption
    /// (PDF 2.0, ISO 32000-2:2020).  Use <see cref="WithEncryptionVersion"/> with
    /// <see cref="PdfEncryptionVersion.AES128"/> to select AES-128 (Revision 4)
    /// for broader viewer compatibility.
    /// </summary>
    /// <example>
    /// <code>
    /// // PDF 2.0 with AES-256 (default)
    /// PdfDocument.Create()
    ///     .WithVersion(PdfVersion.Pdf20)
    ///     .WithSecurity(PdfSecurity.Protect("open123", ownerPassword: "owner456")
    ///                              .WithPermissions(PdfPermissions.Print))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Hello", 72, 100))
    ///     .Save("protected.pdf");
    ///
    /// // AES-128 for older readers (PDF 1.5+)
    /// PdfDocument.Create()
    ///     .WithSecurity(PdfSecurity.Protect("open123")
    ///                              .WithEncryptionVersion(PdfEncryptionVersion.AES128))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Hello", 72, 100))
    ///     .Save("protected-legacy.pdf");
    /// </code>
    /// </example>
    public sealed class PdfSecurity
    {
        /// <summary>Password required to open the document. Empty string = no open-password.</summary>
        public string UserPassword  { get; }
        /// <summary>Password that grants full control regardless of <see cref="Permissions"/>.</summary>
        public string OwnerPassword { get; }
        /// <summary>Operations permitted when the document is opened with the user password.</summary>
        public PdfPermissions Permissions { get; }
        /// <summary>Encryption algorithm and strength. Defaults to <see cref="PdfEncryptionVersion.AES256"/>.</summary>
        public PdfEncryptionVersion EncryptionVersion { get; }

        private PdfSecurity(string user, string owner, PdfPermissions perms, PdfEncryptionVersion version)
        {
            UserPassword = user; OwnerPassword = owner; Permissions = perms; EncryptionVersion = version;
        }

        /// <summary>
        /// Create a security descriptor with the given passwords.
        /// Defaults to AES-256 (PDF 2.0) and all permissions allowed;
        /// call <see cref="WithPermissions"/> to restrict.
        /// </summary>
        /// <param name="userPassword">Password to open the file (empty = no password needed).</param>
        /// <param name="ownerPassword">Full-access password; defaults to <paramref name="userPassword"/>.</param>
        public static PdfSecurity Protect(string userPassword = "", string? ownerPassword = null)
            => new PdfSecurity(
                userPassword ?? "",
                ownerPassword ?? userPassword ?? "",
                PdfPermissions.All,
                PdfEncryptionVersion.AES256);

        /// <summary>Return a copy with the specified permissions.</summary>
        public PdfSecurity WithPermissions(PdfPermissions permissions)
            => new PdfSecurity(UserPassword, OwnerPassword, permissions, EncryptionVersion);

        /// <summary>Return a copy with a different owner password.</summary>
        public PdfSecurity WithOwnerPassword(string ownerPassword)
            => new PdfSecurity(UserPassword, ownerPassword ?? "", Permissions, EncryptionVersion);

        /// <summary>Return a copy selecting a different encryption algorithm.</summary>
        public PdfSecurity WithEncryptionVersion(PdfEncryptionVersion version)
            => new PdfSecurity(UserPassword, OwnerPassword, Permissions, version);
    }
}
