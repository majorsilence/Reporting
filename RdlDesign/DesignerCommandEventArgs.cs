using System;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Cancellable event args for a designer command a host application can intercept (issue #469).
    /// Setting <see cref="Handled"/> to <c>true</c> skips the designer's built-in handler for this
    /// invocation, so a host can show its own UI instead of the native dialog. Leaving it <c>false</c>
    /// (the default, and the behavior with no subscribers at all) runs the designer's own handler
    /// exactly as before this event existed.
    /// </summary>
    public class DesignerCommandEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }

    /// <summary>Cancellable event args for a dataset-specific designer command (issue #469).</summary>
    public class DesignerDataSetCommandEventArgs : DesignerCommandEventArgs
    {
        /// <summary>
        /// The dataset's current name. Holds the "New..." menu item's own display text (not an actual
        /// dataset name) when the host chose "Data Sets &gt; New..." rather than an existing dataset,
        /// matching how the built-in handler itself tells the two cases apart.
        /// </summary>
        public string DataSetName { get; }

        public DesignerDataSetCommandEventArgs(string dataSetName)
        {
            DataSetName = dataSetName;
        }
    }
}
