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
                throw new ArgumentException("Input data length is inconsistent with claimed point count.", nameof(pointData));
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
    }
}
