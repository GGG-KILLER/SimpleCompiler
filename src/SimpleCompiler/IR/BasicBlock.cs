using System.Collections.Immutable;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed class BasicBlock(int blockOrdinal, ImmutableArray<Instruction> instructions)
{
    private ImmutableArray<NameValue> _lazyReferencedValues;
    private ImmutableArray<NameValue> _lazyExportedValues;

    /// <summary>
    /// This block's index in the <see cref="IrGraph.BasicBlocks"/>.
    /// </summary>
    public int Ordinal { get; } = blockOrdinal;
    /// <summary>
    /// This block's instructions.
    /// </summary>
    public ImmutableArray<Instruction> Instructions { get; } = instructions;

    /// <summary>
    /// All values that are referenced by this block.
    /// </summary>
    public ImmutableArray<NameValue> ReferencedValues
    {
        get
        {
            if (_lazyReferencedValues.IsDefault)
            {
                ComputeValues(Instructions, out _lazyReferencedValues, out _lazyExportedValues);
            }
            return _lazyReferencedValues;
        }
    }

    /// <summary>
    /// All values that are "exported" (last version of each name that has an assignment) by this block.
    /// </summary>
    public ImmutableArray<NameValue> ExportedValues
    {
        get
        {
            if (_lazyExportedValues.IsDefault)
            {
                ComputeValues(Instructions, out _lazyReferencedValues, out _lazyExportedValues);
            }
            return _lazyExportedValues;
        }
    }

    private static void ComputeValues(IEnumerable<Instruction> instructions, out ImmutableArray<NameValue> referenced, out ImmutableArray<NameValue> exported)
    {
        var allDeclared = new HashSet<NameValue>();
        var allReferenced = new HashSet<NameValue>();

        foreach (var instruction in instructions)
        {
            if (instruction.IsAssignment && instruction.Assignee is not null)
            {
                allDeclared.Add(instruction.Assignee);
            }

            switch (instruction.Kind)
            {
                case InstructionKind.Assignment:
                {
                    var assignment = CastHelper.FastCast<Assignment>(instruction);
                    if (assignment.Operand is NameValue nameValue)
                        allReferenced.Add(nameValue);
                    break;
                }
                case InstructionKind.UnaryAssignment:
                {
                    var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                    if (assignment.Operand is NameValue nameValue)
                        allReferenced.Add(nameValue);
                    break;
                }
                case InstructionKind.BinaryAssignment:
                {
                    var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                    if (assignment.Left is NameValue leftNameValue)
                        allReferenced.Add(leftNameValue);
                    if (assignment.Right is NameValue rightNameValue)
                        allReferenced.Add(rightNameValue);
                    break;
                }
                case InstructionKind.FunctionAssignment:
                {
                    var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                    if (assignment.Callee is NameValue calleeNameValue)
                        allReferenced.Add(calleeNameValue);
                    foreach (var arg in assignment.Arguments)
                    {
                        if (arg is NameValue argNameValue)
                            allReferenced.Add(argNameValue);
                    }
                    break;
                }
                case InstructionKind.PhiAssignment:
                {
                    var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                    foreach ((_, var value) in assignment.Phi.Values)
                        allReferenced.Add(value);
                    break;
                }
                case InstructionKind.Branch:
                    // Nothing to use here.
                    break;
                case InstructionKind.CondBranch:
                {
                    var branch = CastHelper.FastCast<CondBranch>(instruction);
                    if (branch.Operand is NameValue nameValue)
                        allReferenced.Add(nameValue);
                    break;
                }
                default:
                    throw new NotImplementedException($"Value computation not implemented for instruction {instruction.Kind}");
            }
        }

        allReferenced.ExceptWith(allDeclared);

        referenced = [.. allReferenced];
        exported = [.. allDeclared];
    }
}

public sealed class BasicBlockSuccessor
{
    internal BasicBlockSuccessor(Condition? condition, BasicBlock target)
    {
        Condition = condition;
        Target = target;
    }

    public Condition? Condition { get; }
    public BasicBlock Target { get; }
}

public sealed class Condition
{
    internal Condition(Operand left, bool expectedValue)
    {
        Left = left;
        ExpectedValue = expectedValue;
    }

    public Operand Left { get; }
    public bool ExpectedValue { get; }
}
