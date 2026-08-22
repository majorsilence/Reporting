// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.
//
// System.Drawing.Design.UITypeEditor / System.Windows.Forms.Design.IWindowsFormsEditorService are
// part of the classic WinForms Designer/PropertyGrid infrastructure -- they only exist in the
// Microsoft.WindowsDesktop.App reference assemblies gated by UseWindowsForms, which this project
// no longer sets. RdlProperties/*.cs (PropertyBorder, PropertyFilters, PropertyGrouping, etc.)
// use `[Editor(typeof(XyzUIEditor), typeof(UITypeEditor))]` attributes with custom UITypeEditor
// subclasses to show a "..." button in the PropertyGrid that opens a modal sub-editor.
// Majorsilence.Forms.PropertyGrid has no support for Editor attributes at all (confirmed: no
// UITypeEditor/EditorAttribute handling in its source), so these custom editors won't actually be
// invocable through the grid regardless -- this is a real, documented gap for D6 (Designer
// parity) to address, not something this stub fixes. This stub exists purely so the *attributes*
// and *editor classes* keep compiling unchanged; recreating the same namespace names is legal
// since the real Windows-only assemblies aren't referenced here.

namespace System.Drawing.Design
{
    public enum UITypeEditorEditStyle
    {
        None,
        Modal,
        DropDown
    }

    public abstract class UITypeEditor
    {
        public virtual UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext? context)
            => UITypeEditorEditStyle.None;

        public virtual object? EditValue(System.ComponentModel.ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
            => value;
    }
}

namespace Majorsilence.Forms.Design
{
    // Real IWindowsFormsEditorService shows a dropdown/modal editor UI hosted by the PropertyGrid.
    // Majorsilence.Forms.PropertyGrid never requests this service (no Editor-attribute support),
    // so no implementation is ever actually handed to EditValue's `provider` -- callers already
    // null-check `provider.GetService(typeof(IWindowsFormsEditorService))` and fall back to
    // `base.EditValue(...)`, so this interface only needs to exist for the cast to compile.
    //
    // Namespaced as Majorsilence.Forms.Design (not System.Windows.Forms.Design) because the
    // migrator's built-in System.Windows.Forms -> Majorsilence.Forms prefix rule already rewrote
    // every `using System.Windows.Forms.Design;` to `using Majorsilence.Forms.Design;` in the
    // source files -- this stub has to live where the rewritten code actually looks for it.
    public interface IWindowsFormsEditorService
    {
        void DropDownControl(Majorsilence.Forms.Control control);
        Majorsilence.Forms.DialogResult ShowDialog(Majorsilence.Forms.Form dialog);
        void CloseDropDown();
    }
}
