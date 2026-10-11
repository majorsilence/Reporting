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
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **1000**   |    **60.37 ms** |   **9.061 ms** |  **2.353 ms** |   **3222.2222** |   **888.8889** |  **444.4444** |   **20.54 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 1000   |    62.85 ms |  56.232 ms |  3.082 ms |   3250.0000 |   750.0000 |  500.0000 |   20.54 MB |
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **10000**  |   **544.87 ms** |  **28.590 ms** |  **4.424 ms** |  **34000.0000** |  **8000.0000** | **2000.0000** |  **189.43 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 10000  |   539.08 ms |  72.207 ms |  3.958 ms |  34000.0000 |  8000.0000 | 2000.0000 |  189.43 MB |
| **TablePdf** | **Job-GUUCJL** | **5**              | **Throughput**  | **1**           | **100000** | **5,351.91 ms** |  **72.465 ms** | **11.214 ms** | **332000.0000** | **63000.0000** | **3000.0000** | **1889.01 MB** |
| TablePdf | ShortRun   | 3              | Default     | 3           | 100000 | 5,392.73 ms | 892.095 ms | 48.899 ms | 332000.0000 | 63000.0000 | 3000.0000 | 1889.01 MB |
