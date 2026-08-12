namespace ObjectDiff.Internal;

internal sealed class KeyOrderComparer : IComparer<object>
{
    public static readonly KeyOrderComparer Instance = new();

    private KeyOrderComparer()
    {
    }

    public int Compare(object? x, object? y)
    {
        if (x is IComparable comparableX && x.GetType() == y?.GetType())
        {
            return comparableX.CompareTo(y);
        }

        return string.CompareOrdinal(ValueText.Raw(x), ValueText.Raw(y));
    }
}
