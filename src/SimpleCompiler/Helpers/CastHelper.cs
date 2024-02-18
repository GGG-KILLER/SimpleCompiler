using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Helpers;

internal static class CastHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(value))]
    public static T? FastCast<T>(object? value) where T : class =>
#if DEBUG
        value is null ? null : (T) value;
#else
        Unsafe.As<T>(value);
#endif
}
