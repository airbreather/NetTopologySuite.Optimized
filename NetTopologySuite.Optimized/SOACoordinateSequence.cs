using System;
using System.Runtime.CompilerServices;

using GeoAPI.Geometries;

using NetTopologySuite.Geometries.Implementation;

namespace NetTopologySuite.Optimized
{
    public sealed class SOACoordinateSequence : ICoordinateSequence
    {
        private readonly double[] xs;
        private readonly double[] ys;

        public SOACoordinateSequence(int length)
        {
            this.xs = length == 0 ? Array.Empty<double>() : new double[length];
            this.ys = length == 0 ? Array.Empty<double>() : new double[length];
        }

        public SOACoordinateSequence(ICoordinateSequence seq)
        {
            switch (seq)
            {
                case SOACoordinateSequence soaSeq:
                    this.xs = CloneVals(soaSeq.xs);
                    this.ys = CloneVals(soaSeq.ys);
                    break;

                case PackedDoubleCoordinateSequence aosSeq:
                    int dim = aosSeq.Dimension;
                    double[] coords = aosSeq.GetRawCoordinates();
                    this.xs = new double[aosSeq.Count];
                    this.ys = new double[aosSeq.Count];
                    for (int i = 0, j = 0; i < coords.Length; i += dim, j++)
                    {
                        this.xs[j] = coords[i + 0];
                        this.ys[j] = coords[i + 1];
                    }

                    break;

                default:
                    this.xs = new double[seq.Count];
                    this.ys = new double[this.xs.Length];
                    for (int i = 0; i < this.xs.Length; i++)
                    {
                        this.xs[i] = seq.GetX(i);
                        this.ys[i] = seq.GetY(i);
                    }

                    break;
            }
        }

        public SOACoordinateSequence(ReadOnlySpan<Coordinate> coords)
        {
            this.xs = new double[coords.Length];
            this.ys = new double[coords.Length];
            for (int i = 0; i < coords.Length; i++)
            {
                Coordinate coord = coords[i];
                this.xs[i] = coord.X;
                this.ys[i] = coord.Y;
            }
        }

        private SOACoordinateSequence(double[] xs, double[] ys)
        {
            this.xs = xs;
            this.ys = ys;
        }

        public int Count => this.xs.Length;

        public Span<double> Xs => this.xs;
        public Span<double> Ys => this.ys;

        int ICoordinateSequence.Dimension => 2;
        Ordinates ICoordinateSequence.Ordinates => Ordinates.XY;

        object ICloneable.Clone() => this.Copy();
        ICoordinateSequence ICoordinateSequence.Copy() => this.Copy();
        public SOACoordinateSequence Copy() => new SOACoordinateSequence(CloneVals(this.xs), CloneVals(this.ys));

        public void CopyTo(Span<double> outXs, Span<double> outYs)
        {
            if ((outXs.Length < this.xs.Length) | (outYs.Length < this.ys.Length))
            {
                ThrowArgumentExceptionForBadLength();
            }

            this.xs.AsReadOnlySpan().CopyTo(outXs.Slice(0, this.xs.Length));
            this.ys.AsReadOnlySpan().CopyTo(outYs.Slice(0, this.ys.Length));
        }

        public Envelope ExpandEnvelope(Envelope env)
        {
            for (int i = 0; i < this.xs.Length; i++)
            {
                env.ExpandToInclude(this.xs[i], this.ys[i]);
            }

            return env;
        }

        public Coordinate GetCoordinate(int i) => this.GetCoordinateCopy(i);

        public Coordinate GetCoordinateCopy(int i)
        {
            Coordinate c = new Coordinate();
            this.GetCoordinate(i, c);
            return c;
        }

        public void GetCoordinate(int index, Coordinate coord)
        {
            coord.X = this.xs[index];
            coord.Y = this.ys[index];
        }

        public double GetOrdinate(int index, Ordinate ordinate)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    return this.xs[index];

                case Ordinate.Y:
                    return this.ys[index];
            }

            ThrowArgumentOutOfRangeExceptionForBadOrdinate();
            return 0;
        }

        public double GetX(int index) => this.xs[index];
        public double GetY(int index) => this.ys[index];

        ICoordinateSequence ICoordinateSequence.Reversed() => this.Reversed();
        public SOACoordinateSequence Reversed()
        {
            SOACoordinateSequence cloned = this.Copy();
            cloned.xs.AsSpan().Reverse();
            cloned.ys.AsSpan().Reverse();
            return cloned;
        }

        public void SetOrdinate(int index, Ordinate ordinate, double value)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    this.xs[index] = value;
                    return;

                case Ordinate.Y:
                    this.ys[index] = value;
                    return;
            }

            ThrowArgumentOutOfRangeExceptionForBadOrdinate();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForBadOrdinate() => throw new ArgumentOutOfRangeException("ordinate", "Must be X or Y");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadLength() => throw new ArgumentException("Not enough room");

        public Coordinate[] ToCoordinateArray()
        {
            var result = new Coordinate[this.xs.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Coordinate(this.xs[i], this.ys[i]);
            }

            return result;
        }

        private static double[] CloneVals(ReadOnlySpan<double> val) => val.ToArray();
    }

    public sealed class SOACoordinateSequenceFactory : ICoordinateSequenceFactory
    {
        public static readonly SOACoordinateSequenceFactory Instance = new SOACoordinateSequenceFactory();

        private SOACoordinateSequenceFactory() { }

        public Ordinates Ordinates => Ordinates.XY;

        public ICoordinateSequence Create(ICoordinateSequence coordSeq) => new SOACoordinateSequence(coordSeq);
        public ICoordinateSequence Create(Coordinate[] coordinates) => new SOACoordinateSequence(coordinates);
        public ICoordinateSequence Create(int size, Ordinates ordinates)
        {
            if (ordinates != Ordinates.XY)
            {
                ThrowArgumentExceptionForBadOrdinates();
            }

            return new SOACoordinateSequence(size);
        }

        public ICoordinateSequence Create(int size, int dimension)
        {
            if (dimension != 2)
            {
                ThrowArgumentExceptionForBadDimension();
            }

            return new SOACoordinateSequence(size);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadOrdinates() => throw new ArgumentException("Must be XY", "ordinates");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentExceptionForBadDimension() => throw new ArgumentException("Must be 2", "dimension");
    }
}
