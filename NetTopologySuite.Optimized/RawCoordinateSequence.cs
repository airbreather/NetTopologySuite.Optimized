﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawCoordinateSequence
    {
        public ReadOnlySpan<byte> PointData;

        public RawCoordinateSequence(ReadOnlySpan<byte> pointData)
        {
            this.PointData = pointData;
            int pointCountByLength = Math.DivRem(pointData.Length - 4, 16, out int remainder);
            if (remainder != 0 || pointCountByLength != this.PointCount)
            {
                ThrowArgumentExceptionForInconsistentLengths();
            }
        }

        public int PointCount => MemoryMarshal.Read<int>(this.PointData);

        public ReadOnlySpan<XYCoordinate> NonPortableCoordinates => MemoryMarshal.Cast<byte, XYCoordinate>(this.PointData.Slice(4));

        public GeoAPI.Geometries.ICoordinateSequence ToGeoAPI(GeoAPI.Geometries.ICoordinateSequenceFactory factory)
        {
            var pts = this.PointData.Slice(4);
            var len = pts.Length / 16;
            var seq = factory.Create(len, 2);
            for (int i = 0; i < len; i++, pts = pts.Slice(16))
            {
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.X, MemoryMarshal.Read<double>(pts));
                seq.SetOrdinate(i, GeoAPI.Geometries.Ordinate.Y, MemoryMarshal.Read<double>(pts.Slice(8)));
            }

            return seq;
        }

        public XYCoordinate GetCoordinate(int idx) => MemoryMarshal.Read<XYCoordinate>(this.PointData.Slice(idx * 16 + 4));

        public bool EqualsExact(RawCoordinateSequence other) => this.PointData.SequenceEqual(other.PointData);

        public override string ToString() => $"[PointCount = {this.PointCount}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForInconsistentLengths() =>
            throw new ArgumentException("Input data length is inconsistent with claimed point count.", "pointData");

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> rem;

            private XYCoordinate current;

            internal Enumerator(RawCoordinateSequence seq)
            {
                this.rem = seq.PointData.Slice(4);
                this.current = default;
            }

            public XYCoordinate Current => this.current;

            public bool MoveNext()
            {
                if (this.rem.Length == 0)
                {
                    return false;
                }

                this.current = MemoryMarshal.Read<XYCoordinate>(this.rem);
                this.rem = this.rem.Slice(16);
                return true;
            }
        }
    }
}
