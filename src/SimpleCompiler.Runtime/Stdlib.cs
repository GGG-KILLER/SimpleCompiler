namespace SimpleCompiler.Runtime;

// Functions here need the Lua function signature
public static class Stdlib
{
    public static LuaValue Assert(ReadOnlySpan<LuaValue> args)
    {
        FunctionHelper.Deconstruct(args, out var condition, out var message, out _);

        if (message.Kind is not (ValueKind.String or ValueKind.Nil))
            throw new LuaException("Argument #2 for assert expects a string or nil.");

        if (!condition.IsTruthy)
        {
            throw new LuaException(message.IsString ? message.AsString() : "Assertion failed.");
        }

        return condition;
    }

    public static LuaValue Type(ReadOnlySpan<LuaValue> args)
    {
        FunctionHelper.Deconstruct(args, out var value, out _);

        return new LuaValue(value.Kind switch
        {
            ValueKind.Nil => "nil",
            ValueKind.Boolean => "bool",
            ValueKind.Long or ValueKind.Double => "number",
            ValueKind.String => "string",
            ValueKind.Function => "function",
            _ => "unknown",
        });
    }

    // TODO: rawequal

    // TODO: ipairs

    // TODO: select

    // TODO: rawget

    // TODO: tonumber

    // NOTE: No load because I'm not gonna embed the compiler into the runtime.

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

    // TODO: rawset

    public static LuaValue Error(ReadOnlySpan<LuaValue> args)
    {
        FunctionHelper.Deconstruct(args, out var message, out _);
        throw new LuaException(message.ToString());
    }

    public static LuaValue ToString(ReadOnlySpan<LuaValue> args)
    {
        FunctionHelper.Deconstruct(args, out var value, out _);
        return new LuaValue(value.ToString());
    }

    // NOTE: No require because that's supposed to be handled before the code is compiled.

    // TODO: getmetatable

    // NOTE: No collectgarbage because we depend on .NET's garbage collector and there's
    //       no way to adapt it well to this function.

    // TODO: warn (no idea what this does yet)

    // NOTE: No loadfile because it's supposed to be handled before the code is compiled.

    // TODO: rawlen

    // TODO: next

    // TODO: setmetatable

    // TODO: pairs

    // TODO: pcall

    // TODO: xpcall
}
