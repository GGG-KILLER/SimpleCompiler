namespace SimpleCompiler.MIR.Internal;

internal abstract partial class Expression : MirNode
{
    protected readonly ResultKind _resultKind;
}
