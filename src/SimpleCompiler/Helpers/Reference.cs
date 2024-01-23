namespace SimpleCompiler.Helpers;

internal interface IReadonlyReference<T>
{
    T Value { get; }

    ref readonly T AsReadonlyRef();
}

internal interface IReference<T> : IReadonlyReference<T>
{
    new T Value { get; set; }
    ref T AsRef();
}

internal sealed class Reference<T>(T initialValue) : IReference<T?>
{
    private T? _value = initialValue;
    public T? Value { get => _value; set => _value = value; }
    public ref readonly T? AsReadonlyRef() => ref _value;
    public ref T? AsRef() => ref _value;
}
