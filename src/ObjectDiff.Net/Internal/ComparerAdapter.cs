namespace ObjectDiff.Internal;

internal sealed class ComparerAdapter<T> : IEqualityComparerAdapter
{
    private readonly IEqualityComparer<T> _inner;

    public ComparerAdapter(IEqualityComparer<T> inner)
    {
        _inner = inner;
    }

    public bool AreEqual(object left, object right) => _inner.Equals((T)left, (T)right);
}
