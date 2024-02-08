using System.Collections.Frozen;
using System.Diagnostics;

namespace SimpleCompiler.MIR.Ssa;

public sealed partial class SsaComputer
{
    private static State CalculateState(MirTree tree)
    {
        var walker = new StateCalculatorWalker(tree);
        walker.Visit(tree.Root);
        return new State(
            walker._blocksByNode.ToFrozenDictionary(),
            walker._blocksByScope.ToFrozenDictionary(),
            walker._variablesByNode.ToFrozenDictionary(),
            walker._variablesByInfo.ToFrozenDictionary(),
            walker._versionsByNode.ToFrozenDictionary()
        );
    }
    private sealed record State(
        FrozenDictionary<MirNode, SsaBlock> BlocksByNode,
        FrozenDictionary<ScopeInfo, SsaBlock> BlocksByScope,
        FrozenDictionary<MirNode, SsaVariable> VariablesByNode,
        FrozenDictionary<VariableInfo, SsaVariable> VariablesByInfo,
        FrozenDictionary<MirNode, SsaValueVersion> VersionsByNode);
    private sealed class StateCalculatorWalker : MirWalker
    {
        internal readonly Dictionary<MirNode, SsaBlock> _blocksByNode = [];
        internal readonly Dictionary<ScopeInfo, SsaBlock> _blocksByScope = [];
        internal readonly Dictionary<VariableInfo, SsaVariable> _variablesByInfo = [];
        internal readonly Dictionary<MirNode, SsaVariable> _variablesByNode = [];
        internal readonly Dictionary<MirNode, SsaValueVersion> _versionsByNode = [];
        internal readonly Stack<SsaBlock> _blocks = [];

        public StateCalculatorWalker(MirTree tree)
        {
            var globalScope = new SsaBlock(null, null);
            _blocks.Push(globalScope);
            foreach (var variable in tree.GlobalScope.DeclaredVariables)
                globalScope.CreateVariable(variable);
        }

        public override void VisitStatementList(StatementList node)
        {
            var parent = _blocks.TryPeek(out var p) ? p : null;
            var block = parent?.CreateChild(node) ?? new SsaBlock(node, null);
            _blocksByNode.Add(node, block);
            if (node.ScopeInfo is not null)
                _blocksByScope.Add(node.ScopeInfo, block);
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
