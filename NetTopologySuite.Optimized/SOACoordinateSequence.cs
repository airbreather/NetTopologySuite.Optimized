using System;

using GeoAPI.Geometries;

namespace NetTopologySuite.Optimized
{
    public sealed class SOACoordinateSequence : ICoordinateSequence
    {
        public readonly double[] Xs;
        public readonly double[] Ys;

        public SOACoordinateSequence(int length)
        {
            this.Xs = length == 0 ? Array.Empty<double>() : new double[length];
            this.Ys = length == 0 ? Array.Empty<double>() : new double[length];
        }

        public SOACoordinateSequence(ICoordinateSequence seq)
        {
            if (seq is SOACoordinateSequence soaSeq)
            {
                this.Xs = CloneVals(soaSeq.Xs);
                this.Ys = CloneVals(soaSeq.Ys);
                return;
            }

            this.Xs = new double[seq.Count];
            this.Ys = new double[this.Xs.Length];
            for (int i = 0; i < this.Xs.Length; i++)
            {
                this.Xs[i] = seq.GetX(i);
                this.Ys[i] = seq.GetY(i);
            }
        }

        public SOACoordinateSequence(Coordinate[] coords)
        {
            this.Xs = new double[coords.Length];
            this.Ys = new double[coords.Length];
            for (int i = 0; i < coords.Length; i++)
            {
                Coordinate coord = coords[i];
                this.Xs[i] = coord.X;
                this.Ys[i] = coord.Y;
            }
        }

        private SOACoordinateSequence(double[] xs, double[] ys)
        {
            this.Xs = xs;
            this.Ys = ys;
        }

        public int Count => this.Xs.Length;

        int ICoordinateSequence.Dimension => 2;
        Ordinates ICoordinateSequence.Ordinates => Ordinates.XY;

        object GeoAPI.ICloneable.Clone() => this.Clone();
        public SOACoordinateSequence Clone() => new SOACoordinateSequence(CloneVals(this.Xs), CloneVals(this.Ys));

        public Envelope ExpandEnvelope(Envelope env)
        {
            for (int i = 0; i < this.Xs.Length; i++)
            {
                env.ExpandToInclude(this.Xs[i], this.Ys[i]);
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
            coord.X = this.Xs[index];
            coord.Y = this.Ys[index];
        }

        public double GetOrdinate(int index, Ordinate ordinate)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    return this.Xs[index];

                case Ordinate.Y:
                    return this.Ys[index];
            }

            throw new ArgumentOutOfRangeException(nameof(ordinate), ordinate, "Must be X or Y");
        }

        public double GetX(int index) => this.Xs[index];
        public double GetY(int index) => this.Ys[index];

        ICoordinateSequence ICoordinateSequence.Reversed() => this.Reversed();
        public SOACoordinateSequence Reversed()
        {
            SOACoordinateSequence cloned = this.Clone();
            Array.Reverse(cloned.Xs);
            Array.Reverse(cloned.Ys);
            return cloned;
        }

        public void SetOrdinate(int index, Ordinate ordinate, double value)
        {
            switch (ordinate)
            {
                case Ordinate.X:
                    this.Xs[index] = value;
                    return;

                case Ordinate.Y:
                    this.Ys[index] = value;
                    return;
            }

            throw new ArgumentOutOfRangeException(nameof(ordinate), ordinate, "Must be X or Y");
        }

        public Coordinate[] ToCoordinateArray()
        {
            var result = new Coordinate[this.Xs.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Coordinate(this.Xs[i], this.Ys[i]);
            }

            return result;
        }

        private static unsafe double[] CloneVals(double[] val)
        {
            double[] result = new double[val.Length];
            fixed (void* fromPtr = val)
            fixed (void* toPtr = result)
            {
                Buffer.MemoryCopy(fromPtr, toPtr, val.Length * 8, val.Length * 8);
            }

            return result;
        }
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
                throw new ArgumentException("Must be XY", nameof(ordinates));
            }

            return new SOACoordinateSequence(size);
        }

        public ICoordinateSequence Create(int size, int dimension)
        {
            if (dimension != 2)
            {
                throw new ArgumentException("Must be 2", nameof(dimension));
            }

            return new SOACoordinateSequence(size);
        }
    }
}
