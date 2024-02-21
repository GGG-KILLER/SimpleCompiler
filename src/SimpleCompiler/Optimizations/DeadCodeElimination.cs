using SimpleCompiler.Helpers;
using SimpleCompiler.IR;

namespace SimpleCompiler.Optimizations;

public sealed class DeadCodeElimination : IOptimizationPass
{
    public void Execute(IrGraph graph)
    {
        foreach (var block in graph.BasicBlocks)
        {
            for (var node = block.Instructions.First; node is not null;)
            {
                ref var instruction = ref node.ValueRef;
                if (instruction.Kind == InstructionKind.ConditionalBranch)
                {
                    var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                    if (branch.Condition is Constant constantCondition)
                    {
                        var conditionIsFalse = constantCondition.Value is null or false;

                        var removed = conditionIsFalse ? branch.TargetIfTrue : branch.TargetIfFalse;
                        // Turn the instruction into a condition-less branch
                        instruction = new Branch(conditionIsFalse ? branch.TargetIfFalse : branch.TargetIfTrue);
                        // Remove the edge from the current block to the removed target
                        graph.Edges.RemoveAll(x => x.SourceBlockOrdinal == block.Ordinal && x.TargetBlockOrdinal == removed.BlockOrdinal);
                    }
                }
                else if (instruction.IsAssignment && instruction.Kind is not InstructionKind.FunctionAssignment && !graph.FindUses(instruction.Name).Any())
                {
                    var temp = node.Next;
                    block.Instructions.Remove(node);
                    node = temp;

                    // Skip setting next as we've already done it.
                    goto next;
                }

                node = node.Next;
            next:;
            }
        }
    }
}
