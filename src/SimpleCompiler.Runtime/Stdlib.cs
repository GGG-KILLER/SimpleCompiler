namespace SimpleCompiler.Runtime;

// Functions here need the Lua function signature
public static class Stdlib
{
    public static LuaValue Print(ReadOnlySpan<LuaValue> args)
    {
        var first = true;
        foreach (var value in args)
        {
            if (!first) Console.Write('\t');
            first = false;
            Console.Write(value.ToString());
        }
        Console.WriteLine();
        return LuaValue.Nil;
    }

    public static LuaValue ToString(ReadOnlySpan<LuaValue> args)
    {
        FunctionHelper.Deconstruct(args, out var value, out _);
        return new LuaValue(value.ToString());
    }
}
