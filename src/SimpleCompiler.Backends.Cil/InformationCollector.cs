using SimpleCompiler.Helpers;
using SimpleCompiler.IR;

namespace SimpleCompiler.Backends.Cil;

internal static class InformationCollector
{
    public static SymbolTable CollectSymbolInfomation(IrGraph graph)
    {
        var table = new SymbolTable();
        foreach (var block in graph.EnumerateBlocksBreadthFirst().Select(x => graph.BasicBlocks[x]))
        {
            foreach (var instruction in block.Instructions)
            {
                switch (instruction.Kind)
                {
                    case InstructionKind.Assignment:
                    {
                        var assignment = CastHelper.FastCast<Assignment>(instruction);
                        table[assignment.Name].Types = GetOperandType(assignment.Value, table);
                        break;
                    }
                    case InstructionKind.UnaryAssignment:
                    {
                        var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                        table[assignment.Name].Types = OperationFacts.GetOperationOutput(assignment.OperationKind, GetOperandType(assignment.Operand, table));
                        break;
                    }
                    case InstructionKind.BinaryAssignment:
                    {
                        var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                        table[assignment.Name].Types = OperationFacts.GetOperationOutput(
                            assignment.OperationKind,
                            GetOperandType(assignment.Left, table),
                            GetOperandType(assignment.Right, table));
                        break;
                    }
                    case InstructionKind.FunctionAssignment:
                        table[instruction.Name].Types = SymbolType.All;
                        break;
                    case InstructionKind.PhiAssignment:
                    {
                        var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                        table[assignment.Name].Types = assignment.Phi.Values.Aggregate(SymbolType.None, (acc, value) => acc | GetOperandType(value.Value, table));
                        break;
                    }
                }
            }
        }
        return table;
    }

    public static SymbolType GetOperandType(Operand operand, SymbolTable table) =>
        operand switch
        {
            Constant constant => constant.Value switch
            {
                bool => SymbolType.Boolean,
                double => SymbolType.Double,
                long => SymbolType.Long,
                string => SymbolType.String,
                null => SymbolType.Nil,
                _ => throw new NotImplementedException()
            },
            Builtin => SymbolType.Function,
            NameValue name => table[name].Types,
            _ => throw new NotImplementedException()
        };
}
