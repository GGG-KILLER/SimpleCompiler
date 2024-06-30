using SimpleCompiler.IR.Debug;

namespace SimpleCompiler.IR;

public sealed class DebugData
{
    public SourceFile? SourceFile { get; set; }
    public Dictionary<NameValue, string> OriginalValueNames { get; } = [];
}
