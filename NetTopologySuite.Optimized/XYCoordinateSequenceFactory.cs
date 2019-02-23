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
            if (!(coordinates?.Length > 0))
            {
                return XYCoordinateSequence.Empty;
            }

            var seq = new XYCoordinateSequence(coordinates.Length);
            for (int i = 0; i < coordinates.Length; i++)
            {
                seq.RawData[i] = new XYCoordinate(coordinates[i]);
            }

            return seq;
        }

        ICoordinateSequence ICoordinateSequenceFactory.Create(ICoordinateSequence coordSeq) => this.Create(coordSeq);
        public XYCoordinateSequence Create(ICoordinateSequence coordSeq)
        {
            if (!(coordSeq?.Count > 0))
            {
                return XYCoordinateSequence.Empty;
            }

            var seq = new XYCoordinateSequence(coordSeq.Count);
            for (int i = 0; i < seq.Count; i++)
            {
                seq.SetX(i, coordSeq.GetX(i));
                seq.SetY(i, coordSeq.GetY(i));
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

            return size == 0
                ? XYCoordinateSequence.Empty
                : new XYCoordinateSequence(size);
        }

        ICoordinateSequence ICoordinateSequenceFactory.Create(int size, Ordinates ordinates)
        {
            if (ordinates != Ordinates.XY)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinates), ordinates, "Ordinates must be XY");
            }

            return size == 0
                ? XYCoordinateSequence.Empty
                : new XYCoordinateSequence(size);
        }
    }
}
