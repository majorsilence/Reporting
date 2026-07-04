using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms;

namespace Majorsilence.WinformUtils;

// Majorsilence.Forms.Form does not derive from ContainerControl/Control (see WaitForm.cs for the
// full explanation), so the original single ContainerControl-typed overload that special-cased
// "is this actually a Form" no longer type-checks -- Form and ContainerControl share no
// inheritance relationship here, so `parent is Form` on a ContainerControl parameter is now a
// compile error (an impossible cast), not just a runtime false. Split into two overloads instead.
public static class FormExtensions
{
    private static bool isOpen = false;
    private static WaitForm? _current;

    public static void ShowWaiter(this UserControl parent)
    {
        if (parent.FindForm() is Form form)
        {
            form.ShowWaiter();
        }
    }

    public static void ShowWaiter(this Form parent)
    {
        if (isOpen) return;

        isOpen = true;
        var waitForm = new WaitForm(() => new Rectangle(parent.Location, parent.Size))
        {
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            Owner = parent,
        };
        _current = waitForm;

        waitForm.Show();
        waitForm.BringToFront();
        Application.DoEvents();
    }

    public static void ShowWaiter(this ContainerControl parent)
    {
        if (parent.FindForm() is Form form)
        {
            form.ShowWaiter();
        }
    }

    public static void HideWaiter(this UserControl parent)
    {
        if (parent.FindForm() is Form form)
        {
            form.HideWaiter();
        }
    }

    public static void HideWaiter(this Form parent)
    {
        var waitForm = parent.OwnedForms.OfType<WaitForm>().FirstOrDefault() ?? _current;
        if (waitForm != null)
        {
            waitForm.Close();
            waitForm.Dispose();
            if (ReferenceEquals(_current, waitForm)) _current = null;
        }

        isOpen = false;
    }

    public static void HideWaiter(this ContainerControl parent)
    {
        if (parent.FindForm() is Form form)
        {
            form.HideWaiter();
        }
    }
}
