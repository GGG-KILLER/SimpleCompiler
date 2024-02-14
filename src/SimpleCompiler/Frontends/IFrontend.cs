using SimpleCompiler.IR;

namespace SimpleCompiler.Frontends;

public interface IFrontend<TInput>
{
    IrGraph Lower(TInput input);
}
