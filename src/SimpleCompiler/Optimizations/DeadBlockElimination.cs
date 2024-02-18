using System.Collections.Immutable;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;

namespace SimpleCompiler.Optimizations;

public sealed class DeadBlockElimination : IOptimizationPass
{
    public void Execute(IrGraph graph)
    {
        var placeholder = new BasicBlock(-1, []);

        var phis = graph.BasicBlocks.SelectMany(x => x.Instructions).OfType<PhiAssignment>().ToImmutableArray();

        var initialCount = graph.BasicBlocks.Count;
        var toRemove = new List<int>();
        do
        {
            toRemove.Clear();

            for (var ordinal = 0; ordinal < graph.BasicBlocks.Count; ordinal++)
            {
                var block = graph.BasicBlocks[ordinal];
                // Find blocks that aren't reachable by other blocks
                if (block.Ordinal != graph.EntryBlock.Ordinal
                    && block != placeholder
                    && !graph.Edges.Any(x => x.TargetBlockOrdinal == block.Ordinal && x.SourceBlockOrdinal != block.Ordinal))
                {
                    toRemove.Add(ordinal);
                }
            }

            foreach (var ordinal in toRemove)
            {
                var block = graph.BasicBlocks[ordinal];
                graph.BasicBlocks[ordinal] = placeholder;
                graph.Edges.RemoveAll(x => x.SourceBlockOrdinal == ordinal);
                RemoveBlock(graph, block.Ordinal);
            }
        }
        while (toRemove.Count > 0);

        // Remove jump only blocks.
        do
        {
            toRemove.Clear();

            for (var ordinal = 0; ordinal < graph.BasicBlocks.Count; ordinal++)
            {
                var block = graph.BasicBlocks[ordinal];
                if (block.Instructions.NonDebugCount() == 1 && block.Instructions.FirstNonDebug()?.Kind == InstructionKind.Branch)
                {
                    toRemove.Add(ordinal);
                }
            }

            foreach (var ordinal in toRemove)
            {
                var block = graph.BasicBlocks[ordinal];

                // Retarget the jumps to the block's target.
                var branch = CastHelper.FastCast<Branch>(block.Instructions.FirstNonDebug()!);
                RetargetBlocks(graph, ordinal, branch.Target.BlockOrdinal);
                for (var idx = graph.Edges.Count - 1; idx >= 0; idx--)
                {
                    if (graph.Edges[idx].TargetBlockOrdinal == block.Ordinal)
                        graph.Edges[idx] = graph.Edges[idx] with { TargetBlockOrdinal = branch.Target.BlockOrdinal };
                }
                graph.Edges.RemoveAll(x => x.SourceBlockOrdinal == block.Ordinal);

                // Remove the block
                graph.BasicBlocks[ordinal] = placeholder;
                RemoveBlock(graph, block.Ordinal);
            }
        }
        while (toRemove.Count > 0);

        // TODO: Fix phis.
        // // Join together blocks.
        // var toJoin = new List<(int First, int Second)>();
        // do
        // {
        //     toJoin.Clear();

        //     foreach (var block in graph.BasicBlocks)
        //     {
        //         // If block only has 1 successor
        //         if (graph.Edges.Count(x => x.SourceBlockOrdinal == block.Ordinal) == 1)
        //         {
        //             var successor = graph.Edges.Single(x => x.SourceBlockOrdinal == block.Ordinal).TargetBlockOrdinal;
        //             // And successor only has block as predecessor
        //             if (graph.Edges.Count(x => x.TargetBlockOrdinal == successor) == 1)
        //             {
        //                 // Then join both of them
        //                 toJoin.Add((block.Ordinal, successor));
        //             }
        //         }
        //     }

        //     foreach (var pair in toJoin)
        //     {
        //         var (firstOrdinal, secondOrdinal) = pair;

        //         // Handle case when first was already joined with someone else.
        //         if (toJoin.Any(x => x.Second == firstOrdinal))
        //             firstOrdinal = toJoin.Single(x => x.Second == firstOrdinal).First;

        //         var instructions = new List<Instruction>();
        //         var firstBlock = graph.BasicBlocks[firstOrdinal];
        //         var secondBlock = graph.BasicBlocks[secondOrdinal];

        //         firstBlock.Instructions.RemoveLast(); // Remove jump to 2nd block

        //         var phi = firstBlock.Instructions.FindLastPhi();
        //         foreach (var instruction in secondBlock.Instructions)
        //         {
        //             if (instruction.Kind == InstructionKind.PhiAssignment)
        //             {
        //                 if (phi is not null)
        //                     phi = firstBlock.Instructions.AddAfter(phi, instruction);
        //                 else
        //                     phi = firstBlock.Instructions.AddFirst(instruction);
        //             }
        //             else
        //             {
        //                 firstBlock.Instructions.AddLast(instruction);
        //             }
        //         }

        //         // Remove the edge that went from first to second
        //         graph.Edges.RemoveAll(x => x.SourceBlockOrdinal == firstOrdinal);

        //         // Rewrite the edges that came from second to come from first
        //         for (var idx = 0; idx < graph.Edges.Count; idx++)
        //         {
        //             if (graph.Edges[idx].SourceBlockOrdinal == secondOrdinal)
        //                 graph.Edges[idx] = graph.Edges[idx] with { SourceBlockOrdinal = firstOrdinal };
        //         }

        //         // Replace second with placeholder
        //         graph.BasicBlocks[secondOrdinal] = placeholder;
        //     }
        // }
        // while (toJoin.Count > 0);

        if (graph.BasicBlocks.Contains(placeholder))
        {
            for (var idx = 0; idx < graph.BasicBlocks.Count; idx++)
            {
                if (graph.BasicBlocks[idx] == placeholder)
                {
                    graph.BasicBlocks.RemoveAt(idx);
                    idx--; // Re-visit index.
                    continue;
                }

                var previous = graph.BasicBlocks[idx].Ordinal;
                graph.BasicBlocks[idx] = new BasicBlock(idx, graph.BasicBlocks[idx].Instructions);
                for (var i = 0; i < graph.Edges.Count; i++)
                {
                    var edge = graph.Edges[i];
                    if (edge.SourceBlockOrdinal == previous || edge.TargetBlockOrdinal == previous)
                    {
                        graph.Edges[i] = new IrEdge(
                            edge.SourceBlockOrdinal == previous ? idx : edge.SourceBlockOrdinal,
                            edge.TargetBlockOrdinal == previous ? idx : edge.TargetBlockOrdinal);
                    }
                }
                RetargetBlocks(graph, previous, idx);
            }
        }
    }

