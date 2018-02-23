using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized
{
    public static class OptimizedDouglasPeuckerLineSimplifier
    {
        public static int Simplify(ReadOnlySpan<Raw.Coordinate> inputCoords, Span<Raw.Coordinate> outputCoords, double distanceTolerance)
        {
            if (!IsFinite(distanceTolerance))
            {
                throw new ArgumentOutOfRangeException(nameof(distanceTolerance), distanceTolerance, "Must be finite (not NaN or infinity)");
            }

            if (distanceTolerance <= 0)
            {
                return 0;
            }

            Span<bool> includes;
            switch (inputCoords.Length)
            {
                case 0:
                    return 0;

                case 1:
                case 2:
                    includes = stackalloc bool[inputCoords.Length];
                    includes[0] = true;
                    includes[includes.Length - 1] = true;
                    break;

                default:
                    if (inputCoords.Length < 1024)
                    {
                        includes = stackalloc bool[inputCoords.Length];
                        includes.Fill(false);
                    }
                    else
                    {
                        includes = new bool[inputCoords.Length];
                    }

                    SimplifyCore(inputCoords, includes, distanceTolerance);
                    break;
            }

            int cnt = 0;
            for (int i = 0; i < includes.Length; i++)
            {
                if (!includes[i])
                {
                    continue;
                }

                if (outputCoords.Length <= cnt)
                {
                    throw new ArgumentException("Must be large enough to hold all output coordinates.", nameof(outputCoords));
                }

                outputCoords[cnt++] = inputCoords[i];
            }

            return cnt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFinite(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
        }

        private static void SimplifyCore(ReadOnlySpan<Raw.Coordinate> coords, Span<bool> includes, double distanceTolerance)
        {
            if (includes.Length == 2)
            {
                includes[0] = true;
                includes[1] = true;
                return;
            }

            // work around dotnet/coreclr#16470 by making copies.
            Raw.Coordinate p0 = new Raw.Coordinate(coords[0]);
            Raw.Coordinate p1 = new Raw.Coordinate(coords[coords.Length - 1]);
            LineSegment l = new LineSegment(p0, p1);

            double maxDistance = -1;
            int maxIndex = 0;
            for (int i = 1; i < coords.Length - 1; i++)
            {
                double distance = l.DistanceTo(coords[i]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            if (maxDistance <= distanceTolerance)
            {
                includes[0] = true;
                includes[includes.Length - 1] = true;
            }
            else
            {
                SimplifyCore(coords.Slice(0, maxIndex + 1), includes.Slice(0, maxIndex + 1), distanceTolerance);
                SimplifyCore(coords.Slice(maxIndex), includes.Slice(maxIndex), distanceTolerance);
            }
        }

        private struct LineSegment
        {
            private Raw.Coordinate A;
            private Raw.Coordinate B;

            private double dx;
            private double dy;
            private double len2;
            private double len;

            public LineSegment(Raw.Coordinate p0, Raw.Coordinate p1)
            {
                A = p0;
                B = p1;

                dx = p1.X - p0.X;
                dy = p1.Y - p0.Y;

                len2 = dx * dx + dy * dy;
                len = Math.Sqrt(len2);
            }

            public double DistanceTo(Raw.Coordinate p)
            {
                if (len2 == 0)
                {
                    return Distance(p, A);
                }

                double r = ((p.X - A.X) * dx + (p.Y - A.Y) * dy) / len2;

                if (r <= 0)
                {
                    return Distance(p, A);
                }

                if (r >= 1)
                {
                    return Distance(p, B);
                }

                double s = ((A.Y - p.Y) * dx - (A.X - p.X) * dy) / len2;

                return Math.Abs(s) * len;
            }

            private static double Distance(Raw.Coordinate p0, Raw.Coordinate p1)
            {
                double dx = p1.X - p0.X;
                double dy = p1.Y - p0.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }
    }
}
