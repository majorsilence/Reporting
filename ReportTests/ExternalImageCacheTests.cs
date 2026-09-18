using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// An external image is asked for at least twice per report — once while the pages are
    /// laid out and again while they are drawn — and the run cache is replaced between those
    /// two passes, so every fetch used to happen twice. Where the URL names an API that
    /// renders the image on demand rather than a static file, the repeat costs
    /// as much as the original and produces the same bytes.
    /// <para>
    /// These tests count what the server actually received, because that is the thing that
    /// was wrong: the report rendered correctly throughout.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ExternalImageCacheTests
    {
        private static readonly byte[] Png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAT0lEQVR42u3OMQEAAAgDoC251a3gLWQgnbrYRRdd" +
            "dNFFF1100UUXXXTRRRdddNFFF1100UUXXXTRRRdddNFFF1100UUXXXTRRRdddNFFF1100UUXXXTRRRdddNHlWxYy0gFvKM+PTAAAAABJRU5ErkJggg==");

        private HttpListener _listener;
        private int _port;
        private int _requests;

        [SetUp]
        public void SetUp ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            _requests = 0;

            // A free port, found by asking the OS for one and handing it straight back.
            var probe = new System.Net.Sockets.TcpListener (System.Net.IPAddress.Loopback, 0);
            probe.Start ();
            _port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop ();

            _listener = new HttpListener ();
            _listener.Prefixes.Add ($"http://localhost:{_port}/");
            _listener.Start ();
            Listen ();
        }

        private void Listen ()
        {
            _ = Task.Run (async () => {
                while (_listener != null && _listener.IsListening) {
                    HttpListenerContext ctx;
                    try { ctx = await _listener.GetContextAsync (); }
                    catch { return; }   // listener stopped

                    Interlocked.Increment (ref _requests);
                    ctx.Response.ContentType = "image/png";
                    await ctx.Response.OutputStream.WriteAsync (Png, 0, Png.Length);
                    ctx.Response.Close ();
                }
            });
        }

        [TearDown]
        public void TearDown ()
        {
            var listener = _listener;
            _listener = null;
            if (listener != null) {
                listener.Stop ();
                listener.Close ();
            }
        }

        private string ImageReport (params string[] urls)
        {
            var items = string.Empty;
            for (var i = 0; i < urls.Length; i++) {
                var top = 0.2 + (i * 1.2);
                items += $@"<Image Name=""I{i}""><Source>External</Source>
                              <Value>=""{urls[i]}""</Value>
                              <Sizing>FitProportional</Sizing>
                              <Top>{top.ToString (System.Globalization.CultureInfo.InvariantCulture)}in</Top>
                              <Left>0.2in</Left><Height>1in</Height><Width>1in</Width></Image>";
            }

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width>
  <Body><Height>10in</Height><ReportItems>{items}</ReportItems></Body>
  <PageHeight>20in</PageHeight><PageWidth>8.5in</PageWidth>
</Report>";
        }

        private async Task RenderAsync (string rdl)
        {
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report, Is.Not.Null, "report failed to parse");
            await report.RunGetData (null);
            await report.BuildPages ();

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
        }

        [Test]
        public async Task AnExternalImage_IsFetchedOncePerReport ()
        {
            await RenderAsync (ImageReport ($"http://localhost:{_port}/one.png"));

            Assert.That (_requests, Is.EqualTo (1),
                "laying the page out and drawing it should share one fetch, not take one each");
        }

        [Test]
        public async Task EachDistinctUrl_IsFetchedOnce ()
        {
            await RenderAsync (ImageReport (
                $"http://localhost:{_port}/a.png",
                $"http://localhost:{_port}/b.png",
                $"http://localhost:{_port}/c.png"));

            Assert.That (_requests, Is.EqualTo (3), "three distinct images, three fetches");
        }

        /// <summary>
        /// The cache is keyed by resolved URL, so the same image used twice is fetched once —
        /// the case a report hits when every row points at the same placeholder.
        /// </summary>
        [Test]
        public async Task TheSameUrlTwice_IsFetchedOnce ()
        {
            var url = $"http://localhost:{_port}/shared.png";

            await RenderAsync (ImageReport (url, url));

            Assert.That (_requests, Is.EqualTo (1), "one URL, however many images use it");
        }

        /// <summary>
        /// The cache lives on the report, not on the type: two reports must not share images,
        /// or a long-lived process would serve one tenant's picture to another.
        /// </summary>
        [Test]
        public async Task AnotherReport_DoesNotReuseTheFirstReportsImages ()
        {
            var url = $"http://localhost:{_port}/per-report.png";

            await RenderAsync (ImageReport (url));
            await RenderAsync (ImageReport (url));

            Assert.That (_requests, Is.EqualTo (2), "each report fetches for itself");
        }
    }
}
