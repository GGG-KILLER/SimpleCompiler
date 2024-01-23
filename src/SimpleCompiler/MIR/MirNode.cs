namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public abstract partial class MirNode
{
    private MirNode? _parent = null;
    public MirNode? Parent
    {
        get => _parent;
        internal set
        {
            if (_parent is not null)
                throw new InvalidOperationException($"Cannot set parent more than once. Parent was already set to {_parent}");
            _parent = value;
        }
    }

    internal MirNode()
    {
    }

    public abstract IEnumerable<MirNode> GetChildren();

    public MirNode WithParent(MirNode? parent)
    {
        _parent = parent;
        return this;
    }
}
