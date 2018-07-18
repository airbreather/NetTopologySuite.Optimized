using System;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.Optimized
{
    internal static class Helpers
    {
        // https://github.com/dotnet/coreclr/blob/cc52c67f5a0a26194c42fbd1b59e284d6727635a/src/System.Private.CoreLib/shared/System/Double.cs#L47-L54
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(this double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
        }

        public static string ToRoundTripString(this double val, IFormatProvider formatProvider)
        {
            string result = val.ToString("R", formatProvider);

            // work around dotnet/coreclr#13106
            if (val.IsFinite())
            {
                if (val != Double.Parse(result, formatProvider))
                {
                    result = val.ToString("G16", formatProvider);
                    if (val != Double.Parse(result, formatProvider))
                    {
                        result = val.ToString("G17", formatProvider);
                    }
                }
            }

            return result;
        }
    }
}
