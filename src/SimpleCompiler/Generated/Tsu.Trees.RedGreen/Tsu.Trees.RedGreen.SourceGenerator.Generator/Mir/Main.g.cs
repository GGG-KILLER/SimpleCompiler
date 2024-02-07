﻿// <auto-generated />

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SimpleCompiler.MIR.Internal;

namespace SimpleCompiler.MIR
{

    public partial class MirVisitor
    {
        public virtual void Visit(global::SimpleCompiler.MIR.MirNode? node)
        {
            if (node != null)
            {
                node.Accept(this);
            }
        }
        public virtual void VisitMirNone(global::SimpleCompiler.MIR.MirNone node) => this.DefaultVisit(node);
        public virtual void VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node) => this.DefaultVisit(node);
        public virtual void VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node) => this.DefaultVisit(node);
        public virtual void VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node) => this.DefaultVisit(node);
        public virtual void VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node) => this.DefaultVisit(node);
        public virtual void VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node) => this.DefaultVisit(node);
        public virtual void VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node) => this.DefaultVisit(node);
        public virtual void VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node) => this.DefaultVisit(node);
        public virtual void VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node) => this.DefaultVisit(node);
        public virtual void VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node) => this.DefaultVisit(node);
        public virtual void VisitStatementList(global::SimpleCompiler.MIR.StatementList node) => this.DefaultVisit(node);
        protected virtual void DefaultVisit(global::SimpleCompiler.MIR.MirNode node) { }
    }

    public partial class MirVisitor<TResult>
    {
        public virtual TResult? Visit(global::SimpleCompiler.MIR.MirNode? node) => node == null ? default : node.Accept(this
        );
        public virtual TResult? VisitMirNone(global::SimpleCompiler.MIR.MirNone node) => this.DefaultVisit(node);
        public virtual TResult? VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node) => this.DefaultVisit(node);
        public virtual TResult? VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node) => this.DefaultVisit(node);
        public virtual TResult? VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node) => this.DefaultVisit(node);
        public virtual TResult? VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node) => this.DefaultVisit(node);
        public virtual TResult? VisitStatementList(global::SimpleCompiler.MIR.StatementList node) => this.DefaultVisit(node);
        protected virtual TResult? DefaultVisit(global::SimpleCompiler.MIR.MirNode node) => default;
    }

    public partial class MirVisitor<T1, TResult>
    {
        public virtual TResult? Visit(global::SimpleCompiler.MIR.MirNode? node, T1 arg1) => node == null ? default : node.Accept(this
        , arg1);
        public virtual TResult? VisitMirNone(global::SimpleCompiler.MIR.MirNone node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node, T1 arg1) => this.DefaultVisit(node, arg1);
        public virtual TResult? VisitStatementList(global::SimpleCompiler.MIR.StatementList node, T1 arg1) => this.DefaultVisit(node, arg1);
        protected virtual TResult? DefaultVisit(global::SimpleCompiler.MIR.MirNode node, T1 arg1) => default;
    }

    public partial class MirVisitor<T1, T2, TResult>
    {
        public virtual TResult? Visit(global::SimpleCompiler.MIR.MirNode? node, T1 arg1, T2 arg2) => node == null ? default : node.Accept(this
        , arg1, arg2);
        public virtual TResult? VisitMirNone(global::SimpleCompiler.MIR.MirNone node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        public virtual TResult? VisitStatementList(global::SimpleCompiler.MIR.StatementList node, T1 arg1, T2 arg2) => this.DefaultVisit(node, arg1, arg2);
        protected virtual TResult? DefaultVisit(global::SimpleCompiler.MIR.MirNode node, T1 arg1, T2 arg2) => default;
    }

    public partial class MirVisitor<T1, T2, T3, TResult>
    {
        public virtual TResult? Visit(global::SimpleCompiler.MIR.MirNode? node, T1 arg1, T2 arg2, T3 arg3) => node == null ? default : node.Accept(this
        , arg1, arg2, arg3);
        public virtual TResult? VisitMirNone(global::SimpleCompiler.MIR.MirNone node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        public virtual TResult? VisitStatementList(global::SimpleCompiler.MIR.StatementList node, T1 arg1, T2 arg2, T3 arg3) => this.DefaultVisit(node, arg1, arg2, arg3);
        protected virtual TResult? DefaultVisit(global::SimpleCompiler.MIR.MirNode node, T1 arg1, T2 arg2, T3 arg3) => default;
    }



    public partial class MirRewriter : global::SimpleCompiler.MIR.MirVisitor<global::SimpleCompiler.MIR.MirNode>
    {
        public global::SimpleCompiler.MIR.MirList<TNode> VisitList<TNode>(global::SimpleCompiler.MIR.MirList<TNode> list) where TNode : global::SimpleCompiler.MIR.MirNode
        {
            global::SimpleCompiler.MIR.MirListBuilder? alternate = null;
            for (int i = 0, n = list.Count; i < n; i++)
            {
                var item = list[i];
                var visited = Visit(item);
                if (item != visited && alternate == null)
                {
                    alternate = new global::SimpleCompiler.MIR.MirListBuilder(n);
                    alternate.AddRange(list, 0, i);
                }

                if (alternate != null && visited != null && visited.Kind != global::SimpleCompiler.MIR.MirKind.None)
                {
                    alternate.Add(visited);
                }
            }

            if (alternate != null)
            {
                return alternate.ToList();
            }

            return list;
        }

        public override global::SimpleCompiler.MIR.MirNode VisitMirNone(global::SimpleCompiler.MIR.MirNone node) =>
            node.Update(node.OriginalNode);
        public override global::SimpleCompiler.MIR.MirNode VisitBinaryOperationExpression(global::SimpleCompiler.MIR.BinaryOperationExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind, node.BinaryOperationKind, (global::SimpleCompiler.MIR.Expression?)Visit(node.Left) ?? throw new global::System.InvalidOperationException("Left cannot be null."), (global::SimpleCompiler.MIR.Expression?)Visit(node.Right) ?? throw new global::System.InvalidOperationException("Right cannot be null."));
        public override global::SimpleCompiler.MIR.MirNode VisitConstantExpression(global::SimpleCompiler.MIR.ConstantExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind, node.ConstantKind, node.Value);
        public override global::SimpleCompiler.MIR.MirNode VisitDiscardExpression(global::SimpleCompiler.MIR.DiscardExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind);
        public override global::SimpleCompiler.MIR.MirNode VisitFunctionCallExpression(global::SimpleCompiler.MIR.FunctionCallExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind, (global::SimpleCompiler.MIR.Expression?)Visit(node.Callee) ?? throw new global::System.InvalidOperationException("Callee cannot be null."), VisitList(node.Arguments));
        public override global::SimpleCompiler.MIR.MirNode VisitUnaryOperationExpression(global::SimpleCompiler.MIR.UnaryOperationExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind, node.UnaryOperationKind, (global::SimpleCompiler.MIR.Expression?)Visit(node.Operand) ?? throw new global::System.InvalidOperationException("Operand cannot be null."));
        public override global::SimpleCompiler.MIR.MirNode VisitVariableExpression(global::SimpleCompiler.MIR.VariableExpression node) =>
            node.Update(node.OriginalNode, node.ResultKind, node.VariableInfo);
        public override global::SimpleCompiler.MIR.MirNode VisitAssignmentStatement(global::SimpleCompiler.MIR.AssignmentStatement node) =>
            node.Update(node.OriginalNode, VisitList(node.Assignees), VisitList(node.Values));
        public override global::SimpleCompiler.MIR.MirNode VisitEmptyStatement(global::SimpleCompiler.MIR.EmptyStatement node) =>
            node.Update(node.OriginalNode);
        public override global::SimpleCompiler.MIR.MirNode VisitExpressionStatement(global::SimpleCompiler.MIR.ExpressionStatement node) =>
            node.Update(node.OriginalNode, (global::SimpleCompiler.MIR.Expression?)Visit(node.Expression) ?? throw new global::System.InvalidOperationException("Expression cannot be null."));
        public override global::SimpleCompiler.MIR.MirNode VisitStatementList(global::SimpleCompiler.MIR.StatementList node) =>
            node.Update(node.OriginalNode, VisitList(node.Statements), node.ScopeInfo);
    }
    public static partial class MirFactory
    {
        public static global::SimpleCompiler.MIR.MirNone MirNone() =>
            (global::SimpleCompiler.MIR.MirNone) global::SimpleCompiler.MIR.Internal.MirFactory.MirNone().CreateRed();

        public static global::SimpleCompiler.MIR.MirNone MirNone(global::Loretta.CodeAnalysis.SyntaxReference? originalNode) =>
            (global::SimpleCompiler.MIR.MirNone) global::SimpleCompiler.MIR.Internal.MirFactory.MirNone(originalNode).CreateRed();

        public static global::SimpleCompiler.MIR.BinaryOperationExpression BinaryOperationExpression(global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.BinaryOperationKind binaryOperationKind, global::SimpleCompiler.MIR.Expression left, global::SimpleCompiler.MIR.Expression right) =>
            (global::SimpleCompiler.MIR.BinaryOperationExpression) global::SimpleCompiler.MIR.Internal.MirFactory.BinaryOperationExpression(resultKind, binaryOperationKind, (global::SimpleCompiler.MIR.Internal.Expression)left.Green, (global::SimpleCompiler.MIR.Internal.Expression)right.Green).CreateRed();

        public static global::SimpleCompiler.MIR.BinaryOperationExpression BinaryOperationExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.BinaryOperationKind binaryOperationKind, global::SimpleCompiler.MIR.Expression left, global::SimpleCompiler.MIR.Expression right) =>
            (global::SimpleCompiler.MIR.BinaryOperationExpression) global::SimpleCompiler.MIR.Internal.MirFactory.BinaryOperationExpression(originalNode, resultKind, binaryOperationKind, (global::SimpleCompiler.MIR.Internal.Expression)left.Green, (global::SimpleCompiler.MIR.Internal.Expression)right.Green).CreateRed();

        public static global::SimpleCompiler.MIR.ConstantExpression ConstantExpression(global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.ConstantKind constantKind, object value) =>
            (global::SimpleCompiler.MIR.ConstantExpression) global::SimpleCompiler.MIR.Internal.MirFactory.ConstantExpression(resultKind, constantKind, value).CreateRed();

        public static global::SimpleCompiler.MIR.ConstantExpression ConstantExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.ConstantKind constantKind, object value) =>
            (global::SimpleCompiler.MIR.ConstantExpression) global::SimpleCompiler.MIR.Internal.MirFactory.ConstantExpression(originalNode, resultKind, constantKind, value).CreateRed();

        public static global::SimpleCompiler.MIR.DiscardExpression DiscardExpression(global::SimpleCompiler.MIR.ResultKind resultKind) =>
            (global::SimpleCompiler.MIR.DiscardExpression) global::SimpleCompiler.MIR.Internal.MirFactory.DiscardExpression(resultKind).CreateRed();

        public static global::SimpleCompiler.MIR.DiscardExpression DiscardExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind) =>
            (global::SimpleCompiler.MIR.DiscardExpression) global::SimpleCompiler.MIR.Internal.MirFactory.DiscardExpression(originalNode, resultKind).CreateRed();

        public static global::SimpleCompiler.MIR.FunctionCallExpression FunctionCallExpression(global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.Expression callee) =>
            (global::SimpleCompiler.MIR.FunctionCallExpression) global::SimpleCompiler.MIR.Internal.MirFactory.FunctionCallExpression(resultKind, (global::SimpleCompiler.MIR.Internal.Expression)callee.Green).CreateRed();

        public static global::SimpleCompiler.MIR.FunctionCallExpression FunctionCallExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.Expression callee, global::SimpleCompiler.MIR.MirList<global::SimpleCompiler.MIR.Expression> arguments) =>
            (global::SimpleCompiler.MIR.FunctionCallExpression) global::SimpleCompiler.MIR.Internal.MirFactory.FunctionCallExpression(originalNode, resultKind, (global::SimpleCompiler.MIR.Internal.Expression)callee.Green, arguments.Node.ToMirList<global::SimpleCompiler.MIR.Internal.Expression>()).CreateRed();

        public static global::SimpleCompiler.MIR.UnaryOperationExpression UnaryOperationExpression(global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.UnaryOperationKind unaryOperationKind, global::SimpleCompiler.MIR.Expression operand) =>
            (global::SimpleCompiler.MIR.UnaryOperationExpression) global::SimpleCompiler.MIR.Internal.MirFactory.UnaryOperationExpression(resultKind, unaryOperationKind, (global::SimpleCompiler.MIR.Internal.Expression)operand.Green).CreateRed();

        public static global::SimpleCompiler.MIR.UnaryOperationExpression UnaryOperationExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.UnaryOperationKind unaryOperationKind, global::SimpleCompiler.MIR.Expression operand) =>
            (global::SimpleCompiler.MIR.UnaryOperationExpression) global::SimpleCompiler.MIR.Internal.MirFactory.UnaryOperationExpression(originalNode, resultKind, unaryOperationKind, (global::SimpleCompiler.MIR.Internal.Expression)operand.Green).CreateRed();

        public static global::SimpleCompiler.MIR.VariableExpression VariableExpression(global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.VariableInfo variableInfo) =>
            (global::SimpleCompiler.MIR.VariableExpression) global::SimpleCompiler.MIR.Internal.MirFactory.VariableExpression(resultKind, variableInfo).CreateRed();

        public static global::SimpleCompiler.MIR.VariableExpression VariableExpression(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.ResultKind resultKind, global::SimpleCompiler.MIR.VariableInfo variableInfo) =>
            (global::SimpleCompiler.MIR.VariableExpression) global::SimpleCompiler.MIR.Internal.MirFactory.VariableExpression(originalNode, resultKind, variableInfo).CreateRed();

        public static global::SimpleCompiler.MIR.AssignmentStatement AssignmentStatement() =>
            (global::SimpleCompiler.MIR.AssignmentStatement) global::SimpleCompiler.MIR.Internal.MirFactory.AssignmentStatement().CreateRed();

        public static global::SimpleCompiler.MIR.AssignmentStatement AssignmentStatement(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.MirList<global::SimpleCompiler.MIR.Expression> assignees, global::SimpleCompiler.MIR.MirList<global::SimpleCompiler.MIR.Expression> values) =>
            (global::SimpleCompiler.MIR.AssignmentStatement) global::SimpleCompiler.MIR.Internal.MirFactory.AssignmentStatement(originalNode, assignees.Node.ToMirList<global::SimpleCompiler.MIR.Internal.Expression>(), values.Node.ToMirList<global::SimpleCompiler.MIR.Internal.Expression>()).CreateRed();

        public static global::SimpleCompiler.MIR.EmptyStatement EmptyStatement() =>
            (global::SimpleCompiler.MIR.EmptyStatement) global::SimpleCompiler.MIR.Internal.MirFactory.EmptyStatement().CreateRed();

        public static global::SimpleCompiler.MIR.EmptyStatement EmptyStatement(global::Loretta.CodeAnalysis.SyntaxReference? originalNode) =>
            (global::SimpleCompiler.MIR.EmptyStatement) global::SimpleCompiler.MIR.Internal.MirFactory.EmptyStatement(originalNode).CreateRed();

        public static global::SimpleCompiler.MIR.ExpressionStatement ExpressionStatement(global::SimpleCompiler.MIR.Expression expression) =>
            (global::SimpleCompiler.MIR.ExpressionStatement) global::SimpleCompiler.MIR.Internal.MirFactory.ExpressionStatement((global::SimpleCompiler.MIR.Internal.Expression)expression.Green).CreateRed();

        public static global::SimpleCompiler.MIR.ExpressionStatement ExpressionStatement(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.Expression expression) =>
            (global::SimpleCompiler.MIR.ExpressionStatement) global::SimpleCompiler.MIR.Internal.MirFactory.ExpressionStatement(originalNode, (global::SimpleCompiler.MIR.Internal.Expression)expression.Green).CreateRed();

        public static global::SimpleCompiler.MIR.StatementList StatementList() =>
            (global::SimpleCompiler.MIR.StatementList) global::SimpleCompiler.MIR.Internal.MirFactory.StatementList().CreateRed();

        public static global::SimpleCompiler.MIR.StatementList StatementList(global::Loretta.CodeAnalysis.SyntaxReference? originalNode, global::SimpleCompiler.MIR.MirList<global::SimpleCompiler.MIR.Statement> statements, global::SimpleCompiler.MIR.ScopeInfo? scopeInfo) =>
            (global::SimpleCompiler.MIR.StatementList) global::SimpleCompiler.MIR.Internal.MirFactory.StatementList(originalNode, statements.Node.ToMirList<global::SimpleCompiler.MIR.Internal.Statement>(), scopeInfo).CreateRed();
    }
}

