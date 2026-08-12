namespace ObjectDiff.Internal;

internal sealed class MemberAccessor
{
    private readonly Func<object, object?> _getter;

    public MemberAccessor(string name, Func<object, object?> getter)
    {
        Name = name;
        _getter = getter;
    }

    public string Name { get; }

    public object? GetValue(object instance) => _getter(instance);
}
