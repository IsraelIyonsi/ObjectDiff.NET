namespace ObjectDiff.Internal;

internal interface IEqualityComparerAdapter
{
    bool AreEqual(object left, object right);
}
