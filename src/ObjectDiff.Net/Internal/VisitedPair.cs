using System.Runtime.CompilerServices;

namespace ObjectDiff.Internal;

internal readonly struct VisitedPair : IEquatable<VisitedPair>
{
    private readonly object _left;
    private readonly object _right;

    public VisitedPair(object left, object right)
    {
        _left = left;
        _right = right;
    }

    public bool Equals(VisitedPair other) =>
        ReferenceEquals(_left, other._left) && ReferenceEquals(_right, other._right);

    public override bool Equals(object? obj) => obj is VisitedPair other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(RuntimeHelpers.GetHashCode(_left), RuntimeHelpers.GetHashCode(_right));
}
