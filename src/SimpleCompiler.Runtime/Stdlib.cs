namespace SimpleCompiler.Runtime;

// Functions here need the Lua function signature
public static class Stdlib
{
    public static LuaValue PrintImpl(Span<LuaValue> args)
    {
        var first = true;
        foreach (var value in args)
        {
            if (!first) Console.Write('\t');
            first = false;
            Console.Write(value.ToString());
        }
        return LuaValue.Nil;
    }

    public static readonly LuaFunction Print = PrintImpl;
}
