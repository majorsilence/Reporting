# Majorsilence.Pdf.Security

Majorsilence.Pdf.Security is tri licensed under Apache-2.0, MIT, or BSD-3-Clause. Pick your choice.

- SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
- Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

---

Optional companion package for **Majorsilence.Pdf** that adds password-based encryption, certificate-based (public-key) encryption, and PKCS#7 digital signatures.

## Installation

Add a reference to both packages. `Majorsilence.Pdf.Security` only activates when you call its extension methods; there is no overhead when it is referenced but not used.

## Password encryption

```csharp
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;

PdfDocument.Create()
    .WithSecurity(PdfSecurity.Create("ownerPass", "userPass")
        .WithEncryptionVersion(PdfEncryptionVersion.AES256))   // AES-256 (default) or AES128
    .AddPage(PageSizes.A4, c => c.DrawText("Confidential", 72, 72))
    .Save("encrypted.pdf");
```

### `PdfSecurity` options

| Method | Description |
|---|---|
| `PdfSecurity.Create(ownerPassword, userPassword)` | Build an options object |
| `.WithEncryptionVersion(PdfEncryptionVersion.AES256)` | AES-256 R=6 / PDF 2.0 (default) |
| `.WithEncryptionVersion(PdfEncryptionVersion.AES128)` | AES-128 R=4 / PDF 1.4 — broader compatibility |
| `.WithPermissions(PdfPermissions.Print \| PdfPermissions.Copy)` | Restrict what users can do |

`AES256` automatically upgrades the document to `PdfVersion.Pdf20`.

## Certificate-based (public-key) encryption

Only holders of the listed X.509 certificate private keys can open the document (`/Filter /Adobe.PubSec`, V=4, AES-128).

```csharp
var recipientCert = new X509Certificate2("recipient.cer");

PdfDocument.Create()
    .WithPublicKeySecurity(
        PdfPublicKeySecurity.ForRecipients(recipientCert)
            .WithPermissions(PdfPermissions.Print))
    .AddPage(PageSizes.A4, c => c.DrawText("Eyes Only", 72, 72))
    .Save("pubkey-encrypted.pdf");
```

Multiple recipients are supported:
```csharp
.WithPublicKeySecurity(PdfPublicKeySecurity.ForRecipients(cert1, cert2, cert3))
```

## Digital signatures

Embed an invisible PKCS#7 detached signature (`/SubFilter /adbe.pkcs7.detached`):

```csharp
var cert = new X509Certificate2("signer.p12", "password",
    X509KeyStorageFlags.Exportable);

PdfDocument.Create()
    .WithSignature(new PdfSignatureOptions(cert)
        .WithReason("Approved")
        .WithLocation("Toronto")
        .WithSignerName("Jane Smith"))
    .AddPage(PageSizes.A4, c => c.DrawText("Signed document", 72, 72))
    .Save("signed.pdf");
```

### Visible signature appearance

Add a visible signature box that viewers render in the page:

```csharp
new PdfSignatureOptions(cert)
    .WithSignerName("Jane Smith")
    .WithReason("Approved")
    .WithAppearance(x: 72, y: 700, width: 200, height: 50)
```

Coordinates use top-left origin (same convention as `PdfCanvas` drawing methods). The appearance box shows a border, "Digitally Signed", and the signer name.

### RFC 3161 timestamps

Embed an authenticated timestamp so the signature remains verifiable after the signing certificate expires:

```csharp
new PdfSignatureOptions(cert)
    .WithTimestampAuthority("http://timestamp.digicert.com")
```

### `PdfSignatureOptions` builder

| Method | Description |
|---|---|
| `WithReason(string)` | Signing reason shown in viewer |
| `WithSignerName(string)` | Signer name shown in viewer |
| `WithLocation(string)` | Signing location shown in viewer |
| `WithTimestampAuthority(string url)` | RFC 3161 TSA URL for embedded timestamps |
| `WithAppearance(x, y, width, height)` | Visible signature appearance box |

## Permissions flags

`PdfPermissions` is a `[Flags]` enum:

| Value | Description |
|---|---|
| `PdfPermissions.Print` | Allow printing |
| `PdfPermissions.Modify` | Allow document modification |
| `PdfPermissions.Copy` | Allow content copying |
| `PdfPermissions.Annotate` | Allow adding annotations |
| `PdfPermissions.All` | All permissions (default) |

## Combining security features

A signature can be combined with password encryption:

```csharp
PdfDocument.Create()
    .WithSecurity(PdfSecurity.Create("owner", "user"))
    .WithSignature(new PdfSignatureOptions(cert).WithReason("Final"))
    .AddPage(PageSizes.A4, c => c.DrawText("Encrypted and signed", 72, 72))
    .Save("secured-signed.pdf");
```

**Note:** PDF/A conformance (`WithConformance`) and encryption are mutually exclusive. Attempting to combine them throws `InvalidOperationException` at save time.
