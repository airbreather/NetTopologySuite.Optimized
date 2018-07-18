using System;
using System.Globalization;
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

        public static bool operator ==(XYCoordinate first, XYCoordinate second) => first.X == second.X && first.Y == second.Y;
        public static bool operator !=(XYCoordinate first, XYCoordinate second) => first.X != second.X || first.Y != second.Y;

        public GeoAPI.Geometries.Coordinate CopyToGeoAPI(GeoAPI.Geometries.Coordinate coord = null)
        {
            if (coord is null)
            {
                coord = new GeoAPI.Geometries.Coordinate();
            }

            coord.X = this.X;
            coord.Y = this.Y;
            coord.Z = GeoAPI.Geometries.Coordinate.NullOrdinate;
            return coord;
        }

        public override bool Equals(object obj) => obj is XYCoordinate other && this.Equals(other);

        public bool Equals(XYCoordinate other) => this.X.Equals(other.X) && this.Y.Equals(other.Y);

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
        private static uint QueueRound(uint hash, uint queuedValue)
        {
            unchecked
            {
                hash += queuedValue * Prime3;
                return ((hash << 17) | (hash >> 15)) * Prime4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MixFinal(uint hash)
        {
            unchecked
            {
                hash ^= hash >> 15;
                hash *= Prime2;
                hash ^= hash >> 13;
                hash *= Prime3;
                hash ^= hash >> 16;
                return hash;
            }
        }

        public override string ToString()
        {
            ReadOnlySpan<char> xStr = this.X.ToRoundTripString(CultureInfo.InvariantCulture).AsSpan();
            ReadOnlySpan<char> yStr = this.Y.ToRoundTripString(CultureInfo.InvariantCulture).AsSpan();
            string result = new string(' ', 4 + xStr.Length + yStr.Length);
            unsafe
            {
                fixed (char* c = result)
                {
                    Span<char> chars = new Span<char>(c, result.Length);
                    chars[0] = '(';
                    xStr.CopyTo(chars.Slice(1, xStr.Length));
                    chars[xStr.Length + 1] = ',';
                    yStr.CopyTo(chars.Slice(xStr.Length + 3, yStr.Length));
                    chars[chars.Length - 1] = ')';
                }
            }

            return result;
        }
    }
}
