using Majorsilence.Reporting.RdlDesign.Syntax;
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

            scintilla.Styles[(int)RdlScriptLexer.Style.Default].ForeColor = Color.Black;
            scintilla.Styles[(int)RdlScriptLexer.Style.Identifier].ForeColor = Color.Black;
            scintilla.Styles[(int)RdlScriptLexer.Style.Error].ForeColor = Color.Red;
            scintilla.Styles[(int)RdlScriptLexer.Style.Error].Underline = true;
            scintilla.Styles[(int)RdlScriptLexer.Style.Number].ForeColor = Color.OrangeRed;
            scintilla.Styles[(int)RdlScriptLexer.Style.String].ForeColor = Color.Brown;
            scintilla.Styles[(int)RdlScriptLexer.Style.Method].ForeColor = Color.Blue;
            scintilla.Styles[(int)RdlScriptLexer.Style.AggrMethod].ForeColor = Color.Blue;
            scintilla.Styles[(int)RdlScriptLexer.Style.AggrMethod].Bold = true;
            scintilla.Styles[(int)RdlScriptLexer.Style.UserInfo].ForeColor = Color.BlueViolet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Globals].ForeColor = Color.BlueViolet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Parameter].ForeColor = Color.Violet;
            scintilla.Styles[(int)RdlScriptLexer.Style.Field].ForeColor = Color.DodgerBlue;
        }

        void scintilla_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            var startPos = scintilla.GetEndStyled();
            var endPos = e.Position;

            rdlLexer.StyleText(scintilla, startPos, endPos);
        }
    }
}
