using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.Backend.Cil.Emit;

internal static class Constants
{
    // DynamicallyAccessedMemberTypes.All keeps more data than what a member can use:
    // - Keeps info about interfaces
    // - Complete Nested types (nested type body and all its members including other nested types)
    // - Public and private base type information
    // Instead, the GetAllMembers constant will keep:
    // - The nested types body but not the members
    // - Base type public information but not private information. This information should not
    // be visible via the derived type and is ignored by reflection
    internal const DynamicallyAccessedMemberTypes GetAllMembers = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;

    internal static string GetRankString(int rank)
    {
        if (rank <= 0)
            throw new IndexOutOfRangeException();

        return rank == 1 ?
            "[*]" :
            "[" + new string(',', rank - 1) + "]";
    }
}
