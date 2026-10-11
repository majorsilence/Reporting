```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method     | Workload      | Mean     | Error     | StdDev  | Gen0     | Gen1    | Gen2    | Allocated |
|----------- |-------------- |---------:|----------:|--------:|---------:|--------:|--------:|----------:|
| **RunGetData** | **MatrixExample** | **172.8 μs** |  **20.96 μs** | **1.15 μs** |  **38.0859** |       **-** |       **-** | **116.82 KB** |
| **RunGetData** | **WorldFacts**    | **846.3 μs** | **129.71 μs** | **7.11 μs** | **117.1875** | **54.6875** | **31.2500** | **540.71 KB** |
