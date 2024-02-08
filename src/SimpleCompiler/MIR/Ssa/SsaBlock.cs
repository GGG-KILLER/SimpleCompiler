using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.MIR.Ssa;

public sealed class SsaBlock
{
    private readonly List<SsaVariable> _declaredVars = [], _referencedVars = [];
    private readonly List<SsaBlock> _childBlocks = [];

    internal SsaBlock(MirNode? declaration, SsaBlock? parent)
    {
        Declaration = declaration;
        Parent = parent;
    }

    [MemberNotNullWhen(true, nameof(Parent), nameof(Declaration))]
    public bool IsGlobal => Declaration != null;

    [NotNullIfNotNull(nameof(Parent))]
    public MirNode? Declaration { get; }

    [NotNullIfNotNull(nameof(Declaration))]
    public SsaBlock? Parent { get; }
    public IReadOnlyList<SsaBlock> Children => _childBlocks;

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

    internal SsaBlock CreateChild(MirNode declaration)
    {
        SsaBlock ssaBlock = new(declaration, this);
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
