using Avalonia.Controls;
using Avalonia.Interactivity;
using Majorsilence.Pdf.Security;

namespace Majorsilence.Reporting.UI.RdlAvalonia.Viewer;

public partial class PdfSecurityDialog : Window
{
    public PdfSecurityDialog(PdfSecurity? initial = null)
    {
        InitializeComponent();

        ProtectCheck.IsCheckedChanged += (_, _) =>
            SecurityPanel.IsVisible = ProtectCheck.IsChecked == true;

        OkButton.Click     += OkButtonOnClick;
        CancelButton.Click += (_, _) => Close(null);

        if (initial != null)
            Prefill(initial);
    }

    private void Prefill(PdfSecurity s)
    {
        ProtectCheck.IsChecked       = true;
        SecurityPanel.IsVisible      = true;
        UserPasswordBox.Text         = s.UserPassword;
        OwnerPasswordBox.Text        = s.OwnerPassword;
        AllowPrintCheck.IsChecked    = s.Permissions.HasFlag(PdfPermissions.Print);
        AllowHiResPrintCheck.IsChecked = s.Permissions.HasFlag(PdfPermissions.PrintHighQuality);
        AllowCopyCheck.IsChecked     = s.Permissions.HasFlag(PdfPermissions.CopyText);
        AllowModifyCheck.IsChecked   = s.Permissions.HasFlag(PdfPermissions.ModifyContent);
        AllowAnnotateCheck.IsChecked = s.Permissions.HasFlag(PdfPermissions.AddAnnotations);
        AllowFillFormsCheck.IsChecked = s.Permissions.HasFlag(PdfPermissions.FillForms);
        AllowAssembleCheck.IsChecked = s.Permissions.HasFlag(PdfPermissions.Assemble);
        EncryptionCombo.SelectedIndex = s.EncryptionVersion == PdfEncryptionVersion.AES128 ? 1 : 0;
    }

    private void OkButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (ProtectCheck.IsChecked != true)
        {
            Close((PdfSecurity?)null);
            return;
        }

        var userPwd = UserPasswordBox.Text ?? string.Empty;
        if (string.IsNullOrEmpty(userPwd))
        {
            ValidationText.Text      = "Enter a user password, or uncheck password protection.";
            ValidationText.IsVisible = true;
            return;
        }

        ValidationText.IsVisible = false;

        PdfPermissions perms = PdfPermissions.None;
        if (AllowPrintCheck.IsChecked == true)     perms |= PdfPermissions.Print;
        if (AllowHiResPrintCheck.IsChecked == true) perms |= PdfPermissions.PrintHighQuality;
        if (AllowCopyCheck.IsChecked == true)      perms |= PdfPermissions.CopyText | PdfPermissions.ExtractText;
        if (AllowModifyCheck.IsChecked == true)    perms |= PdfPermissions.ModifyContent;
        if (AllowAnnotateCheck.IsChecked == true)  perms |= PdfPermissions.AddAnnotations;
        if (AllowFillFormsCheck.IsChecked == true) perms |= PdfPermissions.FillForms;
        if (AllowAssembleCheck.IsChecked == true)  perms |= PdfPermissions.Assemble;

        var version = EncryptionCombo.SelectedIndex == 1
            ? PdfEncryptionVersion.AES128
            : PdfEncryptionVersion.AES256;

        var ownerPwd = OwnerPasswordBox.Text;
        var security = PdfSecurity
            .Protect(userPwd, string.IsNullOrEmpty(ownerPwd) ? null : ownerPwd)
            .WithPermissions(perms)
            .WithEncryptionVersion(version);

        Close(security);
    }
}
