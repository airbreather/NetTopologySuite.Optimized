using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized.Algorithm
{
    public static class OptimizedDouglasPeuckerLineSimplifier
    {
        public static int Simplify(ReadOnlySpan<XYCoordinate> inputCoords, Span<XYCoordinate> outputCoords, double distanceTolerance)
        {
            if (!IsFinite(distanceTolerance))
            {
                throw new ArgumentOutOfRangeException(nameof(distanceTolerance), distanceTolerance, "Must be finite (not NaN or infinity)");
            }

            if (inputCoords.Length == 0 || distanceTolerance <= 0)
            {
                return 0;
            }

            if (inputCoords.Length == 1)
            {
                if (outputCoords.IsEmpty)
                {
                    ThrowExceptionForOutputCoordsTooShort();
                }

                outputCoords[0] = inputCoords[0];
                return 1;
            }

            // dotnet/roslyn#25118 is in the way of a straightforward stackalloc optimization here.
            bool[] includeBuffer = ArrayPool<bool>.Shared.Rent(inputCoords.Length);
            try
            {
                Span<bool> includes = new Span<bool>(includeBuffer, 0, inputCoords.Length);
                includes.Clear();

                SimplifyCore(inputCoords, includes, distanceTolerance);

                return outputCoords.Length < inputCoords.Length
                    ? CopyWithLengthChecks(inputCoords, outputCoords, includes)
                    : CopyWithoutLengthChecks(inputCoords, outputCoords, includes);
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(includeBuffer);
            }
        }

        private static int CopyWithLengthChecks(ReadOnlySpan<XYCoordinate> inputCoords, Span<XYCoordinate> outputCoords, ReadOnlySpan<bool> includes)
        {
            int cnt = 0;
            for (int i = 0; i < includes.Length; i++)
            {
                if (!includes[i])
                {
                    continue;
                }

                if (outputCoords.Length == cnt)
                {
                    ThrowExceptionForOutputCoordsTooShort();
                }

                outputCoords[cnt++] = inputCoords[i];
            }

            return cnt;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionForOutputCoordsTooShort() => throw new ArgumentException("Must be large enough to hold all output coordinates.", "outputCoords");

        private static int CopyWithoutLengthChecks(ReadOnlySpan<XYCoordinate> inputCoords, Span<XYCoordinate> outputCoords, ReadOnlySpan<bool> includes)
        {
            int cnt = 0;
            for (int i = 0; i < includes.Length; i++)
            {
                if (!includes[i])
                {
                    continue;
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

        private static void SimplifyCore(ReadOnlySpan<XYCoordinate> coords, Span<bool> includes, double distanceTolerance)
        {
            if (includes.Length == 2)
            {
                includes[0] = true;
                includes[1] = true;
                return;
            }

            FindMax(coords, out double maxDistance, out int maxIndex);

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

        private static void FindMax(ReadOnlySpan<XYCoordinate> coords, out double maxDistance, out int maxIndex)
        {
            LineSegment l = new LineSegment(coords[0], coords[coords.Length - 1]);

            maxDistance = -1;
            maxIndex = 0;
            for (int i = 1; i < coords.Length - 1; i++)
            {
                double distance = l.DistanceTo(coords[i]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }
        }

        private readonly struct LineSegment
        {
            private readonly XYCoordinate A;
            private readonly XYCoordinate B;

            private readonly double dx;
            private readonly double dy;
            private readonly double len2;
            private readonly double len;

            public LineSegment(XYCoordinate p0, XYCoordinate p1)
            {
                A = p0;
                B = p1;

                dx = p1.X - p0.X;
                dy = p1.Y - p0.Y;

                len2 = dx * dx + dy * dy;
                len = Math.Sqrt(len2);
            }

            public double DistanceTo(XYCoordinate p)
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

            private static double Distance(XYCoordinate p0, XYCoordinate p1)
            {
                double dx = p1.X - p0.X;
                double dy = p1.Y - p0.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }
    }
}
