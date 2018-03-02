using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        public IGeometry Read(ReadOnlySpan<byte> bytes) => this.Read(ref bytes);

        private IGeometry Read(ref ReadOnlySpan<byte> bytes)
        {
            if (Read<ByteOrder>(ref bytes) != ByteOrder.LittleEndian)
            {
                ThrowNotSupportedExceptionForBigEndian();
            }

            uint packedTyp = Read<uint>(ref bytes);

            if ((packedTyp & 0xE0000000) != 0)
            {
                ThrowNotSupportedExceptionForBadDimension();
            }

            int ordinate = Math.DivRem((int)packedTyp & 0xffff, 1000, out int type);
            switch (ordinate)
            {
                case 1:
                case 2:
                case 3:
                    ThrowNotSupportedExceptionForBadDimension();
                    break;
            }

            switch ((WKBGeometryTypes)type)
            {
                case WKBGeometryTypes.WKBPoint:
                    return this.ReadPoint(ref bytes);

                case WKBGeometryTypes.WKBLineString:
                    return this.ReadLineString(ref bytes);

                case WKBGeometryTypes.WKBPolygon:
                    return this.ReadPolygon(ref bytes);

                case WKBGeometryTypes.WKBMultiPoint:
                    return this.ReadMultiPoint(ref bytes);

                case WKBGeometryTypes.WKBMultiLineString:
                    return this.ReadMultiLineString(ref bytes);

                case WKBGeometryTypes.WKBMultiPolygon:
                    return this.ReadMultiPolygon(ref bytes);

                case WKBGeometryTypes.WKBGeometryCollection:
                    return this.ReadGeometryCollection(ref bytes);

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBigEndian() => throw new NotSupportedException("Big-endian WKB not supported... yet.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadDimension() => throw new NotSupportedException("Only the XY coordinate system is supported at this time, and no SRIDs.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() => throw new NotSupportedException("Unsupported geometry type");

        private ILinearRing ReadLinearRing(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateLinearRing(this.ReadCoordinateSequence(ref bytes));

        private IPoint ReadPoint(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreatePoint(this.ReadCoordinateSequence(ref bytes, 1));

        private ILineString ReadLineString(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateLineString(this.ReadCoordinateSequence(ref bytes));

        private IPolygon ReadPolygon(ref ReadOnlySpan<byte> bytes)
        {
            int numRings = Read<int>(ref bytes);
            if (numRings == 0)
            {
                return this.factory.CreatePolygon(null, null);
            }

            ILinearRing shell = this.ReadLinearRing(ref bytes);
            if (numRings == 1)
            {
                return this.factory.CreatePolygon(shell);
            }

            ILinearRing[] holes = new ILinearRing[numRings - 1];
            for (int i = 0; i < holes.Length; i++)
            {
                holes[i] = this.ReadLinearRing(ref bytes);
            }

            return this.factory.CreatePolygon(shell, holes);
        }

        private IMultiPoint ReadMultiPoint(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateMultiPoint(this.ReadTypedGeometries<IPoint>(ref bytes));

        private IMultiLineString ReadMultiLineString(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateMultiLineString(this.ReadTypedGeometries<ILineString>(ref bytes));

        private IMultiPolygon ReadMultiPolygon(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateMultiPolygon(this.ReadTypedGeometries<IPolygon>(ref bytes));

        private IGeometryCollection ReadGeometryCollection(ref ReadOnlySpan<byte> bytes) =>
            this.factory.CreateGeometryCollection(this.ReadTypedGeometries<IGeometry>(ref bytes));

        private TGeometry[] ReadTypedGeometries<TGeometry>(ref ReadOnlySpan<byte> bytes)
            where TGeometry : IGeometry
        {
            TGeometry[] geometries = new TGeometry[Read<int>(ref bytes)];
            for (int i = 0; i < geometries.Length; i++)
            {
                geometries[i] = (TGeometry)this.Read(ref bytes);
            }

            return geometries;
        }

        private ICoordinateSequence ReadCoordinateSequence(ref ReadOnlySpan<byte> bytes, int cnt = 0)
        {
            if (cnt == 0)
            {
                cnt = Read<int>(ref bytes);
            }

            return this.CoordinatePackingMode == CoordinatePackingMode.AOS
                ? ReadAOSCoordinateSequence(ref bytes, cnt)
                : ReadSOACoordinateSequence(ref bytes, cnt);
        }

        private static ICoordinateSequence ReadSOACoordinateSequence(ref ReadOnlySpan<byte> bytes, int cnt)
        {
            SOACoordinateSequence seq = new SOACoordinateSequence(cnt);
            for (int i = 0; i < cnt; i++)
            {
                seq.Xs[i] = Read<double>(ref bytes);
                seq.Ys[i] = Read<double>(ref bytes);
            }

            return seq;
        }

        private static unsafe ICoordinateSequence ReadAOSCoordinateSequence(ref ReadOnlySpan<byte> bytes, int cnt)
        {
            double[] vals = new double[cnt + cnt];
            ReadOnlySpan<double> src = bytes.NonPortableCast<byte, double>().Slice(0, vals.Length);
            bytes = bytes.Slice(src.AsBytes().Length);
            src.CopyTo(vals);
            return new PackedDoubleCoordinateSequence(vals, 2);
        }

        private static T Read<T>(ref ReadOnlySpan<byte> span)
            where T : struct
        {
            T result = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(Unsafe.SizeOf<T>());
            return result;
        }
    }
}
