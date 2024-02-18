namespace SimpleCompiler.IR;

public sealed class BranchTarget
{
    private int _lazyBlockOrdinal = -1;

    public BranchTarget()
    {
    }

    public BranchTarget(int basicBlockOrdinal)
    {
        _lazyBlockOrdinal = basicBlockOrdinal;
    }

    public int BlockOrdinal
    {
        get
        {
            if (_lazyBlockOrdinal == -1)
                throw new InvalidOperationException("Target block has not been defined yet.");

            return _lazyBlockOrdinal;
        }
    }

    public void SetBlock(int block)
    {
        if (Interlocked.CompareExchange(ref _lazyBlockOrdinal, block, -1) != -1)
            throw new InvalidOperationException("Target block cannot be set more than once.");
    }
}
