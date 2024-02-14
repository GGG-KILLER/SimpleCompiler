namespace SimpleCompiler.IR;

public sealed class BranchTarget
{
    private BasicBlock? _lazyBlock = null;

    public BasicBlock Block
    {
        get
        {
            if (_lazyBlock == null)
                throw new InvalidOperationException("Target block has not been defined yet.");

            return _lazyBlock;
        }
    }

    public void SetBlock(BasicBlock block)
    {
        if (Interlocked.CompareExchange(ref _lazyBlock, block, null) != null)
            throw new InvalidOperationException("Target block cannot be set more than once.");
    }
}
