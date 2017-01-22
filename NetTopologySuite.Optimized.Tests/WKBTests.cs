using System.IO;
using System.Runtime.CompilerServices;

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

            byte[] rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(EqualsData(wkb, rtWkb));
        }

        [Fact]
        public void RoundTripSOA()
        {
            IGeometryServices services = new NtsGeometryServices(SOACoordinateSequenceFactory.Instance, NtsGeometryServices.Instance.DefaultPrecisionModel, NtsGeometryServices.Instance.DefaultSRID);
            var reader = new OptimizedWKBReader(services.CreateGeometryFactory()) { CoordinatePackingMode = CoordinatePackingMode.SOA };

            IGeometry geom = reader.Read(wkb);
            ////Assert.True(geom.IsValid);

            byte[] rtWkb = OptimizedWKBWriter.Write(geom);
            Assert.True(EqualsData(wkb, rtWkb));
        }

        private static bool EqualsData(byte[] first, byte[] second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            int end = first.Length - (first.Length & 7);

            int i;
            for (i = 0; i < end; i += 8)
            {
                if (Unsafe.As<byte, ulong>(ref first[i]) != Unsafe.As<byte, ulong>(ref second[i]))
                {
                    return false;
                }
            }

            if ((first.Length & 4) != 0)
            {
                if (Unsafe.As<byte, uint>(ref first[i]) != Unsafe.As<byte, uint>(ref second[i]))
                {
                    return false;
                }

                i += 4;
            }

            if ((first.Length & 2) != 0)
            {
                if (Unsafe.As<byte, ushort>(ref first[i]) != Unsafe.As<byte, ushort>(ref second[i]))
                {
                    return false;
                }

                i += 2;
            }

            return (first.Length & 1) == 0 || first[i] == second[i];
        }
    }
}
