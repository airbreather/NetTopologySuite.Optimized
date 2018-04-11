using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawGeometry
    {
        public ReadOnlySpan<byte> Data;

        public RawGeometry(ReadOnlySpan<byte> data)
        {
            if (data.Length < 5)
            {
                ThrowArgumentExceptionForTooShortWKB();
            }

            if (WkbByteOrderDiffersFromNative(data))
            {
                ThrowNotSupportedExceptionForEndianness();
            }

            this.Data = data;
        }

        public ByteOrder ByteOrder => (ByteOrder)this.Data[0];

        public GeometryType GeometryType => GetGeometryType(this.Data);

        public static GeometryType GetGeometryType(ReadOnlySpan<byte> wkb)
        {
            if (wkb.Length < 5)
            {
                ThrowArgumentExceptionForTooShortWKB();
            }

            uint packedTyp = MemoryMarshal.Read<uint>(wkb.Slice(1));
            if ((packedTyp & 0b11100000000000000000000000000000) != 0)
            {
                // first 3 most significant bits are "has Z", "has M", "has SRID", in that order.
                // we're hyper-optimized for a very strict binary layout, so we can't support any of
                // the extra stuff that one might find in EWKB.
                ThrowNotSupportedExceptionForBadDimension();
            }

            int ordinate = Math.DivRem(unchecked((int)packedTyp) & 0xFFFF, 1000, out int type);
            switch (ordinate)
            {
                case 1: // XYZ
                case 2: // XYM
                case 3: // XYZM
                    ThrowNotSupportedExceptionForBadDimension();
                    break;
            }

            return (GeometryType)type;
        }

        public static void SwapToNativeByteOrderIfNeeded(Span<byte> wkb)
        {
            Core(ref wkb);

            void Core(ref Span<byte> wkb2)
            {
                bool swapCurrent = WkbByteOrderDiffersFromNative(wkb2);
                if (swapCurrent)
                {
                    wkb2[0] = unchecked((byte)(wkb2[0] ^ 1));
                    wkb2.Slice(1, 4).Reverse();
                }

                GeometryType geometryType = GetGeometryType(wkb2);

                wkb2 = wkb2.Slice(5);
                switch (geometryType)
                {
                    case GeometryType.MultiPoint:
                    case GeometryType.MultiLineString:
                    case GeometryType.MultiPolygon:
                    case GeometryType.GeometryCollection:
                        if (swapCurrent)
                        {
                            wkb2.Slice(0, 4).Reverse();
                        }

                        // still go through components, because they might have a different byte order.
                        int geomCnt = MemoryMarshal.Read<int>(wkb2);

                        wkb2 = wkb2.Slice(4);
                        for (int i = 0; i < geomCnt; i++)
                        {
                            Core(ref wkb2);
                        }

                        return;
                }

                if (!swapCurrent)
                {
                    return;
                }

                switch (geometryType)
                {
                    case GeometryType.Point:
                        wkb2.Slice(0, 8).Reverse();
                        wkb2.Slice(8, 8).Reverse();
                        return;

                    case GeometryType.LineString:
                        wkb2.Slice(0, 4).Reverse();
                        int ptCnt = MemoryMarshal.Read<int>(wkb2);

                        wkb2 = wkb2.Slice(4);
                        for (int i = 0; i < ptCnt; i++)
                        {
                            wkb2.Slice(0, 8).Reverse();
                            wkb2.Slice(8, 8).Reverse();
                            wkb2 = wkb2.Slice(16);
                        }

                        return;

                    case GeometryType.Polygon:
                        wkb2.Slice(0, 4).Reverse();
                        int ringCnt = MemoryMarshal.Read<int>(wkb2);

                        wkb2 = wkb2.Slice(4);
                        for (int i = 0; i < ringCnt; i++)
                        {
                            wkb2.Slice(0, 4).Reverse();
                            int ringPtCnt = MemoryMarshal.Read<int>(wkb2);

                            wkb2 = wkb2.Slice(4);
                            for (int j = 0; j < ringPtCnt; j++)
                            {
                                wkb2.Slice(0, 8).Reverse();
                                wkb2.Slice(8, 8).Reverse();
                                wkb2 = wkb2.Slice(16);
                            }
                        }

                        return;
                }
            }
        }

        internal static int GetLength(ReadOnlySpan<byte> wkb)
        {
            ref var wkbStart = ref MemoryMarshal.GetReference(wkb);

            // most things start with the forced 5 and then have a 4-byte integer after.
            // everything else is fixed-length anyway.
            int res = 9;
            switch (GetGeometryType(wkb))
            {
                case GeometryType.Point:
                    res = 21;
                    break;

                case GeometryType.LineString:
                    res += MemoryMarshal.Read<int>(wkb.Slice(5)) * 16;
                    break;

                case GeometryType.Polygon:
                    int ringCnt = MemoryMarshal.Read<int>(wkb.Slice(5));
                    for (int i = 0; i < ringCnt; i++)
                    {
                        int ptCnt = MemoryMarshal.Read<int>(wkb.Slice(res));
                        res += ptCnt * 16 + 4;
                    }

                    break;

                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    int cnt = MemoryMarshal.Read<int>(wkb.Slice(5));
                    for (int i = 0; i < cnt; i++)
                    {
                        res += GetLength(wkb.Slice(res));
                    }

                    break;

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    break;
            }

            return res;
        }

        private static bool WkbByteOrderDiffersFromNative(ReadOnlySpan<byte> wkb) => unchecked(((ByteOrder)(wkb[0] & 1)) == ByteOrder.BigEndian) == BitConverter.IsLittleEndian;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForTooShortWKB() =>
            throw new ArgumentException("No valid WKB is less than 5 bytes long.", "data");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForEndianness() =>
            throw new NotSupportedException("Machine endianness needs to match WKB endianness for now... sorry.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadByteOrder() =>
            throw new ArgumentException("Only big-endian (0) or little-endian (1) byte orders are supported here.", "wkb");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadDimension() =>
            throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() =>
            throw new NotSupportedException("Unsupported geometry type");
    }
}
