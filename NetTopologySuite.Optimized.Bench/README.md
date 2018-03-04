``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.248)
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical cores and 6 physical cores
Frequency=3515620 Hz, Resolution=284.4448 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-WEFBGH : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Job-YSXYZH : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT

Server=True  

```
|                           Method | Runtime |        Mean |      Error |     StdDev |      Median | Scaled | ScaledSD |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------------------- |-------- |------------:|-----------:|-----------:|------------:|-------:|---------:|---------:|---------:|---------:|----------:|
|            ReadDirectlyFromArray |     Clr |    40.56 us |  0.0408 us |  0.0361 us |    40.56 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|           ReadNTSCoordinateArray |     Clr | 9,033.51 us | 56.5966 us | 50.1714 us | 9,015.13 us | 222.72 |     1.21 | 656.2500 | 468.7500 | 171.8750 | 3843558 B |
|              ReadNTSPackedDouble |     Clr | 4,776.98 us | 11.5944 us |  9.6819 us | 4,778.09 us | 117.77 |     0.25 | 328.1250 | 328.1250 | 328.1250 | 1284690 B |
|               ReadNTSPackedFloat |     Clr | 4,630.77 us | 15.9781 us | 13.3424 us | 4,627.30 us | 114.17 |     0.33 | 179.6875 | 179.6875 | 179.6875 |  643906 B |
|                 ReadOptimizedAOS |     Clr |   696.52 us | 17.3113 us | 51.0427 us |   679.66 us |  17.17 |     1.25 | 333.9844 | 333.0078 | 333.0078 | 1282840 B |
|          ReadOptimizedSOA_Scalar |     Clr | 2,609.46 us |  6.9847 us |  5.4532 us | 2,607.28 us |  64.34 |     0.14 | 332.0313 | 332.0313 | 332.0313 | 1282935 B |
|          ReadOptimizedSOA_Vector |     Clr | 2,587.13 us | 16.3804 us | 14.5208 us | 2,586.44 us |  63.78 |     0.35 | 332.0313 | 332.0313 | 332.0313 | 1282930 B |
| ReadOptimizedRaw_Straightforward |     Clr |   861.51 us |  0.2575 us |  0.2282 us |   861.51 us |  21.24 |     0.02 |        - |        - |        - |       0 B |
| ReadOptimizedRaw_NonPortableCast |     Clr |    79.16 us |  0.0603 us |  0.0564 us |    79.17 us |   1.95 |     0.00 |        - |        - |        - |       0 B |
|  ReadOptimizedRaw_SkipValidation |     Clr |    78.31 us |  0.0534 us |  0.0499 us |    78.30 us |   1.93 |     0.00 |        - |        - |        - |       0 B |
|                                  |         |             |            |            |             |        |          |          |          |          |           |
|            ReadDirectlyFromArray |    Core |    40.63 us |  0.0304 us |  0.0285 us |    40.63 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|           ReadNTSCoordinateArray |    Core | 5,382.99 us | 59.5569 us | 55.7095 us | 5,355.34 us | 132.49 |     1.33 |  23.4375 |  23.4375 |  23.4375 | 3842307 B |
|              ReadNTSPackedDouble |    Core | 4,410.43 us | 27.4371 us | 25.6647 us | 4,408.72 us | 108.55 |     0.61 | 328.1250 | 328.1250 | 328.1250 | 1282640 B |
|               ReadNTSPackedFloat |    Core | 4,159.27 us | 11.3183 us |  8.8366 us | 4,156.65 us | 102.37 |     0.22 | 179.6875 | 179.6875 | 179.6875 |  642640 B |
|                 ReadOptimizedAOS |    Core |   780.55 us | 10.9260 us | 10.2202 us |   783.13 us |  19.21 |     0.24 | 285.1563 | 285.1563 | 285.1563 | 1283391 B |
|          ReadOptimizedSOA_Scalar |    Core | 1,070.46 us | 15.8780 us | 14.8523 us | 1,071.55 us |  26.35 |     0.35 | 339.8438 | 339.8438 | 339.8438 | 1282717 B |
|          ReadOptimizedSOA_Vector |    Core | 1,037.22 us | 22.9880 us | 23.6070 us | 1,031.54 us |  25.53 |     0.56 | 335.9375 | 335.9375 | 335.9375 | 1282808 B |
| ReadOptimizedRaw_Straightforward |    Core |   233.85 us |  1.4831 us |  1.3147 us |   233.63 us |   5.76 |     0.03 |        - |        - |        - |       0 B |
| ReadOptimizedRaw_NonPortableCast |    Core |    44.86 us |  0.0335 us |  0.0313 us |    44.86 us |   1.10 |     0.00 |        - |        - |        - |       0 B |
|  ReadOptimizedRaw_SkipValidation |    Core |    44.55 us |  0.0484 us |  0.0429 us |    44.54 us |   1.10 |     0.00 |        - |        - |        - |       0 B |
