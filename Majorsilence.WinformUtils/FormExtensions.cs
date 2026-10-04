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
        // Close EVERY WaitForm, not just the first. This used to take
        // parent.OwnedForms.OfType<WaitForm>().FirstOrDefault(), which returns the same (earliest)
        // instance on every call -- so a wait dialog that failed to close once was never retried
        // and every later one leaked. Found in ReportDesigner: a stack of black "N Seconds" windows
        // over a preview that had already finished.
        foreach (var wf in parent.OwnedForms.OfType<WaitForm>().ToArray())
            CloseAndDispose(wf);

        if (_current != null)
            CloseAndDispose(_current);

        _current = null;
        isOpen = false;
    }

    private static void CloseAndDispose(WaitForm wf)
    {
        try { wf.Close(); } catch { /* already gone */ }
        try { wf.Dispose(); } catch { /* already gone */ }
    }

    public static void HideWaiter(this ContainerControl parent)
    {
        if (parent.FindForm() is Form form)
        {
            form.HideWaiter();
        }
    }
}
