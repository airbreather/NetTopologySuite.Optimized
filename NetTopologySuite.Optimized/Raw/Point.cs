using System;

namespace NetTopologySuite.Optimized.Raw
{
    public ref struct Point
    {
        public RawGeometry RawGeometry;

        public Point(RawGeometry rawGeometry)
        {
            switch (rawGeometry.GeometryType)
            {
                case GeometryType.Point:
                    break;

                default:
                    throw new ArgumentException("GeometryType must be Point.", nameof(rawGeometry));
            }

            if (rawGeometry.Data.Length != 21)
            {
                throw new ArgumentException("Point geometries are always exactly 21 bytes.", nameof(rawGeometry));
            }

            this.RawGeometry = rawGeometry;
        }

        public double X => this.RawGeometry.Data.Slice(5).NonPortableCast<byte, double>()[0];

        public double Y => this.RawGeometry.Data.Slice(13).NonPortableCast<byte, double>()[0];

        public Coordinate CoordinateValue => this.RawGeometry.Data.Slice(5).NonPortableCast<byte, Coordinate>()[0];

        public GeoAPI.Geometries.IPoint ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory) => factory.CreatePoint(this.CoordinateValue.ToGeoAPI());

        public bool EqualsExact(Point other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[X: {this.X}, Y: {this.Y}]";
    }
}
