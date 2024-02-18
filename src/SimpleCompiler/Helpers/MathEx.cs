using System.Numerics;

namespace SimpleCompiler.Helpers;

internal sealed class MathEx
{
    public static T RoundUpDivide<T>(T dividend, T divisor) where T : IBinaryInteger<T> =>
        (dividend + (divisor - T.One)) / divisor;
}
