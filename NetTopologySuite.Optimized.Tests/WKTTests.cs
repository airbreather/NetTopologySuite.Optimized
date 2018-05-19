using System;
using System.Collections.Generic;
using System.Linq;

using GeoAPI.Geometries;
using NetTopologySuite.IO;

using Xunit;

namespace NetTopologySuite.Optimized.Tests
{
    public sealed class WKTTests
    {
        public static List<object[]> TestCases
        {
            get
            {
                Random rand = new Random(54321);
                double X() => Math.Round(rand.NextDouble() * 1000, rand.Next(6));
                double Y() => Math.Round(rand.NextDouble() * 1000, rand.Next(6));
                string Poly(int cnt) => String.Join(", ", Enumerable.Range(0, cnt).Select(_ => $"({Seq(rand.Next(4, 15), true)})"));
                string Seq(int cnt, bool? closed = null)
                {
                    bool closedVal = closed ?? rand.Next(2) == 0;

                    string ender = $"{X()} {Y()}";
                    var seq = Enumerable.Range(0, closedVal ? cnt - 2 : cnt)
                                        .Select(_ => $"{X()} {Y()}");
                    if (closedVal)
                    {
                        seq = seq.Prepend(ender).Append(ender);
                    }

                    return String.Join(", ", seq);
                }

                List<string> values = new List<string>(110);
                for (int i = 0; i < 5; i++)
                {
                    values.Add($"POINT ({X()} {Y()})");
                    values.Add($"LINESTRING ({Seq(2)})");
                    values.Add($"LINESTRING ({Seq(rand.Next(2, 10))})");
                    values.Add($"POLYGON ({Poly(1)})");
                    values.Add($"POLYGON ({Poly(rand.Next(1, 10))})");
                    values.Add($"MULTIPOINT (({X()} {Y()}))");
                    values.Add($"MULTIPOINT ({String.Join(", ", Enumerable.Range(0, rand.Next(1, 10)).Select(_ => $"({X()} {Y()})"))})");
                    values.Add($"MULTILINESTRING (({Seq(2)}))");
                    values.Add($"MULTILINESTRING ({String.Join(", ", Enumerable.Range(0, rand.Next(1, 10)).Select(_ => $"({Seq(rand.Next(2, 10))})"))})");
                    values.Add($"MULTIPOLYGON (({Poly(1)}))");
                    values.Add($"MULTIPOLYGON ({String.Join(", ", Enumerable.Range(0, rand.Next(1, 10)).Select(_ => $"({Poly(rand.Next(1, 10))})"))})");
                };

                string[] arr = values.ToArray();
                void ShuffleArray()
                {
                    // Knuth
                    for (int i = arr.Length - 1; i > 0; i--)
                    {
                        int j = rand.Next(i + 1);
                        (arr[i], arr[j]) = (arr[j], arr[i]);
                    }
                }

                for (int i = 1; i <= arr.Length; i++)
                {
                    ShuffleArray();
                    values.Add("GEOMETRYCOLLECTION (" + String.Join(", ", arr, 0, i) + ")");
                }

                return values.ConvertAll(str => new object[] { str });
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RoundTripFromCore(string expected)
        {
            IGeometry geom = new WKTReader().Read(expected);
            RawGeometry raw = new RawGeometry(geom.AsBinary());
            string actual = OptimizedWKTWriter.Write(raw);
            Assert.Equal(expected, actual);
        }
    }
}
