using System;
using System.Collections.Generic;

using Xunit;

namespace NetTopologySuite.Optimized.Tests
{
    public sealed class RelateMatrixTests
    {
        private static readonly Dictionary<char, IntersectionMatrixDimension[]> LegalValuesByPatternChar = new Dictionary<char, IntersectionMatrixDimension[]>
        {
            ['0'] = new[] { IntersectionMatrixDimension.Dim0 },
            ['1'] = new[] { IntersectionMatrixDimension.Dim1 },
            ['2'] = new[] { IntersectionMatrixDimension.Dim2 },
            ['F'] = new[] { IntersectionMatrixDimension.DimF },
            ['T'] = new[] { IntersectionMatrixDimension.Dim0, IntersectionMatrixDimension.Dim1, IntersectionMatrixDimension.Dim2 },
            ['*'] = new[] { IntersectionMatrixDimension.Dim0, IntersectionMatrixDimension.Dim1, IntersectionMatrixDimension.Dim2, IntersectionMatrixDimension.DimF },
        };

        [Fact]
        public void RoundTripTest()
        {
            Assert.All(GenerateAllMatchingValues("*********"), x => Assert.Equal(x, IntersectionMatrix.Parse(x.ToString())));
        }

        [Fact]
        public void IsContainsTest()
        {
            const string ContainsPattern = "T*****FF*";
            var expectedMatches = GenerateAllMatchingValues(ContainsPattern);
            Assert.All(expectedMatches, x => Assert.True(x.IsContains));

            var notExpectedMatches = GenerateAllNotMatchingValues(ContainsPattern);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsContains));
        }

        [Fact]
        public void IsCoversTest()
        {
            var expectedMatches = new HashSet<IntersectionMatrix>();
            expectedMatches.UnionWith(GenerateAllMatchingValues("T*****FF*"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("*T****FF*"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("***T**FF*"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("****T*FF*"));
            Assert.All(expectedMatches, x => Assert.True(x.IsCovers));

            var notExpectedMatches = GenerateAllMatchingValues("*********");
            notExpectedMatches.RemoveAll(expectedMatches.Contains);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsCovers));
        }

        [Fact]
        public void IsCoveredByTest()
        {
            var expectedMatches = new HashSet<IntersectionMatrix>();
            expectedMatches.UnionWith(GenerateAllMatchingValues("T*F**F***"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("*TF**F***"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("**FT*F***"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("**F*TF***"));

            Assert.All(expectedMatches, x => Assert.True(x.IsCoveredBy));

            var notExpectedMatches = GenerateAllMatchingValues("*********");
            notExpectedMatches.RemoveAll(expectedMatches.Contains);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsCoveredBy));
        }

        [Fact]
        public void IsDisjointAndIsIntersectsTest()
        {
            const string DisjointPattern = "FF*FF****";
            var expectedMatches = GenerateAllMatchingValues(DisjointPattern);
            Assert.All(expectedMatches, x => Assert.True(x.IsDisjoint && !x.IsIntersects));

            var notExpectedMatches = GenerateAllNotMatchingValues(DisjointPattern);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsDisjoint && !x.IsIntersects));
        }

        [Fact]
        public void IsEqualsTest()
        {
            const string EqualsPattern = "T*F**FFF*";
            var expectedMatches = GenerateAllMatchingValues(EqualsPattern);
            Assert.All(expectedMatches, x => Assert.True(x.IsEquals));

            var notExpectedMatches = GenerateAllNotMatchingValues(EqualsPattern);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsEquals));
        }

        [Fact]
        public void IsTouchesTest()
        {
            var expectedMatches = new HashSet<IntersectionMatrix>();
            expectedMatches.UnionWith(GenerateAllMatchingValues("FT*******"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("F**T*****"));
            expectedMatches.UnionWith(GenerateAllMatchingValues("F***T****"));

            Assert.All(expectedMatches, x => Assert.True(x.IsTouches));

            var notExpectedMatches = GenerateAllMatchingValues("*********");
            notExpectedMatches.RemoveAll(expectedMatches.Contains);

            Assert.All(notExpectedMatches, x => Assert.False(x.IsTouches));
        }

        [Fact]
        public void IsWithinTest()
        {
            const string WithinPattern = "T*F**F***";
            var expectedMatches = GenerateAllMatchingValues(WithinPattern);
            Assert.All(expectedMatches, x => Assert.True(x.IsWithin));

            var notExpectedMatches = GenerateAllNotMatchingValues(WithinPattern);
            Assert.All(notExpectedMatches, x => Assert.False(x.IsWithin));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("01201201")]
        [InlineData("0120120120")]
        [InlineData("000000003")]
        public void ParseFailureTests(string text)
        {
            Assert.Throws<FormatException>(() => IntersectionMatrix.Parse(text));
        }

        private static List<IntersectionMatrix> GenerateAllMatchingValues(ReadOnlySpan<char> pattern)
        {
            var result = new List<IntersectionMatrix>(262144);

            var d0Values = LegalValuesByPatternChar[pattern[0]];
            var d1Values = LegalValuesByPatternChar[pattern[1]];
            var d2Values = LegalValuesByPatternChar[pattern[2]];
            var d3Values = LegalValuesByPatternChar[pattern[3]];
            var d4Values = LegalValuesByPatternChar[pattern[4]];
            var d5Values = LegalValuesByPatternChar[pattern[5]];
            var d6Values = LegalValuesByPatternChar[pattern[6]];
            var d7Values = LegalValuesByPatternChar[pattern[7]];
            var d8Values = LegalValuesByPatternChar[pattern[8]];

            foreach (var d0 in d0Values)
            {
                foreach (var d1 in d1Values)
                {
                    foreach (var d2 in d2Values)
                    {
                        foreach (var d3 in d3Values)
                        {
                            foreach (var d4 in d4Values)
                            {
                                foreach (var d5 in d5Values)
                                {
                                    foreach (var d6 in d6Values)
                                    {
                                        foreach (var d7 in d7Values)
                                        {
                                            foreach (var d8 in d8Values)
                                            {
                                                result.Add(new IntersectionMatrix(d0, d1, d2, d3, d4, d5, d6, d7, d8));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static List<IntersectionMatrix> GenerateAllNotMatchingValues(ReadOnlySpan<char> pattern)
        {
            var result = new List<IntersectionMatrix>(262144);
            Span<char> invertedPattern = stackalloc char[9];
            invertedPattern.Fill('*');
            for (int i = 0; i < pattern.Length; i++)
            {
                switch (pattern[i])
                {
                    case '0':
                        invertedPattern[i] = '1';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '2';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = 'F';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '*';
                        break;

                    case '1':
                        invertedPattern[i] = '0';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '2';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = 'F';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '*';
                        break;

                    case '2':
                        invertedPattern[i] = '0';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '1';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = 'F';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '*';
                        break;

                    case 'T':
                        invertedPattern[i] = 'F';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '*';
                        break;

                    case 'F':
                        invertedPattern[i] = 'T';
                        result.AddRange(GenerateAllMatchingValues(invertedPattern));
                        invertedPattern[i] = '*';
                        break;
                }
            }

            return result;
        }
    }
}
