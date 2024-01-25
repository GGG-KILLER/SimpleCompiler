namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeVisitor(typeof(MirNode))]
public abstract partial class MirVisitor
{
}

[Tsu.TreeSourceGen.TreeVisitor(typeof(MirNode))]
public abstract partial class MirVisitor<TReturn>
{
}

public abstract class MirWalker : MirVisitor
{
    protected override void DefaultVisit(MirNode node)
    {
        foreach (var child in node.GetChildren())
        {
            Visit(child);
        }
    }
}
