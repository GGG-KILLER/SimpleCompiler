using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.IR.Ssa;

public sealed class SsaScope
{
    private readonly List<SsaVariable> _declaredVars = [], _referencedVars = [];
    private readonly List<SsaScope> _childBlocks = [];

    internal SsaScope(IrNode? declaration, SsaScope? parent)
    {
        Declaration = declaration;
        Parent = parent;
    }

    [MemberNotNullWhen(true, nameof(Parent), nameof(Declaration))]
    public bool IsGlobal => Declaration != null;

    [NotNullIfNotNull(nameof(Parent))]
    public IrNode? Declaration { get; }

    [NotNullIfNotNull(nameof(Declaration))]
    public SsaScope? Parent { get; }
    public IReadOnlyList<SsaScope> Children => _childBlocks;

    public IReadOnlyList<SsaVariable> DeclaredVariables => _declaredVars;
    public IReadOnlyList<SsaVariable> ReferencedVariables => _referencedVars;

    public SsaVariable? FindVariable(VariableInfo variable)
    {
        for (var block = this; block is not null; block = block.Parent)
        {
            if (block.DeclaredVariables.FirstOrDefault(x => x.Variable == variable) is { } ssaVar)
            {
                _referencedVars.Add(ssaVar);
                return ssaVar;
            }
        }

        return null;
    }

    internal SsaScope CreateChild(IrNode declaration)
    {
        SsaScope ssaBlock = new(declaration, this);
        _childBlocks.Add(ssaBlock);
        return ssaBlock;
    }

    internal SsaVariable CreateVariable(VariableInfo variable)
    {
        SsaVariable ssaVariable = new(variable, this);
        _declaredVars.Add(ssaVariable);
        return ssaVariable;
    }
}
