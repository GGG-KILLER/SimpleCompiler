using Loretta.CodeAnalysis;
using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenTreeRoot(
    typeof(MIR.MirNode),
    "Mir",
    typeof(MirKind),
    CreateLists = true,
    CreateRewriter = true,
    CreateVisitors = true,
    CreateWalker = true
)]
internal abstract partial class MirNode
{
    protected readonly SyntaxReference? _originalNode;
}

// Needs to be defined otherwise lists don't work.
partial class MirList : MirNode
{
}
