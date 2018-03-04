using System;
using System.IO;

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
            var services = new NtsGeometryServices(PackedCoordinateSequenceFactory.DoubleFactory, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID);
            var factory = services.CreateGeometryFactory();
            var reader = new OptimizedWKBReader(factory) { CoordinatePackingMode = CoordinatePackingMode.AOS };

            IGeometry geom = reader.Read(wkb);
            ////Assert.True(geom.IsValid);

            ReadOnlySpan<byte> rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(rtWkb.SequenceEqual(wkb));

            var geom2 = new RawPolygon(new RawGeometry(wkb)).ToGeoAPI(factory);
            Assert.True(geom.EqualsExact(geom2));
        }

        [Fact]
        public void RoundTripSOA()
        {
            var services = new NtsGeometryServices(SOACoordinateSequenceFactory.Instance, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID);
            var factory = services.CreateGeometryFactory();
            var reader = new OptimizedWKBReader(factory) { CoordinatePackingMode = CoordinatePackingMode.SOA };

            IGeometry geom = reader.Read(wkb);
            ////Assert.True(geom.IsValid);

            ReadOnlySpan<byte> rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(rtWkb.SequenceEqual(wkb));

            var geom2 = new RawPolygon(new RawGeometry(wkb)).ToGeoAPI(factory);
            Assert.True(geom.EqualsExact(geom2));
        }
    }
}
