``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.248)
Intel Core i7-6850K CPU 3.60GHz (Skylake), 1 CPU, 12 logical cores and 6 physical cores
Frequency=3515627 Hz, Resolution=284.4443 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-ILRHIX : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Job-HDOCVB : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT

Server=True  

```
|                                   Method | Runtime |        Mean |      Error |     StdDev | Scaled | ScaledSD |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------------------------------- |-------- |------------:|-----------:|-----------:|-------:|---------:|---------:|---------:|---------:|----------:|
|                    ReadDirectlyFromArray |     Clr |    40.62 us |  0.0185 us |  0.0164 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|                   ReadNTSCoordinateArray |     Clr | 9,035.17 us | 22.9902 us | 20.3802 us | 222.42 |     0.49 | 656.2500 | 468.7500 | 171.8750 | 3843686 B |
|                      ReadNTSPackedDouble |     Clr | 4,768.08 us |  8.9315 us |  8.3546 us | 117.37 |     0.20 | 328.1250 | 328.1250 | 328.1250 | 1284727 B |
|                       ReadNTSPackedFloat |     Clr | 4,621.80 us | 10.8097 us |  9.5825 us | 113.77 |     0.23 | 179.6875 | 179.6875 | 179.6875 |  643891 B |
|                         ReadOptimizedAOS |     Clr |   714.95 us | 17.3446 us | 51.1409 us |  17.60 |     1.25 | 333.0078 | 333.0078 | 333.0078 | 1282832 B |
|                  ReadOptimizedSOA_Scalar |     Clr | 2,621.11 us | 17.0743 us | 15.1359 us |  64.52 |     0.36 | 332.0313 | 332.0313 | 332.0313 | 1282930 B |
|                  ReadOptimizedSOA_Vector |     Clr | 2,588.59 us | 14.9710 us | 14.0039 us |  63.72 |     0.33 | 332.0313 | 332.0313 | 332.0313 | 1282958 B |
| ReadOptimizedRaw_Visitor_Straightforward |     Clr |   976.00 us |  0.0602 us |  0.0435 us |  24.03 |     0.01 |        - |        - |        - |      32 B |
| ReadOptimizedRaw_Visitor_NonPortableCast |     Clr |    78.80 us |  0.0422 us |  0.0330 us |   1.94 |     0.00 |        - |        - |        - |      25 B |
|         ReadOptimizedRaw_Straightforward |     Clr |   861.39 us |  0.0847 us |  0.0612 us |  21.20 |     0.01 |        - |        - |        - |       0 B |
|         ReadOptimizedRaw_NonPortableCast |     Clr |    79.20 us |  0.0384 us |  0.0360 us |   1.95 |     0.00 |        - |        - |        - |       0 B |
|          ReadOptimizedRaw_SkipValidation |     Clr |    78.29 us |  0.0203 us |  0.0159 us |   1.93 |     0.00 |        - |        - |        - |       0 B |
|                    ReadOptimizedRaw_Refs |     Clr |    42.86 us |  0.0222 us |  0.0185 us |   1.06 |     0.00 |        - |        - |        - |       0 B |
|                                          |         |             |            |            |        |          |          |          |          |           |
|                    ReadDirectlyFromArray |    Core |    40.88 us |  0.0187 us |  0.0165 us |   1.00 |     0.00 |        - |        - |        - |       0 B |
|                   ReadNTSCoordinateArray |    Core | 5,093.32 us | 64.9243 us | 60.7302 us | 124.60 |     1.44 |  23.4375 |  23.4375 |  23.4375 | 3842307 B |
|                      ReadNTSPackedDouble |    Core | 4,474.35 us | 15.8196 us | 14.0236 us | 109.46 |     0.33 | 328.1250 | 328.1250 | 328.1250 | 1282640 B |
|                       ReadNTSPackedFloat |    Core | 4,215.13 us | 10.5594 us |  9.3606 us | 103.12 |     0.22 | 179.6875 | 179.6875 | 179.6875 |  642640 B |
|                         ReadOptimizedAOS |    Core |   786.39 us | 14.9528 us | 13.9868 us |  19.24 |     0.33 | 286.1328 | 286.1328 | 286.1328 | 1283315 B |
|                  ReadOptimizedSOA_Scalar |    Core | 1,067.39 us | 21.0604 us | 30.2043 us |  26.11 |     0.73 | 332.0313 | 332.0313 | 332.0313 | 1282734 B |
|                  ReadOptimizedSOA_Vector |    Core | 1,061.42 us | 19.7867 us | 18.5085 us |  25.97 |     0.44 | 335.9375 | 335.9375 | 335.9375 | 1282777 B |
| ReadOptimizedRaw_Visitor_Straightforward |    Core |   330.66 us |  1.6701 us |  1.5622 us |   8.09 |     0.04 |        - |        - |        - |      24 B |
| ReadOptimizedRaw_Visitor_NonPortableCast |    Core |    44.43 us |  0.0161 us |  0.0143 us |   1.09 |     0.00 |        - |        - |        - |      24 B |
|         ReadOptimizedRaw_Straightforward |    Core |   205.47 us |  1.1332 us |  1.0600 us |   5.03 |     0.03 |        - |        - |        - |       0 B |
|         ReadOptimizedRaw_NonPortableCast |    Core |    42.17 us |  0.0218 us |  0.0170 us |   1.03 |     0.00 |        - |        - |        - |       0 B |
|          ReadOptimizedRaw_SkipValidation |    Core |    41.79 us |  0.0241 us |  0.0201 us |   1.02 |     0.00 |        - |        - |        - |       0 B |
|                    ReadOptimizedRaw_Refs |    Core |    41.95 us |  0.0140 us |  0.0117 us |   1.03 |     0.00 |        - |        - |        - |       0 B |
