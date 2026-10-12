#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Data;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTests
{
    [TestFixture]
    public class AsyncDataCommandTests
    {
        /// <summary>Only supports SendAsync, like a browser: HttpClient.Send throws NotSupportedException.</summary>
        private sealed class AsyncOnlyHandler : HttpMessageHandler
        {
            public int Calls;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref Calls);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""[{"Name":"Alice","Age":30},{"Name":"Bob","Age":25}]""")
                });
            }
        }

        [Test]
        public async Task JsonCommand_ExecuteReaderAsync_FetchesWithoutSyncHttp()
        {
            var handler = new AsyncOnlyHandler();
            using var cn = new JsonConnection("file=https://example.test/people.json", new HttpClient(handler));
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "root";

            Assert.That(cmd, Is.InstanceOf<IAsyncDataCommand>());
            using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);

            int rows = 0;
            while (dr.Read()) rows++;
            Assert.That(rows, Is.EqualTo(2));
            Assert.That(handler.Calls, Is.EqualTo(1));
        }

        [Test]
        public void JsonCommand_SyncExecuteReader_StillNeedsSyncHttp()
        {
            using var cn = new JsonConnection("file=https://example.test/people.json", new HttpClient(new AsyncOnlyHandler()));
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "root";
            Assert.Throws<NotSupportedException>(() => cmd.ExecuteReader());
        }

        [Test]
        public async Task JsonCommand_ExecuteReaderAsync_ReadsLocalFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "rdl-async-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, """[{"Name":"Alice"}]""");
            try
            {
                using var cn = new JsonConnection($"file={path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "root";
                using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);
                Assert.That(dr.Read(), Is.True);
                Assert.That(dr.GetValue(dr.GetOrdinal("Name")), Is.EqualTo("Alice"));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public async Task TxtCommand_ExecuteReaderAsync_ReadsHttp()
        {
            using var listener = new HttpListener();
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            string prefix = $"http://localhost:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();
            var server = Task.Run(async () =>
            {
                var ctx = await listener.GetContextAsync();
                await ctx.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("a,b\n1,2\n3,4\n"));
                ctx.Response.Close();
            });

            using var cn = new TxtConnection("file=" + prefix);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "Url=" + prefix + ";Header=true;Separator=,";

            using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);
            int rows = 0;
            while (dr.Read()) rows++;
            Assert.That(rows, Is.EqualTo(2));
            Assert.That(dr.GetName(0), Is.EqualTo("a"));
            Assert.That(server.Wait(5000), Is.True);
        }

        private static (HttpListener listener, string prefix, Task server) Serve(params string[] bodies)
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            var listener = new HttpListener();
            string prefix = $"http://localhost:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();
            var server = Task.Run(async () =>
            {
                foreach (string body in bodies)
                {
                    var ctx = await listener.GetContextAsync();
                    await ctx.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
                    ctx.Response.Close();
                }
            });
            return (listener, prefix, server);
        }

        [Test]
        public async Task XmlCommand_ExecuteReaderAsync_ReadsHttp()
        {
            var (listener, prefix, server) = Serve("<root><item><Name>Alice</Name></item><item><Name>Bob</Name></item></root>");
            using var _ = listener;
            using var cn = new XmlConnection("file=" + prefix);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "Url=" + prefix + ";RowsXPath=/root/item";

            using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);
            int rows = 0;
            while (dr.Read()) rows++;
            Assert.That(rows, Is.EqualTo(2));
            Assert.That(server.Wait(5000), Is.True);
        }

        [Test]
        public async Task LogCommand_ExecuteReaderAsync_ReadsSeveralHttpUrls()
        {
            const string line = "127.0.0.1 - bob [10/Oct/2000:13:55:36 -0700] \"GET /a.gif HTTP/1.0\" 200 2326\n";
            var (listener, prefix, server) = Serve(line + line, line);
            using var _ = listener;
            using var cn = new LogConnection("file=" + prefix);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"Url={prefix}one {prefix}two";

            using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);
            int rows = 0;
            while (dr.Read()) rows++;
            Assert.That(rows, Is.EqualTo(3));
            Assert.That(server.Wait(5000), Is.True);
        }

        [Test]
        public async Task GedcomCommand_ExecuteReaderAsync_FetchesHttpUpFront()
        {
            var (listener, prefix, server) = Serve("0 HEAD\n0 @I1@ INDI\n1 NAME John /Smith/\n0 TRLR\n");
            using var _ = listener;
            using var cn = new GedcomConnection("file=" + prefix);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "Url=" + prefix;

            using IDataReader dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.Default);
            // the download happened in ExecuteReaderAsync, before any row was read
            Assert.That(server.Wait(5000), Is.True);
            Assert.DoesNotThrow(() => { while (dr.Read()) { } });
        }
    }
}
#endif
