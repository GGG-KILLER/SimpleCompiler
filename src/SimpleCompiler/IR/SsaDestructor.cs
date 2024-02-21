
using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed class SsaDestructor
{
    public static void DestructSsa(IrGraph graph) =>
        new SsaDestructor(graph).Destruct();
    private readonly IrGraph _graph;

    private SsaDestructor(IrGraph graph)
    {
        _graph = graph;
    }

    private void Destruct()
    {
        foreach (var block in _graph.BasicBlocks)
        {
            var node = block.Instructions.First;
            while (node is not null)
            {
                if (node.Value.Kind == InstructionKind.PhiAssignment)
                {
                    var instruction = CastHelper.FastCast<PhiAssignment>(node.Value);

                    foreach (var (blockOrdinal, value) in instruction.Phi.Values)
                    {
                        var assignment = new Assignment(instruction.Name, value);

                        var instructions = _graph.BasicBlocks[blockOrdinal].Instructions;
                        // Last instruction in a basic block is a branch or return.
                        instructions.AddBefore(instructions.Last!, assignment);
                    }

                    var next = node.Next;
                    block.Instructions.Remove(node);
                    node = next;
                }
                else
                {
                    node = node.Next;
                }
            }
        }
    }
}
