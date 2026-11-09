using Majorsilence.Reporting.RdlDesign.Syntax;
using System.Drawing;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    public static class  ScintillaXMLStyle
    {
        public static void ConfigureScintillaStyle(Majorsilence.Reporting.RdlDesign.Syntax.Scintilla scintilla)
        {
            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();

            // Set the XML Lexer
            scintilla.Lexer = Lexer.Xml;

            // Show line numbers
            scintilla.Margins[0].Width = 40;

            // Enable folding
            scintilla.SetProperty("fold", "1");
            scintilla.SetProperty("fold.compact", "1");
            scintilla.SetProperty("fold.html", "1");

            // Use Margin 2 for fold markers
            scintilla.Margins[2].Type = MarginType.Symbol;
            scintilla.Margins[2].Mask = Marker.MaskFolders;
            scintilla.Margins[2].Sensitive = true;
            scintilla.Margins[2].Width = 20;

            // Reset folder markers
            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
            {
                scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
            }

            // Style the folder markers
            scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.Folder].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderEnd].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            scintilla.AutomaticFold = AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change;

            // Set the Styles
            scintilla.StyleResetDefault();
            // I like fixed font for XML
            scintilla.Styles[Style.Default].Font = "Courier";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();
            // Each colour follows the theme (#527): the classic colour on a light theme, a lighter shade
            // on a dark one, or the theme's --rdl-syntax-* variable.
            scintilla.Styles[Style.Xml.Attribute].ThemedForeColor = () => SyntaxColors.Attribute(Color.Red);
            scintilla.Styles[Style.Xml.Entity].ThemedForeColor = () => SyntaxColors.Entity(Color.Red);
            scintilla.Styles[Style.Xml.Comment].ThemedForeColor = () => SyntaxColors.Comment(Color.Green);
            scintilla.Styles[Style.Xml.Tag].ThemedForeColor = () => SyntaxColors.Tag(Color.Blue);
            scintilla.Styles[Style.Xml.TagEnd].ThemedForeColor = () => SyntaxColors.Tag(Color.Blue);
            scintilla.Styles[Style.Xml.DoubleString].ThemedForeColor = () => SyntaxColors.String(Color.DeepPink);
            scintilla.Styles[Style.Xml.SingleString].ThemedForeColor = () => SyntaxColors.String(Color.DeepPink);
        }
    }
}
