using System.Collections.Generic;
using System.Reflection;
using Majorsilence.Forms;
using Majorsilence.Forms.Drawing;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Lets a host application override individual designer toolbar/menu icons while keeping the built-in
    /// raster defaults for everything it doesn't override (issue #468). Icons are keyed by a stable name
    /// (the icon's resource name, e.g. <c>"document_new"</c>, <c>"edit_cut"</c>, <c>"format_justify_left"</c>,
    /// matching the property names in <c>RdlDesign/Properties/Resources.resx</c>).
    /// </summary>
    public interface IDesignerIconProvider
    {
        /// <summary>
        /// Returns the replacement icon for <paramref name="name"/>, or <c>null</c> to keep the built-in
        /// default. Called once per icon when the provider is assigned to a designer control.
        /// </summary>
        Image GetIcon(string name);
    }

    /// <summary>
    /// Applies an <see cref="IDesignerIconProvider"/> to a designer's existing toolbars and menus, matching
    /// each <see cref="ToolStripItem"/>'s current image back to its resource name so hosts can override icons
    /// by name without the designer needing per-button bookkeeping.
    /// </summary>
    internal static class DesignerIconResolver
    {
        private static Dictionary<Image, string> _namesByImage;

        private static Dictionary<Image, string> NamesByImage()
        {
            if (_namesByImage != null)
                return _namesByImage;

            var map = new Dictionary<Image, string>();
            foreach (PropertyInfo prop in typeof(Properties.Resources).GetProperties(
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(Image).IsAssignableFrom(prop.PropertyType))
                    continue;
                if (prop.GetValue(null) is Image img && !map.ContainsKey(img))
                    map[img] = prop.Name;
            }
            _namesByImage = map;
            return map;
        }

        /// <summary>Applies <paramref name="provider"/> to every item (and dropdown item) of each toolstrip.</summary>
        public static void Apply(IDesignerIconProvider provider, params ToolStrip[] toolStrips)
        {
            if (provider == null || toolStrips == null)
                return;
            Dictionary<Image, string> names = NamesByImage();
            foreach (ToolStrip strip in toolStrips)
            {
                if (strip != null)
                    ApplyItems(provider, names, strip.Items);
            }
        }

        private static void ApplyItems(IDesignerIconProvider provider, Dictionary<Image, string> names, ToolStripItemCollection items)
        {
            if (items == null)
                return;

            foreach (ToolStripItem item in items)
            {
                if (item.Image != null && names.TryGetValue(item.Image, out string name))
                {
                    Image replacement = provider.GetIcon(name);
                    if (replacement != null)
                        item.Image = replacement;
                }

                if (item is ToolStripDropDownItem dropDown && dropDown.HasDropDownItems)
                    ApplyItems(provider, names, dropDown.DropDownItems);
            }
        }
    }
}
