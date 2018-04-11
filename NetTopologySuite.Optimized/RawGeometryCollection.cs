using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized
{
    public ref struct RawGeometryCollection
    {
        public RawGeometry RawGeometry;

        public RawGeometryCollection(RawGeometry rawGeometry)
        {
            switch (rawGeometry.GeometryType)
            {
                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    break;

                default:
                    ThrowArgumentExceptionForNonGeometryCollection();
                    break;
            }

            this.RawGeometry = rawGeometry;
            int geomCount = this.GeomCount;
            if (geomCount < 0)
            {
                ThrowArgumentExceptionForNegativeGeomCount();
            }

            var rem = rawGeometry.Data.Slice(9);
            for (int i = 0; i < geomCount && rem.Length >= 4; i++)
            {
                int len = RawGeometry.GetLength(rem);
                RawGeometry cur = new RawGeometry(rem.Slice(0, len));
                rem = rem.Slice(len);

                switch (cur.GeometryType)
                {
                    case GeometryType.Point:
                        new RawPoint(cur);
                        break;

                    case GeometryType.LineString:
                        new RawLineString(cur);
                        break;

                    case GeometryType.Polygon:
                        new RawPolygon(cur);
                        break;

                    case GeometryType.MultiPoint:
                    case GeometryType.MultiLineString:
                    case GeometryType.MultiPolygon:
                    case GeometryType.GeometryCollection:
                        new RawGeometryCollection(cur);
                        break;

                    default:
                        ThrowNotSupportedExceptionForUnrecognizedChild();
                        break;
                }
            }

            if (rem.Length != 0)
            {
                ThrowArgumentExceptionForExcessRemainder();
            }
        }

        public int GeomCount => Unsafe.ReadUnaligned<int>(ref Unsafe.AsRef(this.RawGeometry.Data[5]));

        public RawGeometry GetGeom(int geomIndex)
        {
            var rem = this.RawGeometry.Data.Slice(9);
            for (int i = 0; i < geomIndex; i++)
            {
                rem = rem.Slice(RawGeometry.GetLength(rem));
            }

            RawGeometry result = default;
            result.Data = rem.Slice(0, RawGeometry.GetLength(rem));
            return result;
        }

        public GeoAPI.Geometries.IGeometryCollection ToGeoAPI(GeoAPI.Geometries.IGeometryFactory factory)
        {
            GeometryType myGeometryType = this.RawGeometry.GeometryType;
            int len = this.GeomCount;
            GeoAPI.Geometries.IGeometry[] geoms = null;
            switch (myGeometryType)
            {
                case GeometryType.MultiPoint:
                    geoms = new GeoAPI.Geometries.IPoint[len];
                    break;

                case GeometryType.MultiLineString:
                    geoms = new GeoAPI.Geometries.ILineString[len];
                    break;

                case GeometryType.MultiPolygon:
                    geoms = new GeoAPI.Geometries.IPolygon[len];
                    break;

                case GeometryType.GeometryCollection:
                    geoms = new GeoAPI.Geometries.IGeometry[len];
                    break;

                default:
                    ThrowArgumentExceptionForNonGeometryCollection();
                    break;
            }

            var rem = this.RawGeometry.Data.Slice(9);
            for (int i = 0; i < geoms.Length; i++)
            {
                RawGeometry cur = default;
                cur.Data = rem.Slice(0, RawGeometry.GetLength(rem));
                rem = rem.Slice(cur.Data.Length);
                switch (myGeometryType)
                {
                    case GeometryType.MultiPoint:
                        RawPoint pt = default;
                        pt.RawGeometry = cur;
                        geoms[i] = pt.ToGeoAPI(factory);
                        break;

                    case GeometryType.MultiLineString:
                        RawLineString ls = default;
                        geoms[i] = ls.ToGeoAPI(factory);
                        break;

                    case GeometryType.MultiPolygon:
                        RawPolygon poly = default;
                        poly.RawGeometry = cur;
                        geoms[i] = poly.ToGeoAPI(factory);
                        break;

                    default:
                        switch (cur.GeometryType)
                        {
                            case GeometryType.Point:
                                RawPoint ptChild = default;
                                ptChild.RawGeometry = cur;
                                geoms[i] = ptChild.ToGeoAPI(factory);
                                break;

                            case GeometryType.LineString:
                                RawLineString lsChild = default;
                                lsChild.RawGeometry = cur;
                                geoms[i] = lsChild.ToGeoAPI(factory);
                                break;

                            case GeometryType.Polygon:
                                RawPolygon polyChild = default;
                                polyChild.RawGeometry = cur;
                                geoms[i] = polyChild.ToGeoAPI(factory);
                                break;

                            case GeometryType.MultiPoint:
                            case GeometryType.MultiLineString:
                            case GeometryType.MultiPolygon:
                            case GeometryType.GeometryCollection:
                                RawGeometryCollection coll = default;
                                coll.RawGeometry = cur;
                                geoms[i] = coll.ToGeoAPI(factory);
                                break;

                            default:
                                ThrowNotSupportedExceptionForUnrecognizedChild();
                                break;
                        }

                        break;
                }
            }

            switch (myGeometryType)
            {
                case GeometryType.MultiPoint:
                    return factory.CreateMultiPoint((GeoAPI.Geometries.IPoint[])geoms);

                case GeometryType.MultiLineString:
                    return factory.CreateMultiLineString((GeoAPI.Geometries.ILineString[])geoms);

                case GeometryType.MultiPolygon:
                    return factory.CreateMultiPolygon((GeoAPI.Geometries.IPolygon[])geoms);

                default:
                    return factory.CreateGeometryCollection(geoms);
            }
        }

        public bool EqualsExact(RawGeometryCollection other)
        {
            // don't just check the .Slice(5)
            if (this.GeomCount != other.GeomCount)
            {
                return false;
            }

            var myEnumer = this.GetEnumerator();
            var otherEnumer = other.GetEnumerator();
            while (myEnumer.MoveNext())
            {
                // lengths pre-validated, so no need to check return
                otherEnumer.MoveNext();

                var myCur = myEnumer.Current.Data;
                var otherCur = otherEnumer.Current.Data;

                if (RawGeometry.GetGeometryType(myCur) != RawGeometry.GetGeometryType(otherCur) ||
                    !myCur.Slice(5).SequenceEqual(otherCur.Slice(5)))
                {
                    return false;
                }
            }

            // lengths pre-validated, so no need to check otherEnumer.MoveNext()
            return true;
        }

        public override string ToString() => $"[GeomCount = {this.GeomCount}]";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNonGeometryCollection() =>
            throw new ArgumentException("GeometryType must be a geometry collection.", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotSupportedExceptionForUnrecognizedChild() =>
            throw new NotSupportedException("Child GeometryType is not supported");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForNegativeGeomCount() =>
            throw new ArgumentException("Geom count cannot be negative", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForExcessRemainder() =>
            throw new ArgumentException("Data is janky...", "rawGeometry");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForUnclosedRing(int ringIndex) =>
            throw new ArgumentException($"Ring {ringIndex} is not closed.", "rawGeometry");

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> rem;

            private RawGeometry current;

            internal Enumerator(RawGeometryCollection coll) => this.rem = coll.RawGeometry.Data.Slice(9);

            public RawGeometry Current => this.current;

            public unsafe bool MoveNext()
            {
                if (this.rem.Length == 0)
                {
                    return false;
                }

                this.current.Data = this.rem.Slice(0, RawGeometry.GetLength(this.rem));
                this.rem = this.rem.Slice(this.current.Data.Length);
                return true;
            }
        }
    }
}
