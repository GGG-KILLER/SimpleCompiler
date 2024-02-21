namespace SimpleCompiler.IR;

public abstract class Operand : IEquatable<Operand>
{
    internal Operand()
    {
    }

    public abstract bool Equals(Operand? other);
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public static bool operator ==(Operand left, Operand right) => left.Equals(right);
    public static bool operator !=(Operand left, Operand right) => !(left == right);
}
