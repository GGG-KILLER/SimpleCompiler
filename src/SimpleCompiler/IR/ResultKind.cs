namespace SimpleCompiler.IR;

[Flags]
public enum ResultKind
{
    // Has no result, for unknown use Any.
    None = 0,
    Nil = 1 << 0,
    Bool = 1 << 1,
    Int = 1 << 2,
    Double = 1 << 3,
    Str = 1 << 4,
    Func = 1 << 5,
    Any = Nil | Bool | Int | Double | Str | Func,
}

public static class ResultKindExtensions
{
    public static bool IsMixed(this ResultKind result) =>
        result is not (ResultKind.Nil or ResultKind.Bool or ResultKind.Int or ResultKind.Double or ResultKind.Str or ResultKind.Func);
    public static bool HasNil(this ResultKind result) => (result & ResultKind.Nil) != 0;
    public static bool HasBool(this ResultKind result) => (result & ResultKind.Bool) != 0;
    public static bool HasInt(this ResultKind result) => (result & ResultKind.Int) != 0;
    public static bool HasDouble(this ResultKind result) => (result & ResultKind.Double) != 0;
    public static bool HasStr(this ResultKind result) => (result & ResultKind.Str) != 0;
    public static bool HasFunc(this ResultKind result) => (result & ResultKind.Func) != 0;
}
