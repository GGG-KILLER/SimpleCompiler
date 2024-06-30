using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;
using SimpleCompiler.IR.Debug;

namespace SimpleCompiler.Frontends.Lua;

public sealed class LuaFrontend : IFrontend<SyntaxTree>
{
    public static IrGraph LowerWithoutSsa(SyntaxTree input)
    {
        var script = new Script([input]);
        var walker = new Walker(script);
        walker.LowerRoot(
            input.GetCompilationUnitRoot(),
            out var basicBlocks,
            out var edges,
            out var entryBlocks,
            out var debugData);

        return new IrGraph(basicBlocks, edges, entryBlocks, debugData);
    }

    public IrGraph Lower(SyntaxTree input)
    {
        var graph = LowerWithoutSsa(input);
        SsaRewriter.RewriteGraph(graph);
        return graph;
    }

    private sealed class Walker(Script script)
    {
        // Global state
        private int _scopeCounter = 1;
        private readonly ConditionalWeakTable<IScope, object> _scopeNumber = [];
        private readonly NameTracker _nameTracker = new();
        private readonly Dictionary<IVariable, NameValue> _variableNames = [];
        private readonly List<BasicBlock> _basicBlocks = [];
        private readonly List<IrEdge> _edges = [];
        private readonly List<List<Action<BasicBlock>>> _targetQueue = [];
        private readonly DebugData _debugData = new();

        // Current block state
        private readonly ImmutableArray<Instruction>.Builder _instructions = ImmutableArray.CreateBuilder<Instruction>();

        /// <summary>
        /// Returns an unique name for this variable across the entire program.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        private NameValue GetName(IVariable variable)
        {
            if (!_variableNames.TryGetValue(variable, out var name))
            {
                var num = _scopeNumber.GetValue(variable.ContainingScope, _ => Interlocked.Increment(ref _scopeCounter));
                _variableNames[variable] = name = NameValue.Unversioned($"L_{CastHelper.FastUnbox<int>(num):X}_{variable.Name}");
                _debugData.OriginalValueNames[name] = variable.Name;
            }
            return name;
        }

        private IVariable FindVariable(SyntaxNode node) =>
            script.GetVariable(node)
            ?? throw new InvalidOperationException($"No variable for {node.ToFullString()} at {node.GetLocation()}");

        private void QueueBlockCallback(int offset, Action<BasicBlock> callback)
        {
            // Add missing queues if necessary.
            for (var idx = _targetQueue.Count - 1; idx <= offset; idx++)
                _targetQueue.Add([]);
            _targetQueue[offset].Add(callback);
        }

        private int CurrentBlockOrdinal => _basicBlocks.Count;
        private BasicBlock FinalizeBlock()
        {
            var basicBlock = new BasicBlock(CurrentBlockOrdinal, _instructions.ToImmutable());

            if (_targetQueue.Count > 0)
            {
                _targetQueue[0].ForEach(x => x(basicBlock));
                _targetQueue.RemoveAt(0);
            }

            _basicBlocks.Add(basicBlock);
            _instructions.Clear();

            return basicBlock;
        }

        private void EmitDebugLocation(SyntaxNode node)
        {
            var lineSpan = node.GetLocation().GetLineSpan();
            _instructions.Add(new DebugLocation(new SourceLocation(
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                lineSpan.EndLinePosition.Line + 1,
                lineSpan.EndLinePosition.Character + 1
            )));
        }

