```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Workload      | Mean      | Error     | StdDev    | Gen0      | Gen1      | Allocated  |
|------------------- |-------------- |----------:|----------:|----------:|----------:|----------:|-----------:|
| **ParseDataRenderPdf** | **MatrixExample** |  **5.971 ms** |  **1.310 ms** | **0.0718 ms** |  **531.2500** |  **328.1250** | **2691.34 KB** |
| **ParseDataRenderPdf** | **testdata**      |  **3.385 ms** |  **3.355 ms** | **0.1839 ms** |  **218.7500** |   **70.3125** |   **872.3 KB** |
| **ParseDataRenderPdf** | **WorldFacts**    | **44.228 ms** | **11.759 ms** | **0.6446 ms** | **1666.6667** | **1000.0000** | **9526.83 KB** |
