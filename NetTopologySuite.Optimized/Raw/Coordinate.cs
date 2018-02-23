using System;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized.Raw
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Coordinate : IEquatable<Coordinate>
    {
        public double X;

        public double Y;

        public Coordinate(double x, double y) => (this.X, this.Y) = (x, y);
        public Coordinate((double x, double y) tup) => (this.X, this.Y) = tup;
        public Coordinate(Coordinate copyFrom) => this = copyFrom;
        public Coordinate(GeoAPI.Geometries.Coordinate coordinate) => (this.X, this.Y) = (coordinate.X, coordinate.Y);

        public void Deconstruct(out double x, out double y) => (x, y) = (this.X, this.Y);

        public static bool operator ==(Coordinate first, Coordinate second) => first.Equals(second);
        public static bool operator !=(Coordinate first, Coordinate second) => !first.Equals(second);

        public GeoAPI.Geometries.Coordinate ToGeoAPI() => new GeoAPI.Geometries.Coordinate(this.X, this.Y);

        public override bool Equals(object obj) => obj is Coordinate other && this.Equals(other);

        public bool Equals(Coordinate other) => (this.X, this.Y).Equals((other.X, other.Y));

        public override int GetHashCode() => (this.X, this.Y).GetHashCode();

        public override string ToString() => $"[X: {this.X}, Y: {this.Y}]";
    }
}
