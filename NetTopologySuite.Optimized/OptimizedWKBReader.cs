using System;
using System.Runtime.CompilerServices;

using GeoAPI;
using GeoAPI.Geometries;
using GeoAPI.IO;

using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;

namespace NetTopologySuite.Optimized
{
    public enum CoordinatePackingMode
    {
        AOS,
        SOA
    }

    public sealed class OptimizedWKBReader
    {
        private readonly IGeometryFactory factory;

        public OptimizedWKBReader() => this.factory = GeometryServiceProvider.Instance.CreateGeometryFactory();

        public OptimizedWKBReader(IGeometryFactory factory) => this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

        public CoordinatePackingMode CoordinatePackingMode { get; set; }

        public IGeometry Read(byte[] wkb)
        {
            int pos = 0;
            return this.Read(wkb, ref pos);
        }

        public IGeometry Read(byte[] arr, ref int pos)
        {
            if (Read<ByteOrder>(arr, ref pos) != ByteOrder.LittleEndian)
            {
                throw new NotSupportedException("Big-endian WKB not supported... yet.");
            }

            uint packedTyp = Read<uint>(arr, ref pos);

            if ((packedTyp & 0xE0000000) != 0)
            {
                throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");
            }

            int ordinate = Math.DivRem((int)packedTyp & 0xffff, 1000, out int type);
            switch (ordinate)
            {
                case 1:
                case 2:
                case 3:
                    throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");
            }

            switch ((WKBGeometryTypes)type)
            {
                case WKBGeometryTypes.WKBPoint:
                    return this.ReadPoint(arr, ref pos);

                case WKBGeometryTypes.WKBLineString:
                    return this.ReadLineString(arr, ref pos);

                case WKBGeometryTypes.WKBPolygon:
                    return this.ReadPolygon(arr, ref pos);

                case WKBGeometryTypes.WKBMultiPoint:
                    return this.ReadMultiPoint(arr, ref pos);

                case WKBGeometryTypes.WKBMultiLineString:
                    return this.ReadMultiLineString(arr, ref pos);

                case WKBGeometryTypes.WKBMultiPolygon:
                    return this.ReadMultiPolygon(arr, ref pos);

                case WKBGeometryTypes.WKBGeometryCollection:
                    return this.ReadGeometryCollection(arr, ref pos);

                default:
                    throw new NotSupportedException("Unrecognized geometry type: " + (WKBGeometryTypes)type);
            }
        }

        private ILinearRing ReadLinearRing(byte[] arr, ref int pos) =>
            this.factory.CreateLinearRing(this.ReadCoordinateSequence(arr, ref pos));

        private IPoint ReadPoint(byte[] arr, ref int pos) =>
            this.factory.CreatePoint(this.ReadCoordinateSequence(arr, ref pos, 1));

        private ILineString ReadLineString(byte[] arr, ref int pos) =>
            this.factory.CreateLineString(this.ReadCoordinateSequence(arr, ref pos));

        private IPolygon ReadPolygon(byte[] arr, ref int pos)
        {
            int numRings = Read<int>(arr, ref pos);
            if (numRings == 0)
            {
                return this.factory.CreatePolygon(null, null);
            }

            ILinearRing shell = this.ReadLinearRing(arr, ref pos);
            if (numRings == 1)
            {
                return this.factory.CreatePolygon(shell);
            }

            ILinearRing[] holes = new ILinearRing[numRings - 1];
            for (int i = 0; i < holes.Length; ++i)
            {
                holes[i] = this.ReadLinearRing(arr, ref pos);
            }

            return this.factory.CreatePolygon(shell, holes);
        }

        private IMultiPoint ReadMultiPoint(byte[] arr, ref int pos) =>
            this.factory.CreateMultiPoint(this.ReadTypedGeometries<IPoint>(arr, ref pos));

        private IMultiLineString ReadMultiLineString(byte[] arr, ref int pos) =>
            this.factory.CreateMultiLineString(this.ReadTypedGeometries<ILineString>(arr, ref pos));

        private IMultiPolygon ReadMultiPolygon(byte[] arr, ref int pos) =>
            this.factory.CreateMultiPolygon(this.ReadTypedGeometries<IPolygon>(arr, ref pos));

        private IGeometryCollection ReadGeometryCollection(byte[] arr, ref int pos) =>
            this.factory.CreateGeometryCollection(this.ReadTypedGeometries<IGeometry>(arr, ref pos));

        private TGeometry[] ReadTypedGeometries<TGeometry>(byte[] arr, ref int pos)
            where TGeometry : IGeometry
        {
            TGeometry[] geometries = new TGeometry[Read<int>(arr, ref pos)];
            for (int i = 0; i < geometries.Length; ++i)
            {
                geometries[i] = (TGeometry)this.Read(arr, ref pos);
            }

            return geometries;
        }

        private ICoordinateSequence ReadCoordinateSequence(byte[] arr, ref int pos, int cnt = 0)
        {
            if (cnt == 0)
            {
                cnt = Read<int>(arr, ref pos);
            }

            return this.CoordinatePackingMode == CoordinatePackingMode.AOS
                ? ReadAOSCoordinateSequence(arr, ref pos, cnt)
                : ReadSOACoordinateSequence(arr, ref pos, cnt);
        }

        private static ICoordinateSequence ReadSOACoordinateSequence(byte[] arr, ref int pos, int cnt)
        {
            SOACoordinateSequence seq = new SOACoordinateSequence(cnt);
            for (int i = 0; i < cnt; ++i)
            {
                seq.Xs[i] = Read<double>(arr, ref pos);
                seq.Ys[i] = Read<double>(arr, ref pos);
            }

            return seq;
        }

        private static unsafe ICoordinateSequence ReadAOSCoordinateSequence(byte[] arr, ref int pos, int cnt)
        {
            PackedDoubleCoordinateSequence seq = new PackedDoubleCoordinateSequence(cnt, 2);
            int byteCnt = cnt * 16;
            fixed (void* toPtr = seq.GetRawCoordinates())
            fixed (void* fromPtr = &arr[pos])
            {
                Buffer.MemoryCopy(fromPtr, toPtr, byteCnt, byteCnt);
            }

            pos += byteCnt;
            return seq;
        }

        private static T Read<T>(byte[] arr, ref int pos)
        {
            T result = Unsafe.As<byte, T>(ref arr[pos]);
            pos += Unsafe.SizeOf<T>();
            return result;
        }
    }
}
