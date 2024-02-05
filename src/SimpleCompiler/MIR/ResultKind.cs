namespace SimpleCompiler.MIR;

[Flags]
public enum ResultKind
{
    // Has no result, for unknown use Any.
    None = 0,
    Nil = 0b00001,
    Bool = 0b00010,
    Int = 0b00100,
    Double = 0b01000,
    Str = 0b10000,
    Any = 0b11111,
}

public static class ResultKindExtensions
{
    public static bool IsMixed(this ResultKind result) =>
        result is not (ResultKind.Nil or ResultKind.Bool or ResultKind.Int or ResultKind.Double or ResultKind.Str);
    public static bool HasNil(this ResultKind result) => (result & ResultKind.Nil) != 0;
    public static bool HasBool(this ResultKind result) => (result & ResultKind.Bool) != 0;
    public static bool HasInt(this ResultKind result) => (result & ResultKind.Int) != 0;
    public static bool HasDouble(this ResultKind result) => (result & ResultKind.Double) != 0;
    public static bool HasStr(this ResultKind result) => (result & ResultKind.Str) != 0;
}