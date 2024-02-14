namespace SimpleCompiler.IR.Debug;

public readonly record struct SourceLocation(string Path, int StartLine, int StartColumn, int EndLine, int EndColumn);
