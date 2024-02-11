using SimpleCompiler.IR;

namespace SimpleCompiler.Frontends;

public interface IFrontend
{
    IrTree GetTree();
}
