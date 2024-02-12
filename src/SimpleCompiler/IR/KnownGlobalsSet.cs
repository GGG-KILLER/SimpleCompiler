namespace SimpleCompiler.IR;

public sealed record KnownGlobalsSet(
    VariableInfo Assert,
    VariableInfo Type,
    VariableInfo Print,
    VariableInfo Error,
    VariableInfo Tostring
)
{
    public static KnownGlobalsSet CreateGlobals(ScopeInfo scope)
    {
        if (scope.Kind != ScopeKind.Global)
            throw new ArgumentException("Scope is not of global kind.", nameof(scope));

        var assert = new VariableInfo(scope, VariableKind.Global, "assert");
        scope.AddDeclaredVariable(assert);
        var type = new VariableInfo(scope, VariableKind.Global, "type");
        scope.AddDeclaredVariable(type);
        var print = new VariableInfo(scope, VariableKind.Global, "print");
        scope.AddDeclaredVariable(print);
        var error = new VariableInfo(scope, VariableKind.Global, "error");
        scope.AddDeclaredVariable(error);
        var tostring = new VariableInfo(scope, VariableKind.Global, "tostring");
        scope.AddDeclaredVariable(tostring);

        return new KnownGlobalsSet(assert, type, print, error, tostring);
    }
}
