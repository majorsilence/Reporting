using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;
using Majorsilence.Reporting.RdlCreator;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Benchmarks
{
    [AsciiDocExporter]
    [HtmlExporter]
    [MemoryDiagnoser]
    //[RPlotExporter]
    [SimpleJob(RunStrategy.Throughput)]
    public class JsonDataProviderBenchmark
    {
        DataProviders dataProvider = DataProviders.Json;
        private readonly string connectionString =
            "file=" + Path.Combine(AppContext.BaseDirectory, "NestedJsonData.json");
        
        [Benchmark]
        public async Task NestedJson()
        {
            var create = new RdlCreator.Create();

            var fyiReport = await create.GenerateRdl(dataProvider,
                connectionString,
                "columns=EmployeeID,LastName,FirstName,ContactInfo_Phone,ContactInfo_Email",
                pageHeaderText: "DataProviderTest TestMethod1");
            using var ms = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
            await fyiReport.RunGetData();
            await fyiReport.RunRender(ms, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
        }
    }
}