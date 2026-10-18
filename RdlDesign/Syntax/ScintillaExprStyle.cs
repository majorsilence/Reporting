using Majorsilence.Reporting.RdlDesign.Syntax;
using Majorsilence.Reporting.RdlViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    public class ScintillaExprStyle
    {
        private readonly RdlScriptLexer rdlLexer;
        private readonly Scintilla scintilla;

        public ScintillaExprStyle(RdlScriptLexer rdlLexer, Scintilla scintilla)
        {
            this.rdlLexer = rdlLexer ?? throw new ArgumentNullException(nameof(rdlLexer));
            this.scintilla = scintilla ?? throw new ArgumentNullException(nameof(scintilla));
        }

        public void ConfigureScintillaStyle()
        {
            var selectionColor = Color.FromArgb(255, 192, 192, 192);
            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.StyleClearAll();

            // Set the XML Lexer
            scintilla.Lexer = Lexer.Container;
            scintilla.StyleNeeded += scintilla_StyleNeeded;

            // The classic palette is unreadable on a dark editor background; swap in lighter shades there.
            bool dark = ChromeColors.IsDarkTheme;
            scintilla.Styles[(int)RdlScriptLexer.Style.Default].ForeColor = ChromeColors.Text;
            scintilla.Styles[(int)RdlScriptLexer.Style.Identifier].ForeColor = ChromeColors.Text;
            scintilla.Styles[(int)RdlScriptLexer.Style.Error].ForeColor = dark ? Color.Tomato : Color.Red;
            scintilla.Styles[(int)RdlScriptLexer.Style.Error].Underline = true;
            scintilla.Styles[(int)RdlScriptLexer.Style.Number].ForeColor = dark ? Color.LightSalmon : Color.OrangeRed;
            scintilla.Styles[(int)RdlScriptLexer.Style.String].ForeColor = dark ? Color.SandyBrown : Color.Brown;
            scintilla.Styles[(int)RdlScriptLexer.Style.Method].ForeColor = dark ? Color.CornflowerBlue : Color.Blue;
            scintilla.Styles[(int)RdlScriptLexer.Style.AggrMethod].ForeColor = dark ? Color.CornflowerBlue : Color.Blue;
            scintilla.Styles[(int)RdlScriptLexer.Style.AggrMethod].Bold = true;
            scintilla.Styles[(int)RdlScriptLexer.Style.UserInfo].ForeColor = dark ? Color.MediumPurple : Color.BlueViolet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Globals].ForeColor = dark ? Color.MediumPurple : Color.BlueViolet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Parameter].ForeColor = Color.Violet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Field].ForeColor = dark ? Color.DeepSkyBlue : Color.DodgerBlue;
        }

        void scintilla_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            var startPos = scintilla.GetEndStyled();
            var endPos = e.Position;

            rdlLexer.StyleText(scintilla, startPos, endPos);
        }
    }
}
