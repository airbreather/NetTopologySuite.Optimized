using System;
using System.IO;

using GeoAPI;
using GeoAPI.Geometries;

using NetTopologySuite.Geometries.Implementation;

using Xunit;
using Xunit.Abstractions;

namespace NetTopologySuite.Optimized.Tests
{
    public sealed class WKBTests
    {
        private readonly ITestOutputHelper output;

        private static readonly byte[] wkb = File.ReadAllBytes(@"C:\Users\Joe\dumb.wkb");

        public WKBTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void RoundTripAOS()
        {
            IGeometryServices services = new NtsGeometryServices(PackedCoordinateSequenceFactory.DoubleFactory, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID);
            var reader = new OptimizedWKBReader(services.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.AOS };

            IGeometry geom = reader.Read(wkb);
            ////Assert.True(geom.IsValid);

            ReadOnlySpan<byte> rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(rtWkb.SequenceEqual(wkb));
        }

        [Fact]
        public void RoundTripSOA()
        {
            IGeometryServices services = new NtsGeometryServices(SOACoordinateSequenceFactory.Instance, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID);
            var reader = new OptimizedWKBReader(services.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.SOA };

            IGeometry geom = reader.Read(wkb);
            ////Assert.True(geom.IsValid);

            ReadOnlySpan<byte> rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(rtWkb.SequenceEqual(wkb));
        }
    }
}
