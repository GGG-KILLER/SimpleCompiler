using System.Collections.Frozen;
using System.Diagnostics;

namespace SimpleCompiler.IR.Ssa;

public sealed partial class SsaComputer
{
    private static State CalculateState(IrTree tree)
    {
        var walker = new StateCalculatorWalker(tree);
        walker.Visit(tree.Root);
        return new State(
            walker._scopesByNode.ToFrozenDictionary(),
            walker._scopesByScope.ToFrozenDictionary(),
            walker._variablesByNode.ToFrozenDictionary(),
            walker._variablesByInfo.ToFrozenDictionary(),
            walker._versionsByNode.ToFrozenDictionary()
        );
    }
    private sealed record State(
        FrozenDictionary<IrNode, SsaScope> ScopesByNode,
        FrozenDictionary<ScopeInfo, SsaScope> ScopesByScope,
        FrozenDictionary<IrNode, SsaVariable> VariablesByNode,
        FrozenDictionary<VariableInfo, SsaVariable> VariablesByInfo,
        FrozenDictionary<IrNode, SsaValueVersion> VersionsByNode);
    private sealed class StateCalculatorWalker : IrWalker
    {
        internal readonly Dictionary<IrNode, SsaScope> _scopesByNode = [];
        internal readonly Dictionary<ScopeInfo, SsaScope> _scopesByScope = [];
        internal readonly Dictionary<VariableInfo, SsaVariable> _variablesByInfo = [];
        internal readonly Dictionary<IrNode, SsaVariable> _variablesByNode = [];
        internal readonly Dictionary<IrNode, SsaValueVersion> _versionsByNode = [];
        internal readonly Stack<SsaScope> _blocks = [];

        public StateCalculatorWalker(IrTree tree)
        {
            var globalScope = new SsaScope(null, null);
            _blocks.Push(globalScope);
            foreach (var variable in tree.GlobalScope.DeclaredVariables)
                globalScope.CreateVariable(variable);
        }

        public override void VisitStatementList(StatementList node)
        {
            var parent = _blocks.TryPeek(out var p) ? p : null;
            var block = parent?.CreateChild(node) ?? new SsaScope(node, null);
            _scopesByNode.Add(node, block);
            if (node.ScopeInfo is not null)
                _scopesByScope.Add(node.ScopeInfo, block);
            _blocks.Push(block);

            base.VisitStatementList(node);

            if (!ReferenceEquals(block, _blocks.Pop()))
                throw new UnreachableException("Popped block was not the expected one.");
        }

        public override void VisitAssignmentStatement(AssignmentStatement node)
        {
            var block = _blocks.Peek();

            // Visit values first
            for (var idx = 0; idx < node.Values.Count; idx++)
                Visit(node.Values[idx]);

            for (var idx = 0; idx < node.Assignees.Count; idx++)
            {
                var assignee = node.Assignees[idx];
                var value = node.Values[idx];

                if (assignee is VariableExpression { VariableInfo: var variable })
                {
                    var ssaVar = block.FindVariable(variable) ?? block.CreateVariable(variable);
                    Debug.Assert(_variablesByInfo.TryAdd(variable, ssaVar) || _variablesByInfo[variable] == ssaVar);
                    _variablesByNode.Add(assignee, ssaVar);

                    ssaVar.AddReferencedBlock(block);
                    ssaVar.AddValueVersion(node, value);

                    _versionsByNode.Add(assignee, ssaVar.ValueVersions[^1]);
                }
                else
                {
                    Visit(assignee);
                }
            }
        }

        public override void VisitVariableExpression(VariableExpression node)
        {
            if (_variablesByInfo.TryGetValue(node.VariableInfo, out var ssaVar))
                _versionsByNode.Add(node, ssaVar.ValueVersions[^1]);
        }
    }
}
