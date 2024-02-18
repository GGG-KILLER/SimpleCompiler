using System.Diagnostics;
using SimpleCompiler.IR.Debug;

namespace SimpleCompiler.IR;

public enum InstructionKind
{
    DebugLocation,
    Assignment,
    UnaryAssignment,
    BinaryAssignment,
    FunctionAssignment,
    PhiAssignment,
    Branch,
    ConditionalBranch,
}

[DebuggerDisplay($"{{{nameof(ToRepr)}(),nq}}")]
public abstract class Instruction
{
    public abstract InstructionKind Kind { get; }
    public virtual bool IsAssignment => false;
    public virtual NameValue Assignee
    {
        get => throw new InvalidOperationException("Instruction is not an assignment");
        set => throw new InvalidOperationException("Instruction is not an assignment");
    }
    public virtual IEnumerable<Operand> Operands => [];

    public virtual bool References(Operand operand) => false;
    public abstract Instruction Clone();
    public abstract string ToRepr();
}

public sealed class DebugLocation(SourceLocation location) : Instruction
{
    public override InstructionKind Kind => InstructionKind.DebugLocation;
    public SourceLocation Location { get; set; } = location;

    public override DebugLocation Clone() => new(Location);
    public override string ToRepr() => $"# {Location.Path} {Location.StartLine},{Location.StartColumn}:{Location.EndLine},{Location.EndColumn}";
}

public sealed class Assignment(NameValue name, Operand operand) : Instruction
{
    public override InstructionKind Kind => InstructionKind.Assignment;
    public override bool IsAssignment => true;
    public NameValue Name { get; set; } = name;
    public Operand Value { get; set; } = operand;
    public override NameValue Assignee { get => Name; set => Name = value; }
    public override IEnumerable<Operand> Operands => [Value];

    public override bool References(Operand operand) => Value == operand;
    public override Assignment Clone() => new(Name, Value);
    public override string ToRepr() => $"{Name} = {Value}";
}

public sealed class UnaryAssignment(NameValue name, UnaryOperationKind operationKind, Operand operand) : Instruction
{
    public override InstructionKind Kind => InstructionKind.UnaryAssignment;
    public override bool IsAssignment => true;
    public NameValue Name { get; set; } = name;
    public UnaryOperationKind OperationKind { get; set; } = operationKind;
    public Operand Operand { get; set; } = operand;
    public override NameValue Assignee { get => Name; set => Name = value; }
    public override IEnumerable<Operand> Operands => [Operand];

    public override bool References(Operand operand) => Operand == operand;
    public override UnaryAssignment Clone() => new(Name, OperationKind, Operand);
    public override string ToRepr() =>
        $"{Name} = {OperationKind switch
        {
            UnaryOperationKind.LogicalNegation => "not",
            UnaryOperationKind.BitwiseNegation => "bnot",
            UnaryOperationKind.NumericalNegation => "neg",
            UnaryOperationKind.LengthOf => "len",
            _ => throw new InvalidOperationException($"Invalid unary operation kind {OperationKind}"),
        }} {Operand}";
}

public sealed class BinaryAssignment(NameValue name, Operand left, BinaryOperationKind operationKind, Operand right) : Instruction
{
    public override InstructionKind Kind => InstructionKind.BinaryAssignment;
    public override bool IsAssignment => true;
    public NameValue Name { get; set; } = name;
    public Operand Left { get; set; } = left;
    public BinaryOperationKind OperationKind { get; set; } = operationKind;
    public Operand Right { get; set; } = right;
    public override NameValue Assignee { get => Name; set => Name = value; }
    public override IEnumerable<Operand> Operands => [Left, Right];

