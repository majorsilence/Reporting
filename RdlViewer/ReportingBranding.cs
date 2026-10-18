using Majorsilence.Reporting.Rdl.Utility;

namespace Majorsilence.Reporting.RdlViewer
{
    /// <summary>
    /// What the designer and viewer call themselves and link to, for an application that ships them under
    /// its own name (white-labelling). Set the values on <see cref="Current"/> at startup, before creating
    /// any designer or viewer.
    /// </summary>
    /// <remarks>
    /// A property left null keeps the built-in, localised text. The fyiReporting licence notice in the
    /// About box is always shown: the licence requires it, so <see cref="AboutText"/> is shown above it
    /// rather than in its place.
    /// </remarks>
    public sealed class ReportingBranding
    {
        /// <summary>The branding in effect.</summary>
        public static ReportingBranding Current { get; set; } = new ReportingBranding();

        /// <summary>The product name, e.g. in the heading of the viewer's error report. Default "Majorsilence Reporting".</summary>
        public string ProductName { get; set; }

        /// <summary>The designer's name: its window title, message box captions and About box.</summary>
        public string DesignerName { get; set; }

        /// <summary>The designer window's icon.</summary>
        public Majorsilence.Forms.Drawing.Icon Icon { get; set; }

        /// <summary>Help > Help opens this.</summary>
        public string HelpUrl { get; set; }

        /// <summary>Help > Support, and the About box's support link.</summary>
        public string SupportUrl { get; set; }

        /// <summary>The About box's website link.</summary>
        public string WebsiteUrl { get; set; }

        /// <summary>Text shown in the About box above the licence notice, e.g. the application's own copyright.</summary>
        public string AboutText { get; set; }

        /// <summary>Whether Help > About is offered. Default true.</summary>
        public bool ShowAboutBox { get; set; } = true;

        /// <summary>
        /// The folder under the user's application-data folders holding settings and caches (default
        /// "MajorsilenceReporting"), so a white-labelled application does not share them with the stock designer.
        /// </summary>
        public string SettingsFolderName
        {
            get => Paths.ApplicationFolderName;
            set => Paths.ApplicationFolderName = value;
        }

        /// <summary>
        /// The name the stand-alone designer uses to find an already running copy of itself (default
        /// "RdlDesigner"), so a white-labelled build and the stock one do not hand files to each other.
        /// </summary>
        public string InstanceName { get; set; } = "RdlDesigner";

        internal const string DefaultProductName = "Majorsilence Reporting";
        internal const string DefaultHelpUrl = "https://github.com/majorsilence/Reporting/wiki/_pages";
        internal const string DefaultSupportUrl = "https://github.com/majorsilence/Reporting/discussions";
        internal const string DefaultWebsiteUrl = "https://github.com/majorsilence/Reporting";

        /// <summary><see cref="ProductName"/>, or the default.</summary>
        public string EffectiveProductName => string.IsNullOrWhiteSpace(ProductName) ? DefaultProductName : ProductName;

        /// <summary><see cref="HelpUrl"/>, or the default.</summary>
        public string EffectiveHelpUrl => string.IsNullOrWhiteSpace(HelpUrl) ? DefaultHelpUrl : HelpUrl;

        /// <summary><see cref="SupportUrl"/>, or the default.</summary>
        public string EffectiveSupportUrl => string.IsNullOrWhiteSpace(SupportUrl) ? DefaultSupportUrl : SupportUrl;

        /// <summary><see cref="WebsiteUrl"/>, or the default.</summary>
        public string EffectiveWebsiteUrl => string.IsNullOrWhiteSpace(WebsiteUrl) ? DefaultWebsiteUrl : WebsiteUrl;
    }
}
