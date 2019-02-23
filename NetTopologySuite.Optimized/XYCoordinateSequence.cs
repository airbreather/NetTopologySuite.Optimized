using System;
using GeoAPI.Geometries;

namespace NetTopologySuite.Optimized
{
    public sealed class XYCoordinateSequence : ICoordinateSequence
    {
        internal static readonly XYCoordinateSequence Empty = new XYCoordinateSequence(0);

        internal XYCoordinateSequence(int size)
        {
            this.RawData = size > 0
                ? new XYCoordinate[size]
                : Array.Empty<XYCoordinate>();
        }

        internal XYCoordinateSequence(ReadOnlySpan<XYCoordinate> rawData)
            : this(rawData.Length)
        {
            rawData.CopyTo(this.RawData);
        }

        public XYCoordinate[] RawData { get; } = Array.Empty<XYCoordinate>();

        public int Count => this.RawData.Length;

        int ICoordinateSequence.Dimension => 2;
        Ordinates ICoordinateSequence.Ordinates => Ordinates.XY;

        object ICloneable.Clone() => this.Copy();
        ICoordinateSequence ICoordinateSequence.Copy() => this.Copy();
        public XYCoordinateSequence Copy()
        {
            return this.Count == 0
                ? Empty
                : new XYCoordinateSequence(this.RawData);
        }

        public Envelope ExpandEnvelope(Envelope env)
        {
            if (env is null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            foreach (var coord in this.RawData)
            {
                env.ExpandToInclude(coord.X, coord.Y);
            }

            return env;
        }

        public void GetCoordinate(int index, Coordinate coord) => (coord.X, coord.Y) = this.RawData[index];

        public Coordinate GetCoordinate(int i) => new Coordinate(this.RawData[i].X, this.RawData[i].Y);

        public Coordinate GetCoordinateCopy(int i) => new Coordinate(this.RawData[i].X, this.RawData[i].Y);

        public Coordinate[] ToCoordinateArray()
        {
            var rawData = this.RawData;
            var result = new Coordinate[rawData.Length];
            for (int i = 0; i < rawData.Length; i++)
            {
                result[i] = new Coordinate(rawData[i].X, rawData[i].Y);
            }

            return result;
        }

        double ICoordinateSequence.GetOrdinate(int index, Ordinate ordinate)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    return this.GetX(index);

                case Ordinate.Y:
                    return this.GetY(index);

                default:
                    return Coordinate.NullOrdinate;
            }
        }

        public double GetX(int index) => this.RawData[index].X;

        public double GetY(int index) => this.RawData[index].Y;

        void ICoordinateSequence.SetOrdinate(int index, Ordinate ordinate, double value)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    this.SetX(index, value);
                    break;

                case Ordinate.Y:
                    this.SetY(index, value);
                    break;
            }
        }

        public void SetX(int index, double value) => this.RawData[index].X = value;

        public void SetY(int index, double value) => this.RawData[index].Y = value;

        ICoordinateSequence ICoordinateSequence.Reversed() => this.Reversed();
        public XYCoordinateSequence Reversed()
        {
            var result = this.Copy();
            Array.Reverse(result.RawData);
            return result;
        }
    }
}
