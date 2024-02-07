namespace SimpleCompiler.MIR;

public sealed record KnownGlobalsSet(
    VariableInfo Print,
    VariableInfo Tostring
)
{
    public static KnownGlobalsSet CreateGlobals(ScopeInfo scope)
    {
        if (scope.Kind != ScopeKind.Global)
            throw new ArgumentException("Scope is not of global kind.", nameof(scope));

        var print = new VariableInfo(scope, VariableKind.Global, "print");
        scope.AddDeclaredVariable(print);
        var tostring = new VariableInfo(scope, VariableKind.Global, "tostring");
        scope.AddDeclaredVariable(tostring);

        return new KnownGlobalsSet(print, tostring);
    }
}
