#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Data;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The text and JSON data providers sit behind synchronous contracts (TextReader.ReadLine, IDataReader),
    /// so they read files and HTTP responses synchronously rather than blocking on async calls.
    /// </summary>
    [TestFixture]
    public class SyncReaderTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "rdl-sync-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private string Write(string name, string text)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, text);
            return path;
        }

        [Test]
        public void MultipleStreamReader_ReadsFilesInOrder_SkippingEmptyOnes()
        {
            string a = Write("a.txt", "1\n2\n");
            string b = Write("b.txt", "");
            string c = Write("c.txt", "3");

            using var reader = new MultipleStreamReader($"{a} {b} {c}");

            Assert.That(reader.ReadLine(), Is.EqualTo("1"));
            Assert.That(reader.ReadLine(), Is.EqualTo("2"));
            Assert.That(reader.ReadLine(), Is.EqualTo("3"));
            Assert.That(reader.ReadLine(), Is.Null);
            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void MultipleStreamReader_ReadsFromHttp()
        {
            using var listener = new HttpListener();
            string prefix = $"http://localhost:{FreePort()}/";
            listener.Prefixes.Add(prefix);
            listener.Start();
            var server = Task.Run(async () =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var ctx = await listener.GetContextAsync();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(i == 0 ? "from http 1\nfrom http 2\n" : "from http 3\n");
                    await ctx.Response.OutputStream.WriteAsync(bytes);
                    ctx.Response.Close();
                }
            });

            using var reader = new MultipleStreamReader($"{prefix}one {prefix}two");

            Assert.That(reader.ReadLine(), Is.EqualTo("from http 1"));
            Assert.That(reader.ReadLine(), Is.EqualTo("from http 2"));
            Assert.That(reader.ReadLine(), Is.EqualTo("from http 3"));
            Assert.That(reader.ReadLine(), Is.Null);
            Assert.That(server.Wait(5000), Is.True);
        }

        [Test]
        public void ReadLine_FromManyThreads_GetsEveryLine()
        {
            // each reader is private to its thread; this just checks nothing is shared between them
            string a = Write("a.txt", string.Join("\n", System.Linq.Enumerable.Range(0, 200)));
            int[] counts = new int[8];
            System.Threading.Tasks.Parallel.For(0, counts.Length, t =>
            {
                using var reader = new MultipleStreamReader(a);
                while (reader.ReadLine() != null) counts[t]++;
            });
            Assert.That(counts, Is.All.EqualTo(200));
        }

        private static int FreePort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
#endif
