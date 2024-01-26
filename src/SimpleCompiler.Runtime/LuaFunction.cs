namespace SimpleCompiler.Runtime;

public delegate LuaValue LuaFunction(Span<LuaValue> args);
public delegate LuaValue LuaFunction<TCaptures>(TCaptures captures, Span<LuaValue> args);