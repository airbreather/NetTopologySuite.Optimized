using System;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized.Raw
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Coordinate : IEquatable<Coordinate>
    {
        public double X;

        public double Y;

        public Coordinate(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Coordinate((double x, double y) tup)
        {
            this.X = tup.x;
            this.Y = tup.y;
        }

        public Coordinate(Coordinate copyFrom) => this = copyFrom;

        public Coordinate(GeoAPI.Geometries.Coordinate coordinate)
        {
            this.X = coordinate.X;
            this.Y = coordinate.Y;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = this.X;
            y = this.Y;
        }

        public static bool operator ==(Coordinate first, Coordinate second) => first.Equals(second);
        public static bool operator !=(Coordinate first, Coordinate second) => !first.Equals(second);

        public GeoAPI.Geometries.Coordinate ToGeoAPI() => new GeoAPI.Geometries.Coordinate(this.X, this.Y);

        public override bool Equals(object obj) => obj is Coordinate other && this.Equals(other);

        public bool Equals(Coordinate other) => this.X == other.X && this.Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(this.X, this.Y);

        public override string ToString() => $"[X: {this.X}, Y: {this.Y}]";
    }
}
