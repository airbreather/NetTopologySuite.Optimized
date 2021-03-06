﻿using System;
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
    [MemoryDiagnoser]
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

        [Benchmark(Baseline = true)]
        public unsafe double ReadDirectlyFromArray()
        {
            // this should be very nearly the absolute fastest that managed code can hope to attain
            // from tooling available today.  it takes advantage of our knowledge of the shape of
            // the input data (stopping short at assuming counts of geometries, rings, or points)
            // and assumes that the hardware can handle unaligned reads no worse than we can; modern
            // JITs should be able to emit very high-quality code for this as there's very little
            // complicated stuff going on. using it as a baseline lets us talk about just how much
            // each different kind of abstraction costs us.
            double minX = Double.PositiveInfinity;
            fixed (byte* f_wkb = this.data)
            {
                int geomCount = *(int*)(f_wkb + 5);
                byte* wkb = f_wkb + 9;
                for (int i = 0; i < geomCount; i++)
                {
                    int ringCount = *(int*)(wkb + 5);
                    wkb += 9;
                    for (int j = 0; j < ringCount; j++)
                    {
                        int ptCount = *(int*)wkb;
                        wkb += 4;
                        if (j == 0)
                        {
                            // extreme points are always in the shell (first ring).
                            for (int k = 0; k < ptCount; k++)
                            {
                                double pt = *(double*)wkb;
                                if (pt < minX)
                                {
                                    minX = pt;
                                }

                                wkb += 16;
                            }
                        }
                        else
                        {
                            wkb += ptCount * 16;
                        }
                    }
                }
            }

            return minX;
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

                // extreme points are always in the shell (first ring).
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

                // extreme points are always in the shell (first ring).
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

                // extreme points are always in the shell (first ring).
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

                // extreme points are always in the shell (first ring).
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

                // extreme points are always in the shell (first ring).
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

                // extreme points are always in the shell (first ring).
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
        public double ReadOptimizedRaw_Visitor_Straightforward()
        {
            var vis = new MinXVisitor_Straightforward();
            vis.Visit(new RawGeometry(this.data), VisitMode.Coordinates);
            return vis.MinX;
        }

        private sealed class MinXVisitor_Straightforward : RawGeometryVisitorBase
        {
            public double MinX = Double.PositiveInfinity;

            protected override void OnVisitCoordinate(XYCoordinate coordinate)
            {
                if (coordinate.X < this.MinX)
                {
                    this.MinX = coordinate.X;
                }
            }
        }

        [Benchmark]
        public double ReadOptimizedRaw_Visitor_NonPortableCast()
        {
            var vis = new MinXVisitor_NonPortableCast();
            vis.Visit(new RawGeometry(this.data), VisitMode.CoordinateSequences);
            return vis.MinX;
        }

        private sealed class MinXVisitor_NonPortableCast : RawGeometryVisitorBase
        {
            public double MinX = Double.PositiveInfinity;

            protected override void OnVisitRawCoordinateSequence(RawCoordinateSequence coordinateSequence)
            {
                var coords = coordinateSequence.NonPortableCoordinates;
                for (int i = 0; i < coords.Length; i++)
                {
                    if (coords[i].X < this.MinX)
                    {
                        this.MinX = coords[i].X;
                    }
                }
            }
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
                // extreme points are always in the shell (first ring).
                foreach (XYCoordinate c in new RawPolygon(geom).GetRing(0))
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
                // extreme points are always in the shell (first ring).
                ReadOnlySpan<XYCoordinate> pts = new RawPolygon(geom).GetRing(0).NonPortableCoordinates;
                for (int j = 0; j < pts.Length; j++)
                {
                    if (pts[j].X < minX)
                    {
                        minX = pts[j].X;
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedRaw_SkipValidation()
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

                // extreme points are always in the shell (first ring).
                ReadOnlySpan<XYCoordinate> pts = poly.GetRing(0).NonPortableCoordinates;
                for (int j = 0; j < pts.Length; j++)
                {
                    if (pts[j].X < minX)
                    {
                        minX = pts[j].X;
                    }
                }
            }

            return minX;
        }

        [Benchmark]
        public double ReadOptimizedRaw_Refs()
        {
            // best we can possibly do with our knowledge of the data:
            // - we can access the WKB directly to avoid copying the whole thing
            // - this one was tuned for architectures that support reads at unaligned addresses
            // - we can also skip the validation that "new" does, because we know it's valid
            // - this one's just about the best you can do in "slow span" runtimes without going all
            //   the way down to "just read directly from the array" though using S.R.CS.Unsafe, ref
            //   locals, and the non-portable magic wand should give one pause before doing this.
            RawGeometryCollection coll = default;
            coll.RawGeometry.Data = this.data;
            double minX = Double.PositiveInfinity;
            foreach (RawGeometry geom in coll)
            {
                RawPolygon poly = default;
                poly.RawGeometry = geom;

                // extreme points are always in the shell (first ring).
                ReadOnlySpan<XYCoordinate> pts = poly.GetRing(0).NonPortableCoordinates;
                ref XYCoordinate ptStart = ref MemoryMarshal.GetReference(pts);
                for (int j = 0; j < pts.Length; j++)
                {
                    double x = Unsafe.Add(ref ptStart, j).X;
                    if (x < minX)
                    {
                        minX = x;
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
            Console.WriteLine(prog.ReadNTSPackedFloat()); // this one's expected to be a little off
            Console.WriteLine(prog.ReadOptimizedAOS());
            Console.WriteLine(prog.ReadOptimizedSOA_Scalar());
            Console.WriteLine(prog.ReadOptimizedSOA_Vector());
            Console.WriteLine(prog.ReadOptimizedRaw_Straightforward());
            Console.WriteLine(prog.ReadOptimizedRaw_NonPortableCast());
            Console.WriteLine(prog.ReadOptimizedRaw_SkipValidation());
            Console.WriteLine(prog.ReadDirectlyFromArray());
            Console.WriteLine(prog.ReadOptimizedRaw_Refs());
            Console.WriteLine(prog.ReadOptimizedRaw_Visitor_Straightforward());
            Console.WriteLine(prog.ReadOptimizedRaw_Visitor_NonPortableCast());

            BenchmarkRunner.Run<Program>(
                ManualConfig.Create(
                    DefaultConfig.Instance.With(
                        Job.Core.WithGcServer(true),
                        Job.Clr.WithGcServer(true))));
        }
    }
}
