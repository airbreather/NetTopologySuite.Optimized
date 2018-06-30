using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized
{
    /// <summary>
    /// A DE-9IM matrix that represents the relationship between two geographies or geometries.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct IntersectionMatrix : IEquatable<IntersectionMatrix>
    {
        internal static readonly Dictionary<char, IntersectionMatrixDimension[]> LegalValuesByPatternChar = new Dictionary<char, IntersectionMatrixDimension[]>
        {
            ['0'] = new[] { IntersectionMatrixDimension.Dim0 },
            ['1'] = new[] { IntersectionMatrixDimension.Dim1 },
            ['2'] = new[] { IntersectionMatrixDimension.Dim2 },
            ['F'] = new[] { IntersectionMatrixDimension.DimF },
            ['T'] = new[] { IntersectionMatrixDimension.Dim0, IntersectionMatrixDimension.Dim1, IntersectionMatrixDimension.Dim2 },
            ['*'] = new[] { IntersectionMatrixDimension.Dim0, IntersectionMatrixDimension.Dim1, IntersectionMatrixDimension.Dim2, IntersectionMatrixDimension.DimF },
        };

        private const int InteriorBits = 0;

        private const int BoundaryBits = 2;

        private const int ExteriorBits = 4;

        private const int InteriorMask = 3 << InteriorBits;

        private const int BoundaryMask = 3 << BoundaryBits;

        private const int ExteriorMask = 3 << ExteriorBits;

        [FieldOffset(0)]
        private readonly byte booleanPatternBits;

        [FieldOffset(1)]
        private readonly byte interiorBits;

        [FieldOffset(2)]
        private readonly byte boundaryBits;

        [FieldOffset(3)]
        private readonly byte exteriorBits;

        [FieldOffset(0)]
        private readonly int packed;

        public IntersectionMatrix(
            IntersectionMatrixDimension interiorInterior,
            IntersectionMatrixDimension interiorBoundary,
            IntersectionMatrixDimension interiorExterior,
            IntersectionMatrixDimension boundaryInterior,
            IntersectionMatrixDimension boundaryBoundary,
            IntersectionMatrixDimension boundaryExterior,
            IntersectionMatrixDimension exteriorInterior,
            IntersectionMatrixDimension exteriorBoundary,
            IntersectionMatrixDimension exteriorExterior)
        {
            if ((interiorInterior | interiorBoundary | interiorExterior | boundaryInterior | boundaryBoundary | boundaryExterior | exteriorInterior | exteriorBoundary | exteriorExterior) > IntersectionMatrixDimension.Dim2)
            {
                throw new ArgumentException("All arguments must be either DimF, Dim0, Dim1, or Dim2.");
            }

            this.packed = 0;
            this.booleanPatternBits = (byte)
            (
                (interiorInterior == 0 ? 0 : 1 << 7) |
                (interiorBoundary == 0 ? 0 : 1 << 6) |
                (interiorExterior == 0 ? 0 : 1 << 5) |
                (boundaryInterior == 0 ? 0 : 1 << 4) |
                (boundaryBoundary == 0 ? 0 : 1 << 3) |
                (boundaryExterior == 0 ? 0 : 1 << 2) |
                (exteriorInterior == 0 ? 0 : 1 << 1) |
                (exteriorBoundary == 0 ? 0 : 1 << 0)
            );
            this.interiorBits = (byte)(((byte)interiorInterior << InteriorBits) | ((byte)interiorBoundary << BoundaryBits) | ((byte)interiorExterior << ExteriorBits));
            this.boundaryBits = (byte)(((byte)boundaryInterior << InteriorBits) | ((byte)boundaryBoundary << BoundaryBits) | ((byte)boundaryExterior << ExteriorBits));
            this.exteriorBits = (byte)(((byte)exteriorInterior << InteriorBits) | ((byte)exteriorBoundary << BoundaryBits) | ((byte)exteriorExterior << ExteriorBits));
        }

        private IntersectionMatrix(ReadOnlySpan<IntersectionMatrixDimension> dims) => this = new IntersectionMatrix(dims[0], dims[1], dims[2], dims[3], dims[4], dims[5], dims[6], dims[7], dims[8]);

        public static IntersectionMatrix Parse(ReadOnlySpan<char> text)
        {
            if (text.Length != 9)
            {
                ThrowFormatExceptionForBadValueFormat();
            }

            Span<IntersectionMatrixDimension> dims = stackalloc IntersectionMatrixDimension[9];
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '0':
                        dims[i] = IntersectionMatrixDimension.Dim0;
                        break;

                    case '1':
                        dims[i] = IntersectionMatrixDimension.Dim1;
                        break;

                    case '2':
                        dims[i] = IntersectionMatrixDimension.Dim2;
                        break;

                    case 'F':
                        dims[i] = IntersectionMatrixDimension.DimF;
                        break;

                    default:
                        ThrowFormatExceptionForBadValueFormat();
                        break;
                }
            }

            return new IntersectionMatrix(dims);
        }

        public IntersectionMatrixDimension InteriorInterior => (IntersectionMatrixDimension)((this.interiorBits & InteriorMask) >> InteriorBits);

        public IntersectionMatrixDimension InteriorBoundary => (IntersectionMatrixDimension)((this.interiorBits & BoundaryMask) >> BoundaryBits);

        public IntersectionMatrixDimension InteriorExterior => (IntersectionMatrixDimension)((this.interiorBits & ExteriorMask) >> ExteriorBits);

        public IntersectionMatrixDimension BoundaryInterior => (IntersectionMatrixDimension)((this.boundaryBits & InteriorMask) >> InteriorBits);

        public IntersectionMatrixDimension BoundaryBoundary => (IntersectionMatrixDimension)((this.boundaryBits & BoundaryMask) >> BoundaryBits);

        public IntersectionMatrixDimension BoundaryExterior => (IntersectionMatrixDimension)((this.boundaryBits & ExteriorMask) >> ExteriorBits);

        public IntersectionMatrixDimension ExteriorInterior => (IntersectionMatrixDimension)((this.exteriorBits & InteriorMask) >> InteriorBits);

        public IntersectionMatrixDimension ExteriorBoundary => (IntersectionMatrixDimension)((this.exteriorBits & BoundaryMask) >> BoundaryBits);

        public IntersectionMatrixDimension ExteriorExterior => (IntersectionMatrixDimension)((this.exteriorBits & ExteriorMask) >> ExteriorBits);

        // T*****FF*
        public bool IsContains => (this.booleanPatternBits & 0b10000011) == 0b10000000;

        // T*F**F***
        public bool IsWithin => (this.booleanPatternBits & 0b10100100) == 0b10000000;

        // T*F**FFF*
        public bool IsEquals => (this.booleanPatternBits & 0b10100111) == 0b10000000;

        // T********
        // *T*******
        // ***T*****
        // ****T****
        public bool IsIntersects => (this.booleanPatternBits & 0b11011000) != 0;

        // FF*FF****
        public bool IsDisjoint => (this.booleanPatternBits & 0b11011000) == 0;

        // T*****FF*
        // *T****FF*
        // ***T**FF*
        // ****T*FF*
        public bool IsCovers
        {
            get
            {
                byte relevantBits = unchecked((byte)(this.booleanPatternBits & 0b11011011));
                return relevantBits != 0 && (((relevantBits & 0b00000011)) == 0);
            }
        }

        // T*F**F***
        // *TF**F***
        // **FT*F***
        // **F*TF***
        public bool IsCoveredBy
        {
            get
            {
                byte relevantBits = unchecked((byte)(this.booleanPatternBits & 0b11111100));
                return relevantBits != 0 && (((relevantBits & 0b00100100)) == 0);
            }
        }

        // FT*******
        // F**T*****
        // F***T****
        public bool IsTouches
        {
            get
            {
                byte relevantBits = unchecked((byte)(this.booleanPatternBits & 0b11011000));
                return relevantBits != 0 && ((relevantBits & 0b10000000) == 0);
            }
        }

        public void CopyDimensionsTo(Span<IntersectionMatrixDimension> span)
        {
            if (span.Length != 9)
            {
                throw new ArgumentException("Must have room for exactly 9 dimension values.", nameof(span));
            }

            span[0] = this.InteriorInterior;
            span[1] = this.InteriorBoundary;
            span[2] = this.InteriorExterior;
            span[3] = this.BoundaryInterior;
            span[4] = this.BoundaryBoundary;
            span[5] = this.BoundaryExterior;
            span[6] = this.ExteriorInterior;
            span[7] = this.ExteriorBoundary;
            span[8] = this.ExteriorExterior;
        }

        public void CopyStandardFormatTo(Span<char> span)
        {
            if (span.Length != 9)
            {
                throw new ArgumentException("Must have room for exactly 9 characters.", nameof(span));
            }

            span[0] = CharOf(this.InteriorInterior);
            span[1] = CharOf(this.InteriorBoundary);
            span[2] = CharOf(this.InteriorExterior);
            span[3] = CharOf(this.BoundaryInterior);
            span[4] = CharOf(this.BoundaryBoundary);
            span[5] = CharOf(this.BoundaryExterior);
            span[6] = CharOf(this.ExteriorInterior);
            span[7] = CharOf(this.ExteriorBoundary);
            span[8] = CharOf(this.ExteriorExterior);

            char CharOf(IntersectionMatrixDimension dim)
            {
                switch (dim)
                {
                    case IntersectionMatrixDimension.DimF:
                        return 'F';

                    case IntersectionMatrixDimension.Dim0:
                        return '0';

                    case IntersectionMatrixDimension.Dim1:
                        return '1';

                    default:
                        return '2';
                }
            }
        }

        public bool Matches(ReadOnlySpan<char> pattern)
        {
            if (pattern.Length != 9)
            {
                ThrowFormatExceptionForBadPatternFormat();
            }

            Span<IntersectionMatrixDimension> values = stackalloc IntersectionMatrixDimension[9];
            this.CopyDimensionsTo(values);

            bool result = true;
            for (int i = 0; i < pattern.Length; i++)
            {
                switch (pattern[i])
                {
                    case '0':
                        result &= values[i] == IntersectionMatrixDimension.Dim0;
                        break;

                    case '1':
                        result &= values[i] == IntersectionMatrixDimension.Dim1;
                        break;

                    case '2':
                        result &= values[i] == IntersectionMatrixDimension.Dim2;
                        break;

                    case 'F':
                        result &= values[i] == IntersectionMatrixDimension.DimF;
                        break;

                    case 'T':
                        result &= values[i] != IntersectionMatrixDimension.DimF;
                        break;

                    case '*':
                        break;

                    default:
                        ThrowFormatExceptionForBadPatternFormat();
                        break;
                }
            }

            return result;
        }

        public bool MatchesSimpleBooleanPattern(byte patternBits, byte maskBits) => (this.booleanPatternBits & maskBits) == patternBits;

        public override bool Equals(object obj) => obj is IntersectionMatrix other && this.Equals(other);

        public bool Equals(IntersectionMatrix other) => this.packed == other.packed;

        public override int GetHashCode() => this.packed;

        public override string ToString()
        {
            string result = new string('\0', 9);
            unsafe
            {
                fixed (char* p = result)
                {
                    this.CopyStandardFormatTo(new Span<char>(p, 9));
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatExceptionForBadPatternFormat() => throw new FormatException("Must contain exactly 9 characters, each of which must be either '0', '1', '2', 'F', 'T', or '*'.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatExceptionForBadValueFormat() => throw new FormatException("Must contain exactly 9 characters, each of which must be either '0', '1', '2', or 'F'.");
    }
}
