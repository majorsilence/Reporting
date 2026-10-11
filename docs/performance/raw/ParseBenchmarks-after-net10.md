```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Workload      | Mean       | Error       | StdDev    | Gen0     | Gen1     | Allocated  |
|------- |-------------- |-----------:|------------:|----------:|---------:|---------:|-----------:|
| **Parse**  | **ChartTypes**    | **1,702.3 μs** | **5,492.14 μs** | **301.04 μs** | **203.1250** | **109.3750** | **1005.65 KB** |
| **Parse**  | **MatrixExample** |   **684.8 μs** | **2,970.02 μs** | **162.80 μs** | **109.3750** |  **31.2500** |  **374.64 KB** |
| **Parse**  | **testdata**      |   **171.6 μs** |   **150.52 μs** |   **8.25 μs** |  **41.0156** |        **-** |  **125.78 KB** |
| **Parse**  | **WorldFacts**    |   **261.6 μs** |    **40.55 μs** |   **2.22 μs** |  **67.3828** |        **-** |   **208.3 KB** |
