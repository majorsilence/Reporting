```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Core i5-8350U CPU 1.70GHz (Max: 0.40GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-GUUCJL : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

LaunchCount=1  

```
| Method   | Job        | IterationCount | RunStrategy | WarmupCount | Rows   | Mean        | Error      | StdDev    | Gen0        | Gen1       | Gen2      | Allocated  |
|--------- |----------- |--------------- |------------ |------------ |------- |------------:|-----------:|----------:|------------:|-----------:|----------:|-----------:|
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **1000**   |    **39.19 ms** |   **2.218 ms** |  **0.576 ms** |   **1750.0000** |   **583.3333** |  **333.3333** |   **10.74 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 1000   |    42.59 ms |  12.497 ms |  0.685 ms |   1800.0000 |   800.0000 |  200.0000 |   10.74 MB |
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **10000**  |   **411.83 ms** |   **8.717 ms** |  **1.349 ms** |  **18000.0000** |  **6000.0000** | **2000.0000** |  **101.71 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 10000  |   413.93 ms |  70.129 ms |  3.844 ms |  18000.0000 |  6000.0000 | 2000.0000 |   101.7 MB |
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **100000** | **4,119.84 ms** | **123.333 ms** | **32.029 ms** | **172000.0000** | **55000.0000** | **3000.0000** | **1012.03 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 100000 | 4,219.61 ms | 157.013 ms |  8.606 ms | 172000.0000 | 55000.0000 | 3000.0000 | 1012.03 MB |
