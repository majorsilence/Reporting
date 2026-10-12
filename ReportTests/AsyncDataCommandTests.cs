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
    }
}
#endif
