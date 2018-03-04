using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XYCoordinate : IEquatable<XYCoordinate>
    {
        ////private const uint Prime1 = 2654435761U;
        private const uint Prime2 = 2246822519U;
        private const uint Prime3 = 3266489917U;
        private const uint Prime4 = 668265263U;
        private const uint Prime5 = 374761393U;
        private const uint S_Seed = 2978308642U; // generated on random.org

        public double X;

        public double Y;

        public XYCoordinate(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public XYCoordinate((double x, double y) tup)
        {
            this.X = tup.x;
            this.Y = tup.y;
        }

        public XYCoordinate(XYCoordinate copyFrom) => this = copyFrom;

        public XYCoordinate(GeoAPI.Geometries.Coordinate coordinate)
        {
            this.X = coordinate.X;
            this.Y = coordinate.Y;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = this.X;
            y = this.Y;
        }

        public static bool operator ==(XYCoordinate first, XYCoordinate second) => first.Equals(second);
        public static bool operator !=(XYCoordinate first, XYCoordinate second) => !first.Equals(second);

        public GeoAPI.Geometries.Coordinate ToGeoAPI() => new GeoAPI.Geometries.Coordinate(this.X, this.Y);

        public override bool Equals(object obj) => obj is XYCoordinate other && this.Equals(other);

        public bool Equals(XYCoordinate other) => this.X == other.X && this.Y == other.Y;

        public override int GetHashCode()
        {
            // inline HashCode.Combine<T1, T2> from dotnet/corefx
            // hopefully this can get replaced after dotnet/corefx#26412
            unchecked
            {
                uint hc1 = (uint)this.X.GetHashCode();
                uint hc2 = (uint)this.Y.GetHashCode();

                // also inline the +8
                const uint EmptyState = S_Seed + Prime5 + 8;
                uint hash = EmptyState;

                hash = QueueRound(hash, hc1);
                hash = QueueRound(hash, hc2);

                hash = MixFinal(hash);
                return (int)hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rol(uint value, int count)
            => (value << count) | (value >> (32 - count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint QueueRound(uint hash, uint queuedValue)
        {
            hash += queuedValue * Prime3;
            return Rol(hash, 17) * Prime4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MixFinal(uint hash)
        {
            hash ^= hash >> 15;
            hash *= Prime2;
            hash ^= hash >> 13;
            hash *= Prime3;
            hash ^= hash >> 16;
            return hash;
        }

        public override string ToString() => $"[X: {this.X}, Y: {this.Y}]";
    }
}
