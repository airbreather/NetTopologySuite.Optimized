using GeoAPI.Geometries;

namespace NetTopologySuite.Optimized
{
    public static partial class OptimizedWKBWriter
    {
        public static int ComputeLength(IGeometry geometry)
        {
            // 1 byte for the byte order, 4 bytes for the packed type.
            const int FixedLength = 5;

            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    return FixedLength + ComputeLength((IPoint)geometry);

                case OgcGeometryType.LineString:
                    return FixedLength + ComputeLength((ILineString)geometry);

                case OgcGeometryType.Polygon:
                    return FixedLength + ComputeLength((IPolygon)geometry);

                case OgcGeometryType.MultiPoint:
                case OgcGeometryType.MultiLineString:
                case OgcGeometryType.MultiPolygon:
                case OgcGeometryType.GeometryCollection:
                    return FixedLength + ComputeLength((IGeometryCollection)geometry);

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    return 0;
            }
        }

        private static int ComputeLength(ILineString lineString)
        {
            // Int32 numPoints
            const int FixedLength = 4;

            return FixedLength + ComputeLength(lineString.CoordinateSequence);
        }

        private static int ComputeLength(IPolygon polygon)
        {
            // Int32 numRings
            const int FixedLength = 4;

            int length = FixedLength + ComputeLength(polygon.Shell);
            foreach (var hole in polygon.Holes)
            {
                length += ComputeLength(hole);
            }

            return length;
        }

        private static int ComputeLength(IPoint point) =>
            ComputeLength(point.CoordinateSequence);

        private static int ComputeLength(IGeometryCollection geometryCollection)
        {
            // Int32 numGeoms
            const int FixedLength = 4;

            int length = FixedLength;
            foreach (var geom in geometryCollection.Geometries)
            {
                length += ComputeLength(geom);
            }

            return length;
        }

        private static int ComputeLength(ICoordinateSequence seq)
        {
            if ((seq.Dimension != 2) | (seq.Ordinates != Ordinates.XY))
            {
                ThrowNotSupportedExceptionForBadDimension();
            }

            return seq.Count * 16;
        }
    }
}
