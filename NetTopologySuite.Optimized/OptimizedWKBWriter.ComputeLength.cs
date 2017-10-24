using System;

using GeoAPI.Geometries;

namespace NetTopologySuite.Optimized
{
    public static partial class OptimizedWKBWriter
    {
        public static int ComputeLength(IGeometry geometry)
        {
            int length = 0;
            AddLength(geometry, ref length);
            return length;
        }

        private static void AddLength(IGeometry geometry, ref int length)
        {
            // 1 byte for the byte order, 4 bytes for the packed type.
            length += 5;

            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.LineString:
                    AddLength((ILineString)geometry, ref length);
                    break;

                case OgcGeometryType.Polygon:
                    AddLength((IPolygon)geometry, ref length);
                    break;

                case OgcGeometryType.Point:
                    AddLength((IPoint)geometry, ref length);
                    break;

                case OgcGeometryType.GeometryCollection:
                case OgcGeometryType.MultiPolygon:
                case OgcGeometryType.MultiLineString:
                case OgcGeometryType.MultiPoint:
                    AddLength((IGeometryCollection)geometry, ref length);
                    break;

                default:
                    throw new NotSupportedException("Unsupported geometry type: " + geometry.OgcGeometryType);
            }
        }

        private static void AddLength(ILineString lineString, ref int length)
        {
            // Int32 numPoints
            length += 4;

            AddLength(lineString.CoordinateSequence, ref length);
        }

        private static void AddLength(IPolygon polygon, ref int length)
        {
            // Int32 numRings
            length += 4;

            AddLength(polygon.Shell, ref length);
            foreach (var hole in polygon.Holes)
            {
                AddLength(hole, ref length);
            }
        }

        private static void AddLength(IPoint point, ref int length) =>
            AddLength(point.CoordinateSequence, ref length);

        private static void AddLength(IGeometryCollection geometryCollection, ref int length)
        {
            // Int32 numGeoms
            length += 4;

            foreach (var geom in geometryCollection.Geometries)
            {
                AddLength(geom, ref length);
            }
        }

        private static void AddLength(ICoordinateSequence seq, ref int length)
        {
            if ((seq.Dimension != 2) | (seq.Ordinates != Ordinates.XY))
            {
                throw new NotSupportedException("Only XY geometries are supported.");
            }

            length += seq.Count * 16;
        }
    }
}
