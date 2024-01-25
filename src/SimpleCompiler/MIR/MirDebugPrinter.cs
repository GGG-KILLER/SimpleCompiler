using System.CodeDom.Compiler;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.MIR;

public sealed class MirDebugPrinter(IndentedTextWriter writer) : MirWalker
{
    public void WriteScope(ScopeInfo? scopeInfo)
    {
        writer.Write($"0x{scopeInfo?.GetHashCode() ?? 0:X} (Variables: [{string.Join(", ", scopeInfo?.DeclaredVariables.Select(s => $"0x{s.GetHashCode():X}") ?? [])}], Scopes: [{string.Join(", ", scopeInfo?.ChildScopes.Select(s => $"0x{s.GetHashCode():X}") ?? [])}])");
    }

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
        var first = true;
        foreach (var assignee in assignment.Assignees)
        {
            if (!first) writer.Write(", ");
            first = false;
            Visit(assignee);
        }
        writer.Write(" = ");
        first = true;
        foreach (var value in assignment.Values)
        {
            if (!first) writer.Write(", ");
            first = false;
            Visit(value);
        }
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
        WriteScope(statementList.ScopeInfo);
        writer.WriteLine(" {");
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

    public override void VisitDiscard(Discard discard)
    {
        writer.Write('_');
    }
}