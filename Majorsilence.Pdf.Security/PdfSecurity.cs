// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Password-based security settings for a PDF document.
    /// Uses Standard Security Handler Revision 4 with AES-128-CBC encryption.
    /// </summary>
    /// <example>
    /// <code>
    /// using Majorsilence.Pdf.Security;
    ///
    /// PdfDocument.Create()
    ///     .WithSecurity(PdfSecurity.Protect("open123", ownerPassword: "owner456")
    ///                              .WithPermissions(PdfPermissions.Print))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Hello", 72, 100))
    ///     .Save("protected.pdf");
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

        private PdfSecurity(string user, string owner, PdfPermissions perms)
        {
            UserPassword = user; OwnerPassword = owner; Permissions = perms;
        }

        /// <summary>
        /// Create a security descriptor with the given passwords.
        /// Defaults to all permissions allowed; call <see cref="WithPermissions"/> to restrict.
        /// </summary>
        /// <param name="userPassword">Password to open the file (empty = no password needed).</param>
        /// <param name="ownerPassword">Full-access password; defaults to <paramref name="userPassword"/>.</param>
        public static PdfSecurity Protect(string userPassword = "", string? ownerPassword = null)
            => new PdfSecurity(userPassword ?? "", ownerPassword ?? userPassword ?? "", PdfPermissions.All);

        /// <summary>Return a copy with the specified permissions.</summary>
        public PdfSecurity WithPermissions(PdfPermissions permissions)
            => new PdfSecurity(UserPassword, OwnerPassword, permissions);

        /// <summary>Return a copy with a different owner password.</summary>
        public PdfSecurity WithOwnerPassword(string ownerPassword)
            => new PdfSecurity(UserPassword, ownerPassword ?? "", Permissions);
    }
}