    private static void RemoveBlock(IrGraph graph, int ordinal)
    {
        foreach (var block in graph.BasicBlocks)
        {
            foreach (var node in block.Instructions.Nodes())
            {
                ref var instruction = ref node.ValueRef;
                if (instruction.Kind == InstructionKind.PhiAssignment)
                {
                    var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                    for (var idx = assignment.Phi.Values.Count - 1; idx >= 0; idx--)
                    {
                        if (assignment.Phi.Values[idx].SourceBlockOrdinal == ordinal)
                            assignment.Phi.Values.RemoveAt(idx);
                    }

                    if (assignment.Phi.Values.Count == 1)
                    {
                        instruction = new Assignment(assignment.Name, assignment.Phi.Values[0].Value);
                    }
                }
                else if (instruction.Kind == InstructionKind.Branch)
                {
                    var branch = CastHelper.FastCast<Branch>(instruction);
                    if (branch.Target.BlockOrdinal == ordinal)
                        throw new InvalidOperationException($"Cannot remove a block (BB{ordinal}) that has jumps towards it.");
                }
                else if (instruction.Kind == InstructionKind.ConditionalBranch)
                {
                    var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                    if (branch.TargetIfTrue.BlockOrdinal == ordinal)
                        throw new InvalidOperationException($"Cannot remove a block (BB{ordinal}) that has jumps towards it.");
                    if (branch.TargetIfFalse.BlockOrdinal == ordinal)
                        throw new InvalidOperationException($"Cannot remove a block (BB{ordinal}) that has jumps towards it.");
                }
            }
        }
    }
    private static void RetargetBlocks(IrGraph graph, int previous, int current)
    {
        foreach (var block in graph.BasicBlocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Kind == InstructionKind.PhiAssignment)
                {
                    var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                    for (var idx = 0; idx < assignment.Phi.Values.Count; idx++)
                    {
                        if (assignment.Phi.Values[idx].SourceBlockOrdinal == previous)
                            assignment.Phi.Values[idx] = assignment.Phi.Values[idx] with { SourceBlockOrdinal = current };
                    }
                }
                else if (instruction.Kind == InstructionKind.Branch)
                {
                    var branch = CastHelper.FastCast<Branch>(instruction);
                    if (branch.Target.BlockOrdinal == previous)
                        branch.Target = new BranchTarget(current);
                }
                else if (instruction.Kind == InstructionKind.ConditionalBranch)
                {
                    var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                    if (branch.TargetIfTrue.BlockOrdinal == previous)
                        branch.TargetIfTrue = new BranchTarget(current);
                    if (branch.TargetIfFalse.BlockOrdinal == previous)
                        branch.TargetIfFalse = new BranchTarget(current);
                }
            }
        }
    }
}