        public void LowerRoot(
            CompilationUnitSyntax node,
            out List<BasicBlock> basicBlocks,
            out List<IrEdge> edges,
            out BasicBlock entryBlock,
            out DebugData debugData)
        {
            var syntaxTree = node.SyntaxTree;
            var sourceText = syntaxTree.GetText();
            _debugData.SourceFile = new SourceFile(
                syntaxTree.FilePath,
                sourceText.ToString(),
                sourceText.Encoding ?? Encoding.UTF8);

            // Setup globals as locals in the entry point.
            foreach (var variable in script.RootScope.DeclaredVariables)
            {
                var name = GetName(variable);
                _instructions.Add(new Assignment(name, Constant.Nil));
            }

            // Setup builtins as locals in the entry point.
            foreach (var builtin in Enum.GetValues<KnownBuiltins>())
            {
                if (builtin == KnownBuiltins.INVALID) continue;

                var variable = script.RootScope.FindVariable(builtin.ToString());
                if (variable is null)
                    continue;

                var name = GetName(variable!);
                _instructions.Add(new Assignment(name, new Builtin(builtin)));
            }

            LowerStatementList(node.Statements);

            basicBlocks = _basicBlocks;
            edges = _edges;
            entryBlock = basicBlocks[0];
            debugData = _debugData;
        }

        private void LowerStatementList(StatementListSyntax node, bool isBlock = true)
        {
            foreach (var statement in node.Statements)
            {
                switch (statement.Kind())
                {
                    case SyntaxKind.DoStatement:
                        LowerStatementList(CastHelper.FastCast<DoStatementSyntax>(statement).Body, false);
                        break;
                    case SyntaxKind.IfStatement:
                        LowerIfStatement(CastHelper.FastCast<IfStatementSyntax>(statement));
                        break;
                    case SyntaxKind.GotoStatement:
                    case SyntaxKind.GotoLabelStatement:
                        throw new NotImplementedException("Goto hasn't been implemented yet.");
                    case SyntaxKind.EmptyStatement:
                        break;
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.GenericForStatement:
                    case SyntaxKind.NumericForStatement:
                    case SyntaxKind.RepeatUntilStatement:
                    case SyntaxKind.ContinueStatement:
                    case SyntaxKind.BreakStatement:
                        throw new NotImplementedException("Loops haven't been implemented yet.");
                    case SyntaxKind.ReturnStatement:
                        throw new NotImplementedException("Return statement hasn't been implemented yet.");
                    case SyntaxKind.AssignmentStatement:
                        LowerAssignmentStatement(CastHelper.FastCast<AssignmentStatementSyntax>(statement));
                        break;
                    case SyntaxKind.ExpressionStatement:
                        LowerExpressionStatement(CastHelper.FastCast<ExpressionStatementSyntax>(statement));
                        break;
                    case SyntaxKind.AddAssignmentStatement:
                    case SyntaxKind.ConcatAssignmentStatement:
                    case SyntaxKind.DivideAssignmentStatement:
                    case SyntaxKind.ModuloAssignmentStatement:
                    case SyntaxKind.MultiplyAssignmentStatement:
                    case SyntaxKind.SubtractAssignmentStatement:
                    case SyntaxKind.ExponentiateAssignmentStatement:
                        LowerCompoundAssignmentStatement(CastHelper.FastCast<CompoundAssignmentStatementSyntax>(statement));
                        break;
                    case SyntaxKind.FunctionDeclarationStatement:
                    case SyntaxKind.LocalFunctionDeclarationStatement:
                        throw new NotImplementedException("Functions haven't been implemented yet.");
                    case SyntaxKind.LocalVariableDeclarationStatement:
                        LowerLocalVariableDeclaration(CastHelper.FastCast<LocalVariableDeclarationStatementSyntax>(statement));
                        break;
                    default:
                        throw new ArgumentException($"Statement list has unknown statement kind {node.Kind()}", nameof(node));
                }
            }

            if (isBlock)
                FinalizeBlock();
        }

