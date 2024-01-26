namespace SimpleCompiler.Runtime;

public delegate LuaValue LuaFunction(ReadOnlySpan<LuaValue> args);
public delegate LuaValue LuaFunction<TCaptures>(TCaptures captures, ReadOnlySpan<LuaValue> args);