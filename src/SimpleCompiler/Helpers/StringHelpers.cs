using System.Diagnostics;
using System.Globalization;

namespace SimpleCompiler.Helpers;

internal static class StringHelper
{
    public static string ToSubscript(this int value)
    {
        Span<char> buffer = stackalloc char[/* int.MinValue.ToString().Length */ 11];

        if (!value.TryFormat(buffer, out var written, provider: CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Unable to format number.");
        }

        var num = buffer[..written];
        for (var idx = 0; idx < written; idx++)
        {
            num[idx] = num[idx] switch
            {
                '-' => '\u208B',
                '0' => '\u2080',
                '1' => '\u2081',
                '2' => '\u2082',
                '3' => '\u2083',
                '4' => '\u2084',
                '5' => '\u2085',
                '6' => '\u2086',
                '7' => '\u2087',
                '8' => '\u2088',
                '9' => '\u2089',
                _ => throw new UnreachableException($"Cannot convert char {num[idx]} to ")
            };
        }

        return new string(num);
    }
}
