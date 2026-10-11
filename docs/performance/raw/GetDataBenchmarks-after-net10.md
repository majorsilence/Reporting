```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method     | Workload      | Mean     | Error     | StdDev   | Gen0     | Gen1    | Gen2    | Allocated |
|----------- |-------------- |---------:|----------:|---------:|---------:|--------:|--------:|----------:|
| **RunGetData** | **MatrixExample** | **171.0 μs** |   **2.40 μs** |  **0.13 μs** |  **38.0859** |       **-** |       **-** | **116.82 KB** |
| **RunGetData** | **WorldFacts**    | **859.8 μs** | **336.19 μs** | **18.43 μs** | **121.0938** | **54.6875** | **31.2500** |  **540.7 KB** |
