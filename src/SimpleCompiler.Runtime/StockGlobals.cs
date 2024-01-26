using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.Runtime;

public static class StockGlobals
{
    [SuppressMessage("Code Quality", "CA2211", Justification = "Not gonna make the runtime use properties unnecessarily.")]
    public static LuaValue Print = new(Stdlib.Print);

    [SuppressMessage("Code Quality", "CA2211", Justification = "Not gonna make the runtime use properties unnecessarily.")]
    public new static LuaValue ToString = new(Stdlib.ToString);
}