        private void LowerIfStatement(IfStatementSyntax ifStatementSyntax)
        {
            var bodyBlockOrdinals = new List<int>();
            var endTarget = new BranchTarget();

            // Queue branch targets
            var ifTrue = new BranchTarget(CurrentBlockOrdinal + 1);
            _edges.Add(new IrEdge(CurrentBlockOrdinal, CurrentBlockOrdinal + 1));
            var ifFalse = new BranchTarget(CurrentBlockOrdinal + 2);
            _edges.Add(new IrEdge(CurrentBlockOrdinal, CurrentBlockOrdinal + 2));

            // Add entry condition and end the block with the branch
            var ifCond = LowerExpression(ifStatementSyntax.Condition);
            EmitDebugLocation(ifStatementSyntax.Condition);
            _instructions.Add(new ConditionalBranch(ifCond, ifTrue, ifFalse));
            FinalizeBlock();

            LowerStatementList(ifStatementSyntax.Body, false);
            _instructions.Add(new Branch(endTarget));
            var ifBodyBlock = FinalizeBlock();
            bodyBlockOrdinals.Add(ifBodyBlock.Ordinal);

            foreach (var clause in ifStatementSyntax.ElseIfClauses)
            {
                ifTrue = new BranchTarget(CurrentBlockOrdinal + 1);
                _edges.Add(new IrEdge(CurrentBlockOrdinal, CurrentBlockOrdinal + 1));
                ifFalse = new BranchTarget(CurrentBlockOrdinal + 2);
                _edges.Add(new IrEdge(CurrentBlockOrdinal, CurrentBlockOrdinal + 2));

                // Create entry condition block
                var elseIfCond = LowerExpression(clause.Condition);
                EmitDebugLocation(clause.Condition);
                _instructions.Add(new ConditionalBranch(elseIfCond, ifTrue, ifFalse));
                FinalizeBlock();

                // Finalize the block for the body
                LowerStatementList(clause.Body, false);
                _instructions.Add(new Branch(endTarget));
                var elseIfBodyBlock = FinalizeBlock();
                bodyBlockOrdinals.Add(elseIfBodyBlock.Ordinal);
            }

            if (ifStatementSyntax.ElseClause is not null)
            {
                // Else has no condition so no entry block to it
                LowerStatementList(ifStatementSyntax.ElseClause.ElseBody, false);
                _instructions.Add(new Branch(endTarget));
                var elseBodyBlock = FinalizeBlock();
                bodyBlockOrdinals.Add(elseBodyBlock.Ordinal);
            }

            // Mark the next block as the end target
            QueueBlockCallback(0, block =>
            {
                foreach (var ordinal in bodyBlockOrdinals)
                    _edges.Add(new IrEdge(ordinal, block.Ordinal));
                endTarget.SetBlock(block.Ordinal);
            });
        }

        private void LowerLocalVariableDeclaration(LocalVariableDeclarationStatementSyntax node)
        {
            var values = new List<NameValue>();
            for (var idx = 0; idx < node.Names.Count; idx++)
            {
                if (idx < node.EqualsValues?.Values.Count)
                    values.Add(LowerExpression(node.EqualsValues.Values[idx]));
                else
                    break;
            }

            EmitDebugLocation(node);
            for (var idx = 0; idx < node.Names.Count; idx++)
            {
                var name = node.Names[index: idx];
                var variable = FindVariable(name);

                _instructions.Add(new Assignment(
                    GetName(variable),
                    idx < values.Count ? values[idx] : Constant.Nil));
            }
        }

        private void LowerAssignmentStatement(AssignmentStatementSyntax node)
        {
            var values = new List<NameValue>();
            for (var idx = 0; idx < node.Variables.Count; idx++)
            {
                if (idx < node.EqualsValues?.Values.Count)
                    values.Add(LowerExpression(node.EqualsValues.Values[idx]));
                else
                    break;
            }

            EmitDebugLocation(node);
            for (var idx = 0; idx < node.Variables.Count; idx++)
            {
                var name = node.Variables[index: idx];
                if (name is not IdentifierNameSyntax)
                    throw new NotImplementedException($"{name.GetLocation()}: Assignment to non-identifiers is not supported.");

                var variable = FindVariable(name);

                _instructions.Add(new Assignment(
                    GetName(variable),
                    idx < values.Count ? values[idx] : Constant.Nil));
            }
        }

        private void LowerCompoundAssignmentStatement(CompoundAssignmentStatementSyntax node)
        {
            if (node.Variable is not IdentifierNameSyntax)
                throw new NotImplementedException($"{node.Variable.GetLocation()}: Assignment to non-identifiers is not supported.");

            var assignee = GetName(FindVariable(node.Variable));
            var operand = LowerExpression(node.Expression);

            _instructions.Add(new BinaryAssignment(
                assignee,
                assignee,
                ToBinaryKind(
                    node,
                    SyntaxFacts.GetCompoundAssignmentOperator(node.AssignmentOperatorToken.Kind())
                               .AndThen(SyntaxFacts.GetBinaryExpression)
                               .Value
                ),
                operand));
        }

