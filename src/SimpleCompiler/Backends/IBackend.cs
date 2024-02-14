using SimpleCompiler.FileSystem;
using SimpleCompiler.IR;

namespace SimpleCompiler.Backends;

public interface IBackend
{
    Task EmitToDirectory(EmitOptions options, IrGraph ir, IOutputManager output, CancellationToken cancellationToken = default);
}
