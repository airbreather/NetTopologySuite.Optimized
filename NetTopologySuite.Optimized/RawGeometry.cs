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

            this.Data = data;

            if ((BitConverter.IsLittleEndian && this.ByteOrder != ByteOrder.LittleEndian) ||
                (!BitConverter.IsLittleEndian && this.ByteOrder != ByteOrder.BigEndian))
            {
                ThrowNotSupportedExceptionForEndianness();
            }
        }

        public ByteOrder ByteOrder => (ByteOrder)this.Data[0];

        public GeometryType GeometryType => GetGeometryType(this.Data);

        public static GeometryType GetGeometryType(ReadOnlySpan<byte> wkb)
        {
            if (wkb.Length < 5)
            {
                ThrowArgumentExceptionForTooShortWKB();
            }

            uint packedTyp = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(wkb[1]));
            if ((packedTyp & 0xE0000000) != 0)
            {
                ThrowNotSupportedExceptionForBadDimension();
            }

            int ordinate = Math.DivRem(unchecked((int)packedTyp) & 0xFFFF, 1000, out int type);
            switch (ordinate)
            {
                case 1:
                case 2:
                case 3:
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
                bool swapCurrent = (unchecked((ByteOrder)(wkb2[0] & 1)) == ByteOrder.LittleEndian) != BitConverter.IsLittleEndian;

                var geometryTypeData = wkb2.Slice(1, 4);
                if (swapCurrent)
                {
                    geometryTypeData.Reverse();
                }

                GeometryType geometryType = GetGeometryType(geometryTypeData);

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
                        int geomCnt = Unsafe.ReadUnaligned<int>(ref wkb2[0]);

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
                        int ptCnt = Unsafe.ReadUnaligned<int>(ref wkb2[0]);

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
                        int ringCnt = Unsafe.ReadUnaligned<int>(ref wkb2[0]);

                        wkb2 = wkb2.Slice(4);
                        for (int i = 0; i < ringCnt; i++)
                        {
                            wkb2.Slice(0, 4).Reverse();
                            int ringPtCnt = Unsafe.ReadUnaligned<int>(ref wkb2[0]);

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
            switch (GetGeometryType(wkb))
            {
                case GeometryType.Point:
                    return 21;

                case GeometryType.LineString:
                    return 9 + Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref wkbStart, new IntPtr(5))) * 16;

                case GeometryType.Polygon:
                    int ringCnt = Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref wkbStart, new IntPtr(5)));
                    int off = 9;
                    var rem1 = wkb.Slice(9);
                    for (int i = 0; i < ringCnt; i++)
                    {
                        int ptCnt = Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref wkbStart, new IntPtr(off)));
                        off += ptCnt * 16 + 4;
                    }

                    return off;

                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    int cnt = Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref wkbStart, new IntPtr(5)));
                    var off2 = 9;
                    for (int i = 0; i < cnt; i++)
                    {
                        off2 += GetLength(wkb.Slice(off2));
                    }

                    return off2;

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    return 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForTooShortWKB() => throw new ArgumentException("No valid WKB is less than 5 bytes long.", "data");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForEndianness() => throw new NotSupportedException("Machine endianness needs to match WKB endianness for now... sorry.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadByteOrder() => throw new ArgumentException("Only big-endian (0) or little-endian (1) byte orders are supported here.", "wkb");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadDimension() => throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() => throw new NotSupportedException("Unsupported geometry type");
    }
}
