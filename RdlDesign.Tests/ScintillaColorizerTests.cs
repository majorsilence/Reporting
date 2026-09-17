// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using Majorsilence.Reporting.RdlDesign.Syntax;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // D5: verifies the ScintillaCompat shim's real syntax coloring end to end -- RdlScriptLexer
    // and ScintillaExprStyle are unchanged from the mechanical D4 migration; only Scintilla's
    // StartStyling/SetStyling/GetEndStyled (now backed by a real span buffer) and its Colorizer
    // wiring (feeding those spans to Majorsilence.Forms.TextBox.Colorizer) are D5's changes.
    [TestFixture]
    public class ScintillaColorizerTests
    {
        [Test]
        public void ExpressionField_ColorsIdentifierSpan()
        {
            var scintilla = new Scintilla();
            var lexer = new RdlScriptLexer();
            lexer.SetFields(new[] { "Name" });
            lexer.SetParameters(System.Array.Empty<string>());
            new ScintillaExprStyle(lexer, scintilla).ConfigureScintillaStyle();

            scintilla.Text = "=Fields!Name.Value";

            var spans = scintilla.Colorizer(scintilla.Text).ToList();

            Assert.That(spans, Is.Not.Empty, "expected at least one colored span for a valid field expression");
            Assert.That(spans.Any(s => s.Start == 1 && s.Length == "Fields!Name.Value".Length),
                "expected a single span covering the whole 'Fields!Name.Value' identifier");
        }

        [Test]
        public void NonExpressionText_ProducesNoSpans()
        {
            // RdlScriptLexer.StyleText bails out immediately unless the text starts with '=' --
            // plain (non-expression) RichTextBox/Scintilla text must stay uncolored.
            var scintilla = new Scintilla();
            var lexer = new RdlScriptLexer();
            lexer.SetFields(System.Array.Empty<string>());
            lexer.SetParameters(System.Array.Empty<string>());
            new ScintillaExprStyle(lexer, scintilla).ConfigureScintillaStyle();

            scintilla.Text = "just some plain text";

            var spans = scintilla.Colorizer(scintilla.Text).ToList();

            Assert.That(spans, Is.Empty);
        }

        [Test]
        public void UnknownField_IsStyledAsError()
        {
            var scintilla = new Scintilla();
            var lexer = new RdlScriptLexer();
            lexer.SetFields(new[] { "Name" });
            lexer.SetParameters(System.Array.Empty<string>());
            new ScintillaExprStyle(lexer, scintilla).ConfigureScintillaStyle();

            scintilla.Text = "=Fields!DoesNotExist.Value";

            var spans = scintilla.Colorizer(scintilla.Text).ToList();

            // Error style is configured red by ScintillaExprStyle.ConfigureScintillaStyle.
            Assert.That(spans, Has.Some.Matches<Majorsilence.Forms.TextSpanStyle>(s => s.Color == SkiaSharp.SKColors.Red));
        }

        [Test]
        public void WithoutConfiguredStyle_DoesNotThrow()
        {
            // A Scintilla with no ScintillaExprStyle attached (SQL/XML editors never call
            // ConfigureScintillaStyle) must stay a safe no-op, not throw.
            var scintilla = new Scintilla { Text = "=1+1" };

            Assert.DoesNotThrow(() => scintilla.Colorizer(scintilla.Text).ToList());
        }
    }
}
