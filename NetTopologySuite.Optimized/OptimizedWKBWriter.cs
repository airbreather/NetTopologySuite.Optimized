using System;
using System.Runtime.CompilerServices;

using GeoAPI.Geometries;
using GeoAPI.IO;

using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;

namespace NetTopologySuite.Optimized
{
    public static partial class OptimizedWKBWriter
    {
        public static byte[] Write(IGeometry geometry)
        {
            byte[] result = new byte[ComputeLength(geometry)];
            Write(geometry, result);
            return result;
        }

        public static int Write(IGeometry geometry, Span<byte> bytes)
        {
            int sz = bytes.Length;
            Write(geometry, ref bytes);
            return sz - bytes.Length;
        }

        private static void Write(IGeometry geometry, ref Span<byte> bytes)
        {
            WriteVal(ByteOrder.LittleEndian, ref bytes);
            WriteVal((WKBGeometryTypes)geometry.OgcGeometryType, ref bytes);
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.LineString:
                    Write((ILineString)geometry, ref bytes);
                    break;

                case OgcGeometryType.Polygon:
                    Write((IPolygon)geometry, ref bytes);
                    break;

                case OgcGeometryType.Point:
                    Write((IPoint)geometry, ref bytes);
                    break;

                case OgcGeometryType.GeometryCollection:
                case OgcGeometryType.MultiPoint:
                case OgcGeometryType.MultiPolygon:
                case OgcGeometryType.MultiLineString:
                    Write((IGeometryCollection)geometry, ref bytes);
                    break;

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    break;
            }
        }

        private static void Write(ILineString lineString, ref Span<byte> bytes) =>
            Write(lineString.CoordinateSequence, ref bytes);

        private static void Write(IPolygon polygon, ref Span<byte> bytes)
        {
            ILinearRing[] holes = polygon.Holes;
            WriteVal(holes.Length + 1, ref bytes);

            Write(polygon.Shell, ref bytes);
            foreach (ILinearRing hole in holes)
            {
                Write(hole, ref bytes);
            }
        }

        private static void Write(IPoint point, ref Span<byte> bytes) =>
            Write(point.CoordinateSequence, ref bytes, emitLength: false);

        private static void Write(IGeometryCollection geometryCollection, ref Span<byte> bytes)
        {
            IGeometry[] geometries = geometryCollection.Geometries;
            WriteVal(geometries.Length, ref bytes);

            foreach (IGeometry geometry in geometries)
            {
                Write(geometry, ref bytes);
            }
        }

        private static void Write(ICoordinateSequence seq, ref Span<byte> bytes, bool emitLength = true)
        {
            if ((seq.Dimension != 2) | (seq.Ordinates != Ordinates.XY))
            {
                ThrowNotSupportedExceptionForBadDimension();
            }

            switch (seq)
            {
                case PackedDoubleCoordinateSequence aos:
                    WriteAOS(aos, ref bytes, emitLength);
                    break;

                case SOACoordinateSequence soa:
                    WriteSOA(soa, ref bytes, emitLength);
                    break;

                default:
                    WriteOther(seq, ref bytes, emitLength);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadDimension() => throw new NotSupportedException("Only XY geometries are supported.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() => throw new NotSupportedException("Unsupported geometry type");

        private static unsafe void WriteAOS(PackedDoubleCoordinateSequence aos, ref Span<byte> bytes, bool emitLength)
        {
            ReadOnlySpan<double> coords = aos.GetRawCoordinates();
            if (emitLength)
            {
                WriteVal(coords.Length / 2, ref bytes);
            }

            Span<byte> dst = bytes.Slice(0, coords.Length * 8);
            coords.AsBytes().CopyTo(dst);
            bytes = bytes.Slice(dst.Length);
        }

        private static void WriteSOA(SOACoordinateSequence soa, ref Span<byte> bytes, bool emitLength)
        {
            ReadOnlySpan<double> xs = soa.Xs;
            ReadOnlySpan<double> ys = soa.Ys;

            if (emitLength)
            {
                WriteVal(xs.Length, ref bytes);
            }

            for (int i = 0; i < xs.Length; i++)
            {
                WriteVal(xs[i], ref bytes);
                WriteVal(ys[i], ref bytes);
            }
        }

        private static void WriteOther(ICoordinateSequence seq, ref Span<byte> bytes, bool emitLength)
        {
            int length = seq.Count;
            if (emitLength)
            {
                WriteVal(length, ref bytes);
            }

            for (int i = 0; i < length; i++)
            {
                WriteVal(seq.GetX(i), ref bytes);
                WriteVal(seq.GetY(i), ref bytes);
            }
        }

        private static void WriteVal<T>(T value, ref Span<byte> bytes)
        {
            Unsafe.WriteUnaligned(ref bytes[0], value);
            bytes = bytes.Slice(Unsafe.SizeOf<T>());
        }
    }
}
