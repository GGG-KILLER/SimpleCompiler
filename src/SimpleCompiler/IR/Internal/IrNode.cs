using Loretta.CodeAnalysis;
using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenTreeRoot(
    typeof(IR.IrNode),
    "Ir",
    typeof(IrKind),
    CreateLists = true,
    CreateRewriter = true,
    CreateVisitors = true,
    CreateWalker = true
)]
internal abstract partial class IrNode
{
    protected readonly SyntaxReference? _originalNode;
}

// Needs to be defined otherwise lists don't work.
internal partial class IrList : IrNode
{
}
