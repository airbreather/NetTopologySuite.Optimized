using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.Optimized.Raw
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
            uint packedTyp = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(wkb.Slice(1)));
            if ((packedTyp & 0xE0000000) != 0)
            {
                Console.WriteLine(packedTyp);
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
        private static void ThrowNotSupportedExceptionForBadDimension() => throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() => throw new NotSupportedException("Unsupported geometry type");
    }
}
