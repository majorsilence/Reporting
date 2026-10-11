```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Workload      | Mean       | Error      | StdDev     | Gen0      | Gen1      | Gen2     | Allocated |
|------------------- |-------------- |-----------:|-----------:|-----------:|----------:|----------:|---------:|----------:|
| **ParseDataRenderPdf** | **MatrixExample** |   **9.169 ms** |   **2.475 ms** |  **0.1357 ms** |  **671.8750** |  **656.2500** | **250.0000** |   **4.44 MB** |
| **ParseDataRenderPdf** | **testdata**      |   **6.863 ms** |  **13.170 ms** |  **0.7219 ms** |  **375.0000** |  **312.5000** | **234.3750** |   **2.39 MB** |
| **ParseDataRenderPdf** | **WorldFacts**    | **100.511 ms** | **437.477 ms** | **23.9796 ms** | **2500.0000** | **1000.0000** | **500.0000** |  **17.09 MB** |
