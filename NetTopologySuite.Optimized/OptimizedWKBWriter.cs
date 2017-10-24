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
            int pos = 0;
            Write(geometry, result, ref pos);
            return result;
        }

        public static void Write(IGeometry geometry, byte[] arr, ref int pos)
        {
            WriteVal(ByteOrder.LittleEndian, arr, ref pos);
            WriteVal((WKBGeometryTypes)geometry.OgcGeometryType, arr, ref pos);
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.LineString:
                    Write((ILineString)geometry, arr, ref pos);
                    break;

                case OgcGeometryType.Polygon:
                    Write((IPolygon)geometry, arr, ref pos);
                    break;

                case OgcGeometryType.Point:
                    Write((IPoint)geometry, arr, ref pos);
                    break;

                case OgcGeometryType.GeometryCollection:
                case OgcGeometryType.MultiPoint:
                case OgcGeometryType.MultiPolygon:
                case OgcGeometryType.MultiLineString:
                    Write((IGeometryCollection)geometry, arr, ref pos);
                    break;

                default:
                    throw new NotSupportedException("Unsupported geometry type: " + geometry.OgcGeometryType);
            }
        }

        private static void Write(ILineString lineString, byte[] arr, ref int pos) =>
            Write(lineString.CoordinateSequence, arr, ref pos);

        private static void Write(IPolygon polygon, byte[] arr, ref int pos)
        {
            ILinearRing[] holes = polygon.Holes;
            WriteVal(holes.Length + 1, arr, ref pos);

            Write(polygon.Shell, arr, ref pos);
            foreach (ILinearRing hole in holes)
            {
                Write(hole, arr, ref pos);
            }
        }

        private static void Write(IPoint point, byte[] arr, ref int pos) =>
            Write(point.CoordinateSequence, arr, ref pos, emitLength: false);

        private static void Write(IGeometryCollection geometryCollection, byte[] arr, ref int pos)
        {
            IGeometry[] geometries = geometryCollection.Geometries;
            WriteVal(geometries.Length, arr, ref pos);

            foreach (IGeometry geometry in geometries)
            {
                Write(geometry, arr, ref pos);
            }
        }

        private static void Write(ICoordinateSequence seq, byte[] arr, ref int pos, bool emitLength = true)
        {
            if ((seq.Dimension != 2) | (seq.Ordinates != Ordinates.XY))
            {
                throw new NotSupportedException("Only XY geometries are supported.");
            }

            switch (seq)
            {
                case PackedDoubleCoordinateSequence aos:
                    WriteAOS(aos, arr, ref pos, emitLength);
                    break;

                case SOACoordinateSequence soa:
                    WriteSOA(soa, arr, ref pos, emitLength);
                    break;

                default:
                    WriteOther(seq, arr, ref pos, emitLength);
                    break;
            }
        }

        private static unsafe void WriteAOS(PackedDoubleCoordinateSequence aos, byte[] arr, ref int pos, bool emitLength)
        {
            double[] coords = aos.GetRawCoordinates();
            if (emitLength)
            {
                WriteVal(coords.Length >> 1, arr, ref pos);
            }

            int byteCnt = coords.Length * 8;
            fixed (void* toPtr = &arr[pos])
            fixed (void* fromPtr = coords)
            {
                Buffer.MemoryCopy(fromPtr, toPtr, byteCnt, byteCnt);
            }

            pos += byteCnt;
        }

        private static void WriteSOA(SOACoordinateSequence soa, byte[] arr, ref int pos, bool emitLength)
        {
            if (emitLength)
            {
                WriteVal(soa.Xs.Length, arr, ref pos);
            }

            for (int i = 0; i < soa.Xs.Length; ++i)
            {
                WriteVal(soa.Xs[i], arr, ref pos);
                WriteVal(soa.Ys[i], arr, ref pos);
            }
        }

        private static void WriteOther(ICoordinateSequence seq, byte[] arr, ref int pos, bool emitLength)
        {
            int length = seq.Count;
            if (emitLength)
            {
                WriteVal(length, arr, ref pos);
            }

            for (int i = 0; i < length; ++i)
            {
                WriteVal(seq.GetX(i), arr, ref pos);
                WriteVal(seq.GetY(i), arr, ref pos);
            }
        }

        private static void WriteVal<T>(T value, byte[] arr, ref int pos)
        {
            Unsafe.WriteUnaligned(ref arr[pos], value);
            pos += Unsafe.SizeOf<T>();
        }
    }
}