        private void LowerExpressionStatement(ExpressionStatementSyntax node) => LowerExpression(node.Expression);

        private NameValue LowerExpression(ExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IfExpression:
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                    throw new NotImplementedException("Logical expressions haven't been implemented yet.");
                case SyntaxKind.VarArgExpression:
                    throw new NotImplementedException("Varargs haven't been implemented yet.");
                case SyntaxKind.TypeCastExpression:
                    return LowerExpression(CastHelper.FastCast<TypeCastExpressionSyntax>(node).Expression);
                case SyntaxKind.ParenthesizedExpression:
                    return LowerExpression(CastHelper.FastCast<ParenthesizedExpressionSyntax>(node).Expression);
                case SyntaxKind.MethodCallExpression:
                case SyntaxKind.MemberAccessExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.TableConstructorExpression:
                    throw new NotImplementedException("Tables haven't been implemented yet.");
                case SyntaxKind.NilLiteralExpression:
                    EmitDebugLocation(node);
                    return CreateNilLiteral();
                case SyntaxKind.TrueLiteralExpression:
                {
                    EmitDebugLocation(node);
                    var name = _nameTracker.NewTemporary();
                    var operand = Constant.True;
                    _instructions.Add(new Assignment(name, operand));
                    return name;
                }
                case SyntaxKind.FalseLiteralExpression:
                {
                    EmitDebugLocation(node);
                    var name = _nameTracker.NewTemporary();
                    var operand = Constant.False;
                    _instructions.Add(new Assignment(name, operand));
                    return name;
                }
                case SyntaxKind.StringLiteralExpression:
                {
                    EmitDebugLocation(node);
                    var name = _nameTracker.NewTemporary();
                    var operand = new Constant(ConstantKind.String, CastHelper.FastCast<LiteralExpressionSyntax>(node).Token.Value);
                    _instructions.Add(new Assignment(name, operand));
                    return name;
                }
                case SyntaxKind.NumericalLiteralExpression:
                case SyntaxKind.HashStringLiteralExpression:
                {
                    EmitDebugLocation(node);
                    var name = _nameTracker.NewTemporary();
                    var operand = new Constant(ConstantKind.Number, CastHelper.FastCast<LiteralExpressionSyntax>(node).Token.Value);
                    _instructions.Add(new Assignment(name, operand));
                    return name;
                }
                case SyntaxKind.IdentifierName:
                    return GetName(FindVariable(node));
                case SyntaxKind.FunctionCallExpression:
                    return LowerFunctionCall(CastHelper.FastCast<FunctionCallExpressionSyntax>(node));
                case SyntaxKind.AnonymousFunctionExpression:
                    throw new NotImplementedException("Functions haven't been implemented yet.");
                case SyntaxKind kind when SyntaxFacts.IsBinaryExpression(kind):
                    return LowerBinaryExpression(CastHelper.FastCast<BinaryExpressionSyntax>(node));
                case SyntaxKind kind when SyntaxFacts.IsUnaryExpression(kind):
                    return LowerUnaryExpression(CastHelper.FastCast<UnaryExpressionSyntax>(node));
                default:
                    throw new ArgumentException($"Unknown expression kind {node.Kind()}.", nameof(node));
            }
        }

        private NameValue CreateNilLiteral()
        {
            var name = _nameTracker.NewTemporary();
            var operand = Constant.Nil;
            _instructions.Add(new Assignment(name, operand));
            return name;
        }

