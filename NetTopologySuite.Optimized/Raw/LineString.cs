using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized.Raw
{
    public ref struct LineString
    {
        public RawGeometry RawGeometry;

        public LineString(RawGeometry rawGeometry)
        {
            if (rawGeometry.GeometryType != GeometryType.LineString)
            {
                ThrowArgumentExceptionForNonLineString();
            }

            this.RawGeometry = rawGeometry;
            new CoordinateSequence(this.RawGeometry.Data.Slice(5));
        }

        public CoordinateSequence Coordinates => new CoordinateSequence(this.RawGeometry.Data.Slice(5));

        public GeoAPI.Geometries.ILineString ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory, bool ring = false)
        {
            var seq = this.Coordinates.ToGeoAPI(factory.CoordinateSequenceFactory);
            return ring
                ? factory.CreateLinearRing(seq)
                : factory.CreateLineString(seq);
        }

        public bool EqualsExact(LineString other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[Coordinates = {this.Coordinates.ToString()}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNonLineString() => throw new ArgumentException("GeometryType must be LineString.", "rawGeometry");
    }
}
