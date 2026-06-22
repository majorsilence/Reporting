#if NET8_0_OR_GREATER
using NUnit.Framework;
using System.Globalization;
using EngineStrings = Majorsilence.Reporting.RdlEngine.Resources.Strings;

namespace ReportTests
{
    /// <summary>
    /// Issue #237: add Spanish (es / es-MX) internationalization.
    /// Verifies the engine's runtime messages (Strings.es.resx) load via the
    /// satellite assembly, and that es-MX correctly falls back to generic es.
    /// </summary>
    [TestFixture]
    public class SpanishLocalizationTest
    {
        [Test]
        public void EngineStrings_HaveSpanishTranslation()
        {
            var rm = EngineStrings.ResourceManager;

            string en = rm.GetString("Parser_ErrorP_ScopeMustConstant", CultureInfo.InvariantCulture);
            string es = rm.GetString("Parser_ErrorP_ScopeMustConstant", new CultureInfo("es"));

            Assert.That(en, Is.EqualTo("{0} function's scope must be a constant."));
            Assert.That(es, Is.EqualTo("El ámbito de la función {0} debe ser una constante."),
                "Spanish satellite resource did not load");
            Assert.That(es, Is.Not.EqualTo(en));
        }

        [Test]
        public void EngineStrings_EsMX_FallsBackToGenericSpanish()
        {
            var rm = EngineStrings.ResourceManager;

            string esMX = rm.GetString("ProcessReport_Error_ReportHasErrors", new CultureInfo("es-MX"));

            Assert.That(esMX, Is.EqualTo("El informe contiene errores. No se puede procesar."),
                "es-MX did not fall back to the generic es resources");
        }

        [Test]
        public void EngineStrings_SpanishCoversAllKeys()
        {
            var rm = EngineStrings.ResourceManager;
            var en = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            var es = new CultureInfo("es");

            foreach (System.Collections.DictionaryEntry entry in en)
            {
                string key = (string)entry.Key;
                string esValue = rm.GetString(key, es);
                Assert.That(esValue, Is.Not.Null.And.Not.Empty,
                    $"Missing Spanish translation for key '{key}'");
            }
        }
    }
}
#endif
