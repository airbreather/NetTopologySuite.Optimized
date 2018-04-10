using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawPoint
    {
        public RawGeometry RawGeometry;

        public RawPoint(RawGeometry rawGeometry)
        {
            if (rawGeometry.GeometryType != GeometryType.Point)
            {
                ThrowArgumentExceptionForNonPoint();
            }

            if (rawGeometry.Data.Length != 21)
            {
                ThrowArgumentExceptionForBadLength();
            }

            this.RawGeometry = rawGeometry;
        }

        public double X => Unsafe.ReadUnaligned<double>(ref Unsafe.AsRef(this.RawGeometry.Data[5]));

        public double Y => Unsafe.ReadUnaligned<double>(ref Unsafe.AsRef(this.RawGeometry.Data[13]));

        public XYCoordinate CoordinateValue => Unsafe.ReadUnaligned<XYCoordinate>(ref Unsafe.AsRef(this.RawGeometry.Data[5]));

        public GeoAPI.Geometries.IPoint ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory) => factory.CreatePoint(this.CoordinateValue.ToGeoAPI());

        public bool EqualsExact(RawPoint other) => this.RawGeometry.Data.Slice(5).SequenceEqual(other.RawGeometry.Data.Slice(5));

        public override string ToString() => $"[X: {this.X}, Y: {this.Y}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNonPoint() => throw new ArgumentException("GeometryType must be Point.", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadLength() => throw new ArgumentException("Point geometries are always exactly 21 bytes.", "rawGeometry");
    }
}
