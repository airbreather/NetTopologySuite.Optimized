using System;
using GeoAPI.Geometries;

namespace NetTopologySuite.Optimized
{
    public sealed class XYCoordinateSequenceFactory : ICoordinateSequenceFactory
    {
        public static readonly XYCoordinateSequenceFactory Instance = new XYCoordinateSequenceFactory();

        private XYCoordinateSequenceFactory() { }

        Ordinates ICoordinateSequenceFactory.Ordinates => Ordinates.XY;

        ICoordinateSequence ICoordinateSequenceFactory.Create(Coordinate[] coordinates) => this.Create(coordinates);
        public XYCoordinateSequence Create(Coordinate[] coordinates)
        {
            var seq = this.Create(coordinates?.Length ?? 0);
            var rawData = seq.RawData;
            for (int i = 0; i < rawData.Length; i++)
            {
                rawData[i] = new XYCoordinate(coordinates[i]);
            }

            return seq;
        }

        ICoordinateSequence ICoordinateSequenceFactory.Create(ICoordinateSequence coordSeq) => this.Create(coordSeq);
        public XYCoordinateSequence Create(ICoordinateSequence coordSeq)
        {
            var seq = this.Create(coordSeq?.Count ?? 0);
            var rawData = seq.RawData;
            for (int i = 0; i < rawData.Length; i++)
            {
                rawData[i] = new XYCoordinate(coordSeq.GetX(i), coordSeq.GetY(i));
            }

            return seq;
        }

        public XYCoordinateSequence Create(ReadOnlySpan<XYCoordinate> coordinates)
        {
            return coordinates.Length == 0
                ? XYCoordinateSequence.Empty
                : new XYCoordinateSequence(coordinates);
        }

        ICoordinateSequence ICoordinateSequenceFactory.Create(int size, int dimension)
        {
            if (dimension != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Dimension must be 2");
            }

            return this.Create(size);
        }

        ICoordinateSequence ICoordinateSequenceFactory.Create(int size, Ordinates ordinates)
        {
            if (ordinates != Ordinates.XY)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinates), ordinates, "Ordinates must be XY");
            }

            return this.Create(size);
        }

        public XYCoordinateSequence Create(int size)
        {
            return size == 0
                ? XYCoordinateSequence.Empty
                : new XYCoordinateSequence(size);
        }
    }
}
