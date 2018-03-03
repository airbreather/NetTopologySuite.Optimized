using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized.Raw
{
    public ref struct CoordinateSequence
    {
        public ReadOnlySpan<byte> PointData;

        public CoordinateSequence(ReadOnlySpan<byte> pointData)
        {
            this.PointData = pointData;
            int pointCountByLength = Math.DivRem(pointData.Length - 4, 16, out int remainder);
            if (remainder != 0 || pointCountByLength != this.PointCount)
            {
                ThrowArgumentExceptionForInconsistentLengths();
            }
        }

        public int PointCount => Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(this.PointData));

        public GeoAPI.Geometries.ICoordinateSequence ToGeoAPI(GeoAPI.Geometries.ICoordinateSequenceFactory factory)
        {
            var pts = this.PointData.Slice(4);
            var len = pts.Length / 16;
            var seq = factory.Create(len, 2);
            ref var ptsStart = ref MemoryMarshal.GetReference(pts);
            for (int i = 0, j = 0; i < len; i++, j += 16)
            {
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.X, Unsafe.ReadUnaligned<double>(ref Unsafe.AddByteOffset(ref ptsStart, new IntPtr(j + 0))));
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.Y, Unsafe.ReadUnaligned<double>(ref Unsafe.AddByteOffset(ref ptsStart, new IntPtr(j + 8))));
            }

            return seq;
        }

        public Coordinate GetCoordinate(int idx) => Unsafe.ReadUnaligned<Coordinate>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(this.PointData), new IntPtr(idx * 16 + 4)));

        public bool EqualsExact(CoordinateSequence other) => this.PointData.SequenceEqual(other.PointData);

        public override string ToString() => $"[PointCount = {this.PointCount}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForInconsistentLengths() => throw new ArgumentException("Input data length is inconsistent with claimed point count.", "pointData");

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> rem;

            private Coordinate current;

            internal Enumerator(CoordinateSequence seq)
            {
                this.rem = seq.PointData.Slice(4);
                this.current = default;
            }

            public Coordinate Current => this.current;

            public bool MoveNext()
            {
                if (this.rem.Length == 0)
                {
                    return false;
                }

                this.current = Unsafe.ReadUnaligned<Coordinate>(ref MemoryMarshal.GetReference(this.rem));
                this.rem = this.rem.Slice(16);
                return true;
            }
        }
    }
}
