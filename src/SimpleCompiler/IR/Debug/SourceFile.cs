using System.Text;

namespace SimpleCompiler.IR.Debug;

public sealed record SourceFile(
    string Path,
    string Contents,
    Encoding Encoding);
