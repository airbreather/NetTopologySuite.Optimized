``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.248)
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical cores and 6 physical cores
Frequency=3515618 Hz, Resolution=284.4450 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-PBLKUC : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Job-THTAKQ : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT

Server=True  

```
|                           Method | Runtime |         Mean |       Error |      StdDev |       Median | Scaled | ScaledSD |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------------------- |-------- |-------------:|------------:|------------:|-------------:|-------:|---------:|---------:|---------:|---------:|----------:|
|            ReadDirectlyFromArray |     Clr |     40.61 us |   0.0640 us |   0.0599 us |     40.58 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|           ReadNTSCoordinateArray |     Clr | 10,248.48 us |  91.5520 us |  66.1981 us | 10,260.63 us | 252.39 |     1.60 | 656.2500 | 468.7500 | 171.8750 | 3843558 B |
|              ReadNTSPackedDouble |     Clr |  7,346.76 us |  13.3433 us |  12.4814 us |  7,350.68 us | 180.93 |     0.39 | 343.7500 | 343.7500 | 343.7500 | 1284744 B |
|               ReadNTSPackedFloat |     Clr |  4,613.58 us |   9.6136 us |   8.5222 us |  4,613.15 us | 113.62 |     0.26 | 187.5000 | 187.5000 | 187.5000 |  644039 B |
|                 ReadOptimizedAOS |     Clr |    689.75 us |  14.1185 us |  41.6288 us |    675.06 us |  16.99 |     1.02 | 333.0078 | 333.0078 | 333.0078 | 1282840 B |
|          ReadOptimizedSOA_Scalar |     Clr |  2,580.68 us |  10.3127 us |   9.6465 us |  2,576.60 us |  63.55 |     0.25 | 332.0313 | 332.0313 | 332.0313 | 1282930 B |
|          ReadOptimizedSOA_Vector |     Clr |  2,586.98 us |  16.1375 us |  14.3055 us |  2,584.80 us |  63.71 |     0.35 | 332.0313 | 332.0313 | 332.0313 | 1282945 B |
| ReadOptimizedRaw_Straightforward |     Clr |    859.88 us |   0.3022 us |   0.2360 us |    859.91 us |  21.18 |     0.03 |        - |        - |        - |       0 B |
| ReadOptimizedRaw_NonPortableCast |     Clr |     79.02 us |   0.0601 us |   0.0562 us |     79.02 us |   1.95 |     0.00 |        - |        - |        - |       0 B |
|  ReadOptimizedRaw_SkipValidation |     Clr |     78.35 us |   0.0263 us |   0.0246 us |     78.36 us |   1.93 |     0.00 |        - |        - |        - |       0 B |
|            ReadOptimizedRaw_Refs |     Clr |     42.97 us |   0.0432 us |   0.0361 us |     42.96 us |   1.06 |     0.00 |        - |        - |        - |       0 B |
|                                  |         |              |             |             |              |        |          |          |          |          |           |
|            ReadDirectlyFromArray |    Core |     40.67 us |   0.0309 us |   0.0289 us |     40.67 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|           ReadNTSCoordinateArray |    Core |  5,385.15 us | 106.8277 us | 195.3406 us |  5,382.31 us | 132.41 |     4.75 |  23.4375 |  15.6250 |  15.6250 | 3842307 B |
|              ReadNTSPackedDouble |    Core |  4,408.59 us |  13.7119 us |  11.4501 us |  4,408.40 us | 108.40 |     0.28 | 328.1250 | 328.1250 | 328.1250 | 1282640 B |
|               ReadNTSPackedFloat |    Core |  4,157.32 us |  11.9805 us |  10.0043 us |  4,157.76 us | 102.22 |     0.25 | 179.6875 | 179.6875 | 179.6875 |  642640 B |
|                 ReadOptimizedAOS |    Core |    782.85 us |   9.0164 us |   8.4340 us |    783.68 us |  19.25 |     0.20 | 282.2266 | 282.2266 | 282.2266 | 1283450 B |
|          ReadOptimizedSOA_Scalar |    Core |  1,063.42 us |  17.3276 us |  15.3604 us |  1,062.48 us |  26.15 |     0.36 | 332.0313 | 332.0313 | 332.0313 | 1283074 B |
|          ReadOptimizedSOA_Vector |    Core |  1,093.61 us |   6.4813 us |   5.0602 us |  1,094.38 us |  26.89 |     0.12 | 328.1250 | 328.1250 | 328.1250 | 1282935 B |
| ReadOptimizedRaw_Straightforward |    Core |    226.84 us |   4.5253 us |  12.1569 us |    225.11 us |   5.58 |     0.30 |        - |        - |        - |       0 B |
| ReadOptimizedRaw_NonPortableCast |    Core |     42.34 us |   0.0292 us |   0.0228 us |     42.34 us |   1.04 |     0.00 |        - |        - |        - |       0 B |
|  ReadOptimizedRaw_SkipValidation |    Core |     42.14 us |   0.0399 us |   0.0333 us |     42.14 us |   1.04 |     0.00 |        - |        - |        - |       0 B |
|            ReadOptimizedRaw_Refs |    Core |     41.64 us |   0.0394 us |   0.0329 us |     41.64 us |   1.02 |     0.00 |        - |        - |        - |       0 B |
