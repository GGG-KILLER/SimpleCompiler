using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.EmptyStatement)]
internal sealed partial class EmptyStatement : Statement
{
}