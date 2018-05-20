using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetTopologySuite.Optimized
{
    public static class WKTRawWriter
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        public static string Write(RawGeometry geometry)
        {
            StringBuilder builder = new StringBuilder();
            using (StringWriter writer = new StringWriter(builder))
            {
                Write(geometry, writer);
            }

            return builder.ToString();
        }

        public static void Write(RawGeometry geometry, TextWriter writer)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    RawPoint point = default;
                    point.RawGeometry = geometry;
                    WritePoint(point, writer);
                    break;

                case GeometryType.LineString:
                    RawLineString lineString = default;
                    lineString.RawGeometry = geometry;
                    WriteLineString(lineString, writer);
                    break;

                case GeometryType.Polygon:
                    RawPolygon polygon = default;
                    polygon.RawGeometry = geometry;
                    WritePolygon(polygon, writer);
                    break;

                case GeometryType.MultiPoint:
                    RawGeometryCollection collection2 = default;
                    collection2.RawGeometry = geometry;
                    WriteMultiPoint(collection2, writer);
                    break;

                case GeometryType.MultiLineString:
                    RawGeometryCollection collection3 = default;
                    collection3.RawGeometry = geometry;
                    WriteMultiLineString(collection3, writer);
                    break;

                case GeometryType.MultiPolygon:
                    RawGeometryCollection collection4 = default;
                    collection4.RawGeometry = geometry;
                    WriteMultiPolygon(collection4, writer);
                    break;

                case GeometryType.GeometryCollection:
                    RawGeometryCollection collection1 = default;
                    collection1.RawGeometry = geometry;
                    WriteGeometryCollection(collection1, writer);
                    break;

                default:
                    ThrowNotSupportedExceptionForBadGeometryType();
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForBadGeometryType() =>
            throw new NotSupportedException("Unsupported geometry type");

        private static void WritePoint(RawPoint point, TextWriter writer)
        {
            writer.Write("POINT (");
            writer.Write(point.X.ToString(InvariantCulture));
            writer.Write(' ');
            writer.Write(point.Y.ToString(InvariantCulture));
            writer.Write(')');
        }

        private static void WriteLineString(RawLineString lineString, TextWriter writer)
        {
            writer.Write("LINESTRING ");
            WriteCoordinates(lineString.Coordinates, writer);
        }

        private static void WritePolygon(RawPolygon polygon, TextWriter writer)
        {
            writer.Write("POLYGON (");

            bool first = true;
            foreach (var ring in polygon)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                WriteCoordinates(ring, writer);
            }

            writer.Write(')');
        }

        private static void WriteGeometryCollection(RawGeometryCollection collection, TextWriter writer)
        {
            writer.Write("GEOMETRYCOLLECTION (");

            bool first = true;
            foreach (var geom in collection)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                Write(geom, writer);
            }

            writer.Write(')');
        }

        private static void WriteMultiPoint(RawGeometryCollection collection, TextWriter writer)
        {
            writer.Write("MULTIPOINT (");

            RawPoint pt = default;
            bool first = true;
            foreach (var geom in collection)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                writer.Write('(');
                pt.RawGeometry = geom;
                writer.Write(pt.X.ToString(InvariantCulture));
                writer.Write(' ');
                writer.Write(pt.Y.ToString(InvariantCulture));
                writer.Write(')');
            }

            writer.Write(')');
        }

        private static void WriteMultiLineString(RawGeometryCollection collection, TextWriter writer)
        {
            writer.Write("MULTILINESTRING (");

            RawLineString lineString = default;
            bool first = true;
            foreach (var geom in collection)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                lineString.RawGeometry = geom;
                WriteCoordinates(lineString.Coordinates, writer);
            }

            writer.Write(')');
        }

        private static void WriteMultiPolygon(RawGeometryCollection collection, TextWriter writer)
        {
            writer.Write("MULTIPOLYGON (");

            RawPolygon polygon = default;
            bool firstPolygon = true;
            foreach (var geom in collection)
            {
                if (firstPolygon)
                {
                    firstPolygon = false;
                }
                else
                {
                    writer.Write(", ");
                }

                polygon.RawGeometry = geom;
                writer.Write('(');
                bool firstRing = true;
                foreach (var ring in polygon)
                {
                    if (firstRing)
                    {
                        firstRing = false;
                    }
                    else
                    {
                        writer.Write(", ");
                    }

                    WriteCoordinates(ring, writer);
                }

                writer.Write(')');
            }

            writer.Write(')');
        }

        private static void WriteCoordinates(RawCoordinateSequence coordinates, TextWriter writer)
        {
            writer.Write('(');

            bool first = true;
            foreach (var coord in coordinates)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                writer.Write(coord.X.ToString(InvariantCulture));
                writer.Write(' ');
                writer.Write(coord.Y.ToString(InvariantCulture));
            }

            writer.Write(')');
        }
    }
}
