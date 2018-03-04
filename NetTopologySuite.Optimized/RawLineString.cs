using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawLineString
    {
        public RawGeometry RawGeometry;

        public RawLineString(RawGeometry rawGeometry)
        {
            if (rawGeometry.GeometryType != GeometryType.LineString)
            {
                ThrowArgumentExceptionForNonLineString();
            }

            this.RawGeometry = rawGeometry;
            new RawCoordinateSequence(this.RawGeometry.Data.Slice(5));
        }

        public RawCoordinateSequence Coordinates
        {
            get
            {
                RawCoordinateSequence result = default;
                result.PointData = this.RawGeometry.Data.Slice(5);
                return result;
            }
        }

        public GeoAPI.Geometries.ILineString ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory, bool ring = false)
        {
            var seq = this.Coordinates.ToGeoAPI(factory.CoordinateSequenceFactory);
            return ring
                ? factory.CreateLinearRing(seq)
                : factory.CreateLineString(seq);
        }

        public bool EqualsExact(RawLineString other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[Coordinates = {this.Coordinates.ToString()}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNonLineString() => throw new ArgumentException("GeometryType must be LineString.", "rawGeometry");
    }
}