    public override bool References(Operand operand) => Left == operand || Right == operand;
    public override BinaryAssignment Clone() => new(Name, Left, OperationKind, Right);
    public override string ToRepr() =>
        $"{Name} = {OperationKind switch
        {
            BinaryOperationKind.Addition => "add",
            BinaryOperationKind.Subtraction => "sub",
            BinaryOperationKind.Multiplication => "mul",
            BinaryOperationKind.Division => "div",
            BinaryOperationKind.IntegerDivision => "idiv",
            BinaryOperationKind.Exponentiation => "pow",
            BinaryOperationKind.Modulo => "mod",
            BinaryOperationKind.Concatenation => "cat",
            BinaryOperationKind.BitwiseAnd => "band",
            BinaryOperationKind.BitwiseOr => "bor",
            BinaryOperationKind.BitwiseXor => "xor",
            BinaryOperationKind.LeftShift => "lsh",
            BinaryOperationKind.RightShift => "rsh",
            BinaryOperationKind.Equals => "eq",
            BinaryOperationKind.NotEquals => "neq",
            BinaryOperationKind.LessThan => "lt",
            BinaryOperationKind.LessThanOrEquals => "lte",
            BinaryOperationKind.GreaterThan => "gt",
            BinaryOperationKind.GreaterThanOrEquals => "gte",
            _ => throw new InvalidOperationException($"Invalid binary operation kind {OperationKind}"),
        }} {Left}, {Right}";
}

public sealed class FunctionAssignment(NameValue name, Operand callee, List<Operand> arguments) : Instruction
{
    public override InstructionKind Kind => InstructionKind.FunctionAssignment;
    public override bool IsAssignment => true;
    public NameValue Name { get; set; } = name;
    public Operand Callee { get; set; } = callee;
    public List<Operand> Arguments { get; } = arguments;
    public override NameValue Assignee { get => Name; set => Name = value; }
    public override IEnumerable<Operand> Operands => [Callee, .. Arguments];

    public override bool References(Operand operand) => Callee == operand || Arguments.Contains(operand);
    public override FunctionAssignment Clone() => new(Name, Callee, [.. Arguments]);
    public override string ToRepr() =>
        $"{Name} = {Callee}({string.Join(", ", Arguments)})";
}

public sealed class PhiAssignment(NameValue name, Phi phi) : Instruction
{
    public override InstructionKind Kind => InstructionKind.PhiAssignment;
    public override bool IsAssignment => true;
    public NameValue Name { get; set; } = name;
    public Phi Phi { get; set; } = phi;
    public override NameValue Assignee { get => Name; set => Name = value; }
    public override IEnumerable<Operand> Operands => Phi.Values.Select(x => x.Value);

    public override bool References(Operand operand) => Phi.Values.Any(x => x.Value == operand);
    public override PhiAssignment Clone() => new(Name, new Phi([.. Phi.Values]));
    public override string ToRepr() => $"{Name} = {Phi}";
}

public sealed class Branch(BranchTarget target) : Instruction
{
    public override InstructionKind Kind => InstructionKind.Branch;
    public BranchTarget Target { get; set; } = target;

    // Branch targets don't need to be cloned because they are "immutable".
    public override Branch Clone() => new(Target);
    public override string ToRepr() => $"br BB{Target.Block.Ordinal}";
}

public sealed class ConditionalBranch(Operand condition, BranchTarget ifTrue, BranchTarget ifFalse) : Instruction
{
    public override InstructionKind Kind => InstructionKind.ConditionalBranch;
    public Operand Condition { get; set; } = condition;
    public BranchTarget TargetIfTrue { get; set; } = ifTrue;
    public BranchTarget TargetIfFalse { get; set; } = ifFalse;
    public override IEnumerable<Operand> Operands => [Condition];

    public override bool References(Operand operand) => Condition == operand;
    // Branch targets don't need to be cloned because they are "immutable".
    public override ConditionalBranch Clone() => new(Condition, TargetIfTrue, TargetIfFalse);
    public override string ToRepr() => $"br BB{TargetIfTrue.Block.Ordinal} if {Condition} else BB{TargetIfFalse.Block.Ordinal}";
}
