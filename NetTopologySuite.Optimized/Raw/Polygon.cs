using System;

namespace NetTopologySuite.Optimized.Raw
{
    public ref struct Polygon
    {
        public RawGeometry RawGeometry;

        public Polygon(RawGeometry rawGeometry)
        {
            switch (rawGeometry.GeometryType)
            {
                case GeometryType.Polygon:
                    break;

                default:
                    throw new ArgumentException("GeometryType must be Polygon.", nameof(rawGeometry));
            }

            this.RawGeometry = rawGeometry;
            int ringCount = this.RingCount;
            if (ringCount < 0)
            {
                throw new ArgumentException("Ring count cannot be negative", nameof(rawGeometry));
            }

            var rem = rawGeometry.Data.Slice(9);
            for (int i = 0; i < ringCount && rem.Length >= 4; i++)
            {
                int len = 4 + rem.NonPortableCast<byte, int>()[0] * 16;
                var cs = new CoordinateSequence(rem.Slice(0, len));
                if (cs.Coordinates[0] != cs.Coordinates[cs.Coordinates.Length - 1])
                {
                    throw new ArgumentException($"Ring {i} is not closed.", nameof(rawGeometry));
                }

                rem = rem.Slice(len);
            }

            if (rem.Length != 0)
            {
                throw new ArgumentException("Data is janky...", nameof(rawGeometry));
            }
        }

        public int RingCount => this.RawGeometry.Data.Slice(5).NonPortableCast<byte, int>()[0];

        public CoordinateSequence GetRing(int ringIndex)
        {
            var rem = this.RawGeometry.Data.Slice(9);
            for (int i = 0; i < ringIndex; i++)
            {
                rem = rem.Slice(rem.NonPortableCast<byte, int>()[0] * 16 + 4);
            }

            return new CoordinateSequence(rem.Slice(0, rem.NonPortableCast<byte, int>()[0]));
        }

        public GeoAPI.Geometries.IPolygon ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory)
        {
            int ringCount = this.RingCount;
            if (ringCount == 0)
            {
                return factory.CreatePolygon(null, null);
            }

            var rem = this.RawGeometry.Data.Slice(9);
            var ringLength = rem.NonPortableCast<byte, int>()[0] * 16 + 4;
            var shell = new CoordinateSequence(rem.Slice(0, ringLength));
            rem = rem.Slice(ringLength);

            var holes = ringCount == 1
                ? Array.Empty< GeoAPI.Geometries.ILinearRing>()
                : new GeoAPI.Geometries.ILinearRing[ringCount - 1];

            for (int i = 0; i < holes.Length; i++)
            {
                ringLength = rem.NonPortableCast<byte, int>()[0] * 16 + 4;
                holes[i] = factory.CreateLinearRing(new CoordinateSequence(rem.Slice(0, ringLength)).ToGeoAPI(factory.CoordinateSequenceFactory));
                rem = rem.Slice(ringLength);
            }

            return factory.CreatePolygon(factory.CreateLinearRing(shell.ToGeoAPI(factory.CoordinateSequenceFactory)), holes);
        }

        public bool EqualsExact(Polygon other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[RingCount = {this.RingCount}]";
    }
}
