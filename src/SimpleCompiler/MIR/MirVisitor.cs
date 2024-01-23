using System.CodeDom.Compiler;

namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeVisitor(typeof(MirNode))]
public abstract partial class MirVisitor
{
}

[Tsu.TreeSourceGen.TreeVisitor(typeof(MirNode))]
public abstract partial class MirVisitor<TReturn>
{
}

public abstract class MirWalker : MirVisitor
{
    protected override void DefaultVisit(MirNode node)
    {
        foreach (var child in node.GetChildren())
        {
            Visit(child);
        }
    }
}

public sealed class MirDebugPrinter(IndentedTextWriter writer) : MirWalker
{
    protected override void DefaultVisit(MirNode node)
    {
        writer.WriteLine('{');
        writer.Indent++;
        base.DefaultVisit(node);
        writer.Indent--;
        writer.WriteLine('}');
    }

    public override void VisitAssignment(Assignment assignment)
    {
        Visit(assignment.Assignee);
        writer.Write(" = ");
        Visit(assignment.Value);
        writer.WriteLine(';');
    }

    public override void VisitBinaryOperation(BinaryOperation binaryOperation)
    {
        Visit(binaryOperation.Left);
        writer.Write(binaryOperation.BinaryOperationKind switch
        {
            BinaryOperationKind.Addition => " + ",
            BinaryOperationKind.Subtraction => " - ",
            BinaryOperationKind.Multiplication => " * ",
            BinaryOperationKind.IntegerDivision => " // ",
            BinaryOperationKind.BitwiseAnd => " & ",
            BinaryOperationKind.BitwiseOr => " | ",
            BinaryOperationKind.BitwiseXor => " ~ ",
            BinaryOperationKind.Modulo => " % ",
            BinaryOperationKind.Concatenation => " .. ",
            BinaryOperationKind.Exponentiation => " ^ ",
            BinaryOperationKind.BooleanAnd => " and ",
            BinaryOperationKind.BooleanOr => " or ",
            BinaryOperationKind.GreaterThan => " > ",
            BinaryOperationKind.GreaterThanOrEquals => " >= ",
            BinaryOperationKind.LessThan => " < ",
            BinaryOperationKind.LessThanOrEquals => " <= ",
            BinaryOperationKind.LeftShift => " << ",
            BinaryOperationKind.RightShift => " >> ",
            BinaryOperationKind.Equals => " == ",
            BinaryOperationKind.NotEquals => " ~= ",
            BinaryOperationKind.Division => " / ",
            _ => throw ExceptionUtil.Unreachable,
        });
        Visit(binaryOperation.Right);
    }

    public override void VisitConstant(Constant constant)
    {
        writer.Write(constant.ConstantKind switch
        {
            ConstantKind.Nil => "nil",
            ConstantKind.Boolean => ((bool)constant.Value).ToString(),
            ConstantKind.Number => ((double)constant.Value).ToString(),
            ConstantKind.String => '"' + (string)constant.Value + '"',
            _ => throw ExceptionUtil.Unreachable
        });
    }

    public override void VisitEmptyStatement(EmptyStatement emptyStatement) =>
        writer.WriteLine(';');

    public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        Visit(expressionStatement.Expression);
        writer.WriteLine(';');
    }

    public override void VisitFunctionCall(FunctionCall functionCall)
    {
        Visit(functionCall.Callee);
        writer.Write('(');
        var first = true;
        foreach (var arg in functionCall.Arguments)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                writer.Write(", ");
            }
            Visit(arg);
        }
        writer.Write(')');
    }

    public override void VisitStatementList(StatementList statementList)
    {
        writer.WriteLine($"0x{statementList.ScopeInfo?.GetHashCode() ?? 0:X} (Variables: [{string.Join(", ", statementList.ScopeInfo?.DeclaredVariables.Select(s => $"0x{s.GetHashCode():X}") ?? [])}], Scopes: [{string.Join(", ", statementList.ScopeInfo?.ChildScopes.Select(s => $"0x{s.GetHashCode():X}") ?? [])}]) {{");
        writer.Indent++;
        foreach (var statement in statementList.Statements)
        {
            Visit(statement);
        }
        writer.Indent--;
        writer.WriteLine('}');
    }

    public override void VisitUnaryOperation(UnaryOperation unaryOperation)
    {
        writer.Write(unaryOperation.UnaryOperationKind switch
        {
            UnaryOperationKind.LogicalNegation => "not ",
            UnaryOperationKind.BitwiseNegation => "~",
            UnaryOperationKind.NumericalNegation => "-",
            UnaryOperationKind.LengthOf => "#",
            _ => throw ExceptionUtil.Unreachable,
        });
        Visit(unaryOperation.Operand);
    }

    public override void VisitVariable(Variable variable)
    {
        writer.Write(variable.VariableInfo.Name);
        writer.Write($" (0x{variable.VariableInfo.GetHashCode():X})");
    }
}