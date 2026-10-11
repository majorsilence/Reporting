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
| **Render** | **XML**       |  **8.566 ms** | **3.3832 ms** | **0.1854 ms** | **1296.8750** | **343.7500** |        **-** |   **4.74 MB** |
| **Render** | **CSV**       |  **5.672 ms** | **0.6359 ms** | **0.0349 ms** | **1023.4375** |        **-** |        **-** |   **3.05 MB** |
| **Render** | **RTF**       |  **7.963 ms** | **0.3580 ms** | **0.0196 ms** | **1125.0000** | **921.8750** | **234.3750** |   **6.78 MB** |
| **Render** | **Excel2007** | **15.391 ms** | **4.5222 ms** | **0.2479 ms** | **1609.3750** | **640.6250** |  **93.7500** |   **8.71 MB** |
