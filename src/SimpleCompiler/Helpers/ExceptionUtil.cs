using System.Diagnostics;

namespace SimpleCompiler.Helpers;

public class ExceptionUtil
{
    public static UnreachableException Unreachable => new();
}
