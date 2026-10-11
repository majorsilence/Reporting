```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Format    | Mean      | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated |
|------- |---------- |----------:|----------:|----------:|----------:|---------:|---------:|----------:|
| **Render** | **XML**       |  **8.690 ms** | **1.0964 ms** | **0.0601 ms** | **1296.8750** | **343.7500** |        **-** |   **4.74 MB** |
| **Render** | **CSV**       |  **5.671 ms** | **0.3950 ms** | **0.0217 ms** | **1023.4375** |        **-** |        **-** |   **3.05 MB** |
| **Render** | **RTF**       |  **7.916 ms** | **1.3888 ms** | **0.0761 ms** | **1000.0000** | **984.3750** | **234.3750** |   **6.78 MB** |
| **Render** | **Excel2007** | **15.491 ms** | **0.2721 ms** | **0.0149 ms** | **1531.2500** | **500.0000** | **109.3750** |   **8.71 MB** |
