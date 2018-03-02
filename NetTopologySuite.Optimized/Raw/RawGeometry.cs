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
            switch (GetGeometryType(wkb))
            {
                case GeometryType.Point:
                    return 21;

                case GeometryType.LineString:
                    return 9 + wkb.Slice(5).NonPortableCast<byte, int>()[0] * 16;

                case GeometryType.Polygon:
                    int ringCnt = wkb.Slice(5).NonPortableCast<byte, int>()[0];
                    var rem1 = wkb.Slice(9);
                    for (int i = 0; i < ringCnt; i++)
                    {
                        int ptCnt = rem1.NonPortableCast<byte, int>()[0];
                        rem1 = rem1.Slice(ptCnt * 16 + 4);
                    }

                    return wkb.Length - rem1.Length;

                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    int cnt = wkb.Slice(5).NonPortableCast<byte, int>()[0];
                    var rem2 = wkb.Slice(9);
                    for (int i = 0; i < cnt; i++)
                    {
                        rem2 = rem2.Slice(GetLength(rem2));
                    }

                    return wkb.Length - rem2.Length;

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
