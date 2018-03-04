using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using GeoAPI.Geometries;

using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;

namespace NetTopologySuite.Optimized.Bench
{
    public class Program
    {
        private byte[] data;

        [GlobalSetup]
        public void Setup()
        {
            IGeometryFactory factory = NtsGeometryServices.Instance.CreateGeometryFactory();
            Random rand = new Random();
            IPolygon[] polygons = new IPolygon[4];
            for (int i = 0; i < polygons.Length; i++)
            {
                var coords = new Coordinate[20000];
                for (int j = 0; j < coords.Length; j++)
                {
                    coords[j] = new Coordinate(rand.NextDouble() * 360 - 180, rand.NextDouble() * 170 - 85);
                }

                coords[coords.Length - 1] = coords[0];
                polygons[i] = factory.CreatePolygon(coords);
            }

            this.data = factory.CreateMultiPolygon(polygons).AsBinary();
        }

        [Benchmark]
        public double ReadNTSCoordinateArray()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know it's stored as Coordinate[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new WKBReader(new NtsGeometryServices(CoordinateArraySequenceFactory.Instance, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID)) { RepairRings = false }.Read(this.data);
            double minX = Double.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                IPolygon poly = (IPolygon)geom;
                foreach (Coordinate coord in poly.Shell.Coordinates)
                {
                    if (coord.X < minX)
                    {
                        minX = coord.X;
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadNTSPackedDouble()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know it's stored as double[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new WKBReader(new NtsGeometryServices(new PackedCoordinateSequenceFactory(PackedCoordinateSequenceFactory.PackedType.Double, 2), NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID)) { RepairRings = false }.Read(this.data);
            double minX = Double.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                IPolygon poly = (IPolygon)geom;
                PackedDoubleCoordinateSequence seq = (PackedDoubleCoordinateSequence)poly.Shell.CoordinateSequence;
                double[] rawCoords = seq.GetRawCoordinates();
                for (int i = 0; i < rawCoords.Length; i += 2)
                {
                    if (rawCoords[i] < minX)
                    {
                        minX = rawCoords[i];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadNTSPackedFloat()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know it's stored as double[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new WKBReader(new NtsGeometryServices(new PackedCoordinateSequenceFactory(PackedCoordinateSequenceFactory.PackedType.Float, 2), NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID)) { RepairRings = false }.Read(this.data);
            float minX = Single.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                IPolygon poly = (IPolygon)geom;
                PackedFloatCoordinateSequence seq = (PackedFloatCoordinateSequence)poly.Shell.CoordinateSequence;
                float[] rawCoords = seq.GetRawCoordinates();
                for (int i = 0; i < rawCoords.Length; i += 2)
                {
                    if (rawCoords[i] < minX)
                    {
                        minX = rawCoords[i];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedAOS()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know it's stored as double[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new OptimizedWKBReader(NtsGeometryServices.Instance.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.AOS }.Read(this.data);
            double minX = Double.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                IPolygon poly = (IPolygon)geom;
                PackedDoubleCoordinateSequence seq = (PackedDoubleCoordinateSequence)poly.Shell.CoordinateSequence;
                double[] rawCoords = seq.GetRawCoordinates();
                for (int i = 0; i < rawCoords.Length; i += 2)
                {
                    if (rawCoords[i] < minX)
                    {
                        minX = rawCoords[i];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedSOA_Scalar()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know just the Xs are stored as double[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new OptimizedWKBReader(NtsGeometryServices.Instance.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.SOA }.Read(this.data);
            double minX = Double.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                IPolygon poly = (IPolygon)geom;
                SOACoordinateSequence seq = (SOACoordinateSequence)poly.Shell.CoordinateSequence;
                Span<double> xs = seq.Xs;
                for (int i = 0; i < xs.Length; i++)
                {
                    if (xs[i] < minX)
                    {
                        minX = xs[i];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedSOA_Vector()
        {
            // best we can possibly do with our knowledge of the data:
            // - we know it's IMultiPolygon so we can get at the IPolygon[]
            // - we know just the Xs are stored as double[], so we can loop more efficiently
            IMultiPolygon mp = (IMultiPolygon)new OptimizedWKBReader(NtsGeometryServices.Instance.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.SOA }.Read(this.data);
            double minX = Double.PositiveInfinity;
            foreach (IGeometry geom in mp.Geometries)
            {
                Vector<double> minXs = new Vector<double>(minX);
                IPolygon poly = (IPolygon)geom;
                SOACoordinateSequence seq = (SOACoordinateSequence)poly.Shell.CoordinateSequence;
                Span<double> xs = seq.Xs;
                ref Vector<double> xsVecStart = ref Unsafe.As<double, Vector<double>>(ref MemoryMarshal.GetReference(xs));
                int vecCount = xs.Length / Vector<double>.Count;
                for (int i = 0; i < vecCount; i++)
                {
                    minXs = Vector.Min(minXs, Unsafe.Add(ref xsVecStart, i));
                }

                minX = minXs[0];
                for (int i = 1; i < Vector<double>.Count; i++)
                {
                    if (minXs[i] < minX)
                    {
                        minX = minXs[i];
                    }
                }

                for (int i = vecCount * Vector<double>.Count; i < xs.Length; i++)
                {
                    if (xs[i] < minX)
                    {
                        minX = xs[i];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedRaw_Straightforward()
        {
            // best we can possibly do with our knowledge of the data:
            // - we can access the WKB directly to avoid copying the whole thing
            RawGeometryCollection coll = new RawGeometryCollection(new RawGeometry(this.data));
            double minX = Double.PositiveInfinity;
            foreach (RawGeometry geom in coll)
            {
                RawCoordinateSequence seq = new RawPolygon(geom).GetRing(0);
                foreach (XYCoordinate c in seq)
                {
                    if (c.X < minX)
                    {
                        minX = c.X;
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedRaw_NonPortableCast()
        {
            // best we can possibly do with our knowledge of the data:
            // - we can access the WKB directly to avoid copying the whole thing
            // - this one was tuned for architectures that support reads at unaligned addresses
            RawGeometryCollection coll = new RawGeometryCollection(new RawGeometry(this.data));
            double minX = Double.PositiveInfinity;
            foreach (RawGeometry geom in coll)
            {
                RawCoordinateSequence seq = new RawPolygon(geom).GetRing(0);
                ReadOnlySpan<double> pts = seq.PointData.Slice(4).NonPortableCast<byte, double>();
                for (int j = 0; j < pts.Length; j += 2)
                {
                    if (pts[j] < minX)
                    {
                        minX = pts[j];
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedRaw_MaximumDanger()
        {
            // best we can possibly do with our knowledge of the data:
            // - we can access the WKB directly to avoid copying the whole thing
            // - this one was tuned for architectures that support reads at unaligned addresses
            // - we can also skip the validation that "new" does, because we know it's valid
            RawGeometryCollection coll = default;
            coll.RawGeometry.Data = this.data;
            double minX = Double.PositiveInfinity;
            foreach (RawGeometry geom in coll)
            {
                RawPolygon poly = default;
                poly.RawGeometry = geom;
                RawCoordinateSequence seq = poly.GetRing(0);
                ReadOnlySpan<double> pts = seq.PointData.Slice(4).NonPortableCast<byte, double>();
                for (int j = 0; j < pts.Length; j += 2)
                {
                    if (pts[j] < minX)
                    {
                        minX = pts[j];
                    }
                }
            }

            return minX;
        }

        static void Main()
        {
            var prog = new Program();
            prog.Setup();
            Console.WriteLine(prog.ReadNTSCoordinateArray());
            Console.WriteLine(prog.ReadNTSPackedDouble());
            Console.WriteLine(prog.ReadNTSPackedFloat()); // this one's expected to be a bit off
            Console.WriteLine(prog.ReadOptimizedAOS());
            Console.WriteLine(prog.ReadOptimizedSOA_Scalar());
            Console.WriteLine(prog.ReadOptimizedSOA_Vector());
            Console.WriteLine(prog.ReadOptimizedRaw_Straightforward());
            Console.WriteLine(prog.ReadOptimizedRaw_NonPortableCast());
            Console.WriteLine(prog.ReadOptimizedRaw_MaximumDanger());

            BenchmarkRunner.Run<Program>(
                ManualConfig.Create(
                    DefaultConfig.Instance.With(
                        Job.Default.WithGcServer(true))));
        }
    }
}
