```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Workload      | Mean       | Error      | StdDev    | Gen0     | Gen1     | Allocated  |
|------- |-------------- |-----------:|-----------:|----------:|---------:|---------:|-----------:|
| **Parse**  | **ChartTypes**    | **1,857.7 μs** | **5,840.5 μs** | **320.14 μs** | **218.7500** | **109.3750** | **1005.68 KB** |
| **Parse**  | **MatrixExample** |   **495.0 μs** |   **192.2 μs** |  **10.53 μs** | **113.2813** |  **33.2031** |  **374.53 KB** |
| **Parse**  | **testdata**      |   **170.0 μs** |   **257.8 μs** |  **14.13 μs** |  **41.0156** |        **-** |  **125.78 KB** |
| **Parse**  | **WorldFacts**    |   **300.4 μs** |   **100.8 μs** |   **5.53 μs** |  **66.4063** |        **-** |  **208.34 KB** |
