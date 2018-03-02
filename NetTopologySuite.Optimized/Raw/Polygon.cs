using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized.Raw
{
    public ref struct Polygon
    {
        public RawGeometry RawGeometry;

        public Polygon(RawGeometry rawGeometry)
        {
            if (rawGeometry.GeometryType != GeometryType.Polygon)
            {
                ThrowArgumentExceptionForNonPolygon();
            }

            this.RawGeometry = rawGeometry;
            int ringCount = this.RingCount;
            if (ringCount < 0)
            {
                ThrowArgumentExceptionForNegativeRingCount();
            }

            var rem = rawGeometry.Data.Slice(9);
            for (int i = 0; i < ringCount && rem.Length >= 4; i++)
            {
                int len = 4 + Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(rem)) * 16;
                var cs = new CoordinateSequence(rem.Slice(0, len));
                if (cs.GetCoordinate(0) != cs.GetCoordinate(cs.PointCount - 1))
                {
                    ThrowArgumentExceptionForUnclosedRing(i);
                }

                rem = rem.Slice(len);
            }

            if (rem.Length != 0)
            {
                ThrowArgumentExceptionForExcessRemainder();
            }
        }

        public int RingCount => Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(this.RawGeometry.Data.Slice(5)));

        public CoordinateSequence GetRing(int ringIndex)
        {
            var rem = this.RawGeometry.Data.Slice(9);
            for (int i = 0; i < ringIndex; i++)
            {
                rem = rem.Slice(Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(rem)) * 16 + 4);
            }

            return new CoordinateSequence(rem.Slice(0, Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(rem))));
        }

        public GeoAPI.Geometries.IPolygon ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory)
        {
            int ringCount = this.RingCount;
            if (ringCount == 0)
            {
                return factory.CreatePolygon(null, null);
            }

            var rem = this.RawGeometry.Data.Slice(9);
            var ringLength = Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(rem)) * 16 + 4;
            var shell = new CoordinateSequence(rem.Slice(0, ringLength));
            rem = rem.Slice(ringLength);

            var holes = ringCount == 1
                ? Array.Empty< GeoAPI.Geometries.ILinearRing>()
                : new GeoAPI.Geometries.ILinearRing[ringCount - 1];

            for (int i = 0; i < holes.Length; i++)
            {
                ringLength = Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(rem)) * 16 + 4;
                holes[i] = factory.CreateLinearRing(new CoordinateSequence(rem.Slice(0, ringLength)).ToGeoAPI(factory.CoordinateSequenceFactory));
                rem = rem.Slice(ringLength);
            }

            return factory.CreatePolygon(factory.CreateLinearRing(shell.ToGeoAPI(factory.CoordinateSequenceFactory)), holes);
        }

        public bool EqualsExact(Polygon other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[RingCount = {this.RingCount}]";

        private static void ThrowArgumentExceptionForNonPolygon() => throw new ArgumentException("GeometryType must be Polygon.", "rawGeometry");

        private static void ThrowArgumentExceptionForNegativeRingCount() => throw new ArgumentException("Ring count cannot be negative", "rawGeometry");

        private static void ThrowArgumentExceptionForExcessRemainder() => throw new ArgumentException("Data is janky...", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForUnclosedRing(int ringIndex) => throw new ArgumentException($"Ring {ringIndex} is not closed.", "rawGeometry");
    }
}
