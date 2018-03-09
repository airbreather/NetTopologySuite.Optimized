namespace NetTopologySuite.Optimized
{
    public enum VisitMode
    {
        TopmostOnly,
        GeometryCollectionElements,
        CoordinateSequences,
        Coordinates
    }

    public abstract class RawGeometryVisitorBase
    {
        public void Visit(RawGeometry geometry, VisitMode mode)
        {
            this.OnVisitRawGeometry(geometry);

            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    RawPoint point = default;
                    point.RawGeometry = geometry;
                    this.OnVisitRawPoint(point);
                    if (mode >= VisitMode.Coordinates)
                    {
                        this.OnVisitCoordinate(point.CoordinateValue);
                    }

                    break;

                case GeometryType.LineString:
                    RawLineString lineString = default;
                    lineString.RawGeometry = geometry;
                    this.OnVisitRawLineString(lineString);
                    if (mode >= VisitMode.CoordinateSequences)
                    {
                        VisitCoordinateSequence(lineString.Coordinates);
                    }

                    break;

                case GeometryType.Polygon:
                    RawPolygon polygon = default;
                    polygon.RawGeometry = geometry;
                    this.OnVisitRawPolygon(polygon);
                    if (mode >= VisitMode.CoordinateSequences)
                    {
                        foreach (RawCoordinateSequence ring in polygon)
                        {
                            VisitCoordinateSequence(ring);
                        }
                    }

                    break;

                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    RawGeometryCollection geometryCollection = default;
                    geometryCollection.RawGeometry = geometry;
                    VisitGeometryCollection(geometryCollection);
                    break;
            }

            void VisitCoordinateSequence(RawCoordinateSequence sequence)
            {
                this.OnVisitRawCoordinateSequence(sequence);

                if (mode >= VisitMode.Coordinates)
                {
                    foreach (XYCoordinate coordinate in sequence)
                    {
                        this.OnVisitCoordinate(coordinate);
                    }
                }
            }

            void VisitGeometryCollection(RawGeometryCollection geometryCollection)
            {
                switch (geometryCollection.RawGeometry.GeometryType)
                {
                    case GeometryType.MultiPoint:
                        this.OnVisitRawMultiPoint(geometryCollection);
                        break;

                    case GeometryType.MultiLineString:
                        this.OnVisitRawMultiLineString(geometryCollection);
                        break;

                    case GeometryType.MultiPolygon:
                        this.OnVisitRawMultiPolygon(geometryCollection);
                        break;

                    default:
                        this.OnVisitRawHeterogeneousGeometryCollection(geometryCollection);
                        break;
                }

                this.OnVisitRawGeometryCollection(geometryCollection);

                if (mode >= VisitMode.GeometryCollectionElements)
                {
                    foreach (RawGeometry element in geometryCollection)
                    {
                        this.Visit(element, mode);
                    }
                }
            }
        }

        protected virtual void OnVisitRawGeometry(RawGeometry geometry) { }

        protected virtual void OnVisitRawPoint(RawPoint point) { }

        protected virtual void OnVisitRawLineString(RawLineString lineString) { }

        protected virtual void OnVisitRawPolygon(RawPolygon polygon) { }

        protected virtual void OnVisitRawGeometryCollection(RawGeometryCollection geometryCollection) { }

        protected virtual void OnVisitRawHeterogeneousGeometryCollection(RawGeometryCollection geometryCollection) { }

        protected virtual void OnVisitRawMultiPoint(RawGeometryCollection multiPoint) { }

        protected virtual void OnVisitRawMultiLineString(RawGeometryCollection multiLineString) { }

        protected virtual void OnVisitRawMultiPolygon(RawGeometryCollection multiPolygon) { }

        protected virtual void OnVisitRawCoordinateSequence(RawCoordinateSequence sequence) { }

        protected virtual void OnVisitCoordinate(XYCoordinate coordinate) { }
    }
}