        private NameValue LowerFunctionCall(FunctionCallExpressionSyntax node)
        {
            var arguments = node.Argument.Kind() switch
            {
                SyntaxKind.StringFunctionArgument => [LowerExpression(CastHelper.FastCast<StringFunctionArgumentSyntax>(node.Argument).Expression)],
                SyntaxKind.TableConstructorFunctionArgument => [LowerExpression(CastHelper.FastCast<TableConstructorFunctionArgumentSyntax>(node.Argument).TableConstructor)],
                SyntaxKind.ExpressionListFunctionArgument => CastHelper.FastCast<ExpressionListFunctionArgumentSyntax>(node.Argument)
                                                                       .Expressions
                                                                       .Select(LowerExpression)
                                                                       .Cast<Operand>()
                                                                       .ToList(),
                _ => throw new UnreachableException()
            };

            var callee = LowerExpression(node.Expression);

            EmitDebugLocation(node);
            var name = _nameTracker.NewTemporary();
            _instructions.Add(new FunctionAssignment(name, callee, arguments));

            return name;
        }

        private NameValue LowerBinaryExpression(BinaryExpressionSyntax node)
        {
            var left = LowerExpression(node.Left);
            var right = LowerExpression(node.Right);

            EmitDebugLocation(node);
            var name = _nameTracker.NewTemporary();
            _instructions.Add(new BinaryAssignment(
                name,
                left,
                ToBinaryKind(node, node.Kind()),
                right));
            return name;
        }

        private NameValue LowerUnaryExpression(UnaryExpressionSyntax node)
        {
            var operand = LowerExpression(node.Operand);

            EmitDebugLocation(node);
            var name = _nameTracker.NewTemporary();
            _instructions.Add(new UnaryAssignment(
                name,
                node.Kind() switch
                {
                    SyntaxKind.LogicalNotExpression => UnaryOperationKind.BitwiseNegation,
                    SyntaxKind.BitwiseNotExpression => UnaryOperationKind.BitwiseNegation,
                    SyntaxKind.UnaryMinusExpression => UnaryOperationKind.NumericalNegation,
                    SyntaxKind.LengthExpression => UnaryOperationKind.LengthOf,
                    _ => throw new ArgumentException($"Unknown unary expression kind {node.Kind()} at {node.GetLocation()}")
                },
                operand));
            return name;
        }

        private static BinaryOperationKind ToBinaryKind(SyntaxNode node, SyntaxKind kind) =>
            kind switch
            {
                SyntaxKind.AddExpression => BinaryOperationKind.Addition,
                SyntaxKind.SubtractExpression => BinaryOperationKind.Subtraction,
                SyntaxKind.MultiplyExpression => BinaryOperationKind.Multiplication,
                SyntaxKind.DivideExpression => BinaryOperationKind.Division,
                SyntaxKind.FloorDivideExpression => BinaryOperationKind.IntegerDivision,
                SyntaxKind.ExponentiateExpression => BinaryOperationKind.Exponentiation,
                SyntaxKind.ModuloExpression => BinaryOperationKind.Modulo,
                SyntaxKind.ConcatExpression => BinaryOperationKind.Concatenation,

                SyntaxKind.BitwiseAndExpression => BinaryOperationKind.BitwiseAnd,
                SyntaxKind.BitwiseOrExpression => BinaryOperationKind.BitwiseOr,
                SyntaxKind.ExclusiveOrExpression => BinaryOperationKind.BitwiseXor,
                SyntaxKind.LeftShiftExpression => BinaryOperationKind.LeftShift,
                SyntaxKind.RightShiftExpression => BinaryOperationKind.RightShift,

                SyntaxKind.EqualsExpression => BinaryOperationKind.Equals,
                SyntaxKind.NotEqualsExpression => BinaryOperationKind.NotEquals,
                SyntaxKind.LessThanExpression => BinaryOperationKind.LessThan,
                SyntaxKind.LessThanOrEqualExpression => BinaryOperationKind.LessThanOrEquals,
                SyntaxKind.GreaterThanExpression => BinaryOperationKind.GreaterThan,
                SyntaxKind.GreaterThanOrEqualExpression => BinaryOperationKind.GreaterThanOrEquals,

                _ => throw new ArgumentException($"Unknown binary expression kind {node.Kind()} at {node.GetLocation()}")
            };
    }
}
