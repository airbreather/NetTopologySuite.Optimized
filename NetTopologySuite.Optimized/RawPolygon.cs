using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawPolygon
    {
        public RawGeometry RawGeometry;

        public RawPolygon(RawGeometry rawGeometry)
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
                int len = 4 + MemoryMarshal.Read<int>(rem) * 16;
                var cs = new RawCoordinateSequence(rem.Slice(0, len));
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

        public int RingCount => MemoryMarshal.Read<int>(this.RawGeometry.Data.Slice(5));

        public RawCoordinateSequence GetRing(int ringIndex)
        {
            var rem = this.RawGeometry.Data.Slice(9);
            for (int i = 0; i < ringIndex; i++)
            {
                rem = rem.Slice(MemoryMarshal.Read<int>(rem) * 16 + 4);
            }

            RawCoordinateSequence result = default;
            result.PointData = rem.Slice(0, MemoryMarshal.Read<int>(rem) * 16 + 4);
            return result;
        }

        public GeoAPI.Geometries.IPolygon ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory)
        {
            int ringCount = this.RingCount;
            if (ringCount == 0)
            {
                return factory.CreatePolygon(null, null);
            }

            var rem = this.RawGeometry.Data.Slice(9);
            var ringLength = MemoryMarshal.Read<int>(rem) * 16 + 4;
            RawCoordinateSequence shell = default;
            shell.PointData = rem.Slice(0, ringLength);
            rem = rem.Slice(ringLength);

            var holes = ringCount == 1
                ? Array.Empty< GeoAPI.Geometries.ILinearRing>()
                : new GeoAPI.Geometries.ILinearRing[ringCount - 1];

            for (int i = 0; i < holes.Length; i++)
            {
                ringLength = MemoryMarshal.Read<int>(rem) * 16 + 4;
                RawCoordinateSequence ring = default;
                ring.PointData = rem.Slice(0, ringLength);
                holes[i] = factory.CreateLinearRing(ring.ToGeoAPI(factory.CoordinateSequenceFactory));
                rem = rem.Slice(ringLength);
            }

            return factory.CreatePolygon(factory.CreateLinearRing(shell.ToGeoAPI(factory.CoordinateSequenceFactory)), holes);
        }

        public bool EqualsExact(RawPolygon other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[RingCount = {this.RingCount}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNonPolygon() =>
            throw new ArgumentException("GeometryType must be Polygon.", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNegativeRingCount() =>
            throw new ArgumentException("Ring count cannot be negative", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForExcessRemainder() =>
            throw new ArgumentException("Data is janky...", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForUnclosedRing(int ringIndex) =>
            throw new ArgumentException($"Ring {ringIndex} is not closed.", "rawGeometry");

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> rem;

            private RawCoordinateSequence current;

            internal Enumerator(RawPolygon poly) => this.rem = poly.RawGeometry.Data.Slice(9);

            public RawCoordinateSequence Current =>
                this.current;

            public bool MoveNext()
            {
                if (this.rem.Length == 0)
                {
                    return false;
                }

                this.current.PointData = this.rem.Slice(0, MemoryMarshal.Read<int>(this.rem) * 16 + 4);
                this.rem = this.rem.Slice(this.current.PointData.Length);
                return true;
            }
        }
    }
}
