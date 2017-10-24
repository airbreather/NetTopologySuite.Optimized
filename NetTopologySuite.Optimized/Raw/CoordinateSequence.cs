using System;

namespace NetTopologySuite.Optimized.Raw
{
    public struct CoordinateSequence
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

        public int PointCount => this.PointData.NonPortableCast<byte, int>()[0];

        public ReadOnlySpan<Coordinate> Coordinates => this.PointData.Slice(4).NonPortableCast<byte, Coordinate>();

        public GeoAPI.Geometries.ICoordinateSequence ToGeoAPI(GeoAPI.Geometries.ICoordinateSequenceFactory factory)
        {
            var coords = this.Coordinates;
            var seq = factory.Create(coords.Length, 2);
            for (int i = 0; i < coords.Length; ++i)
            {
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.X, coords[i].X);
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.Y, coords[i].Y);
            }

            return seq;
        }

        public bool EqualsExact(CoordinateSequence other) => this.PointData.SequenceEqual(other.PointData);

        public override string ToString() => $"[PointCount = {this.PointCount}]";
    }
}
