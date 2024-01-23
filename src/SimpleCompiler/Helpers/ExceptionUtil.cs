using System.Diagnostics;

namespace SimpleCompiler;

public class ExceptionUtil
{
    public static UnreachableException Unreachable => new();
}
