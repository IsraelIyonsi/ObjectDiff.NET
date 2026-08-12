namespace ObjectDiff.Internal;

internal readonly struct SequenceAlignmentOperation
{
    private SequenceAlignmentOperation(SequenceAlignmentKind kind, int? leftIndex, int? rightIndex)
    {
        Kind = kind;
        LeftIndex = leftIndex;
        RightIndex = rightIndex;
    }

    public SequenceAlignmentKind Kind { get; }

    public int? LeftIndex { get; }

    public int? RightIndex { get; }

    public static SequenceAlignmentOperation Matched(int leftIndex, int rightIndex) =>
        new(SequenceAlignmentKind.Matched, leftIndex, rightIndex);

    public static SequenceAlignmentOperation Removed(int leftIndex) =>
        new(SequenceAlignmentKind.Removed, leftIndex, null);

    public static SequenceAlignmentOperation Added(int rightIndex) =>
        new(SequenceAlignmentKind.Added, null, rightIndex);
}
