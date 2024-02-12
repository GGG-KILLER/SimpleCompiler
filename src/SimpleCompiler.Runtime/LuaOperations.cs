using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToInt(double value) =>
        (long) value == value
        ? (long) value
        : throw new LuaException("Number does not have an integer representation.");
}
