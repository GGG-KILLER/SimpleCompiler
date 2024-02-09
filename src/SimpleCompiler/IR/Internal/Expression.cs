namespace SimpleCompiler.IR.Internal;

internal abstract partial class Expression : IrNode
{
    protected readonly ResultKind _resultKind;
}
