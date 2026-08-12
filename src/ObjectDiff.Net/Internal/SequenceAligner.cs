namespace ObjectDiff.Internal;

/// <summary>
/// Aligns two sequences using a longest-common-subsequence table so that inserting or removing
/// an element in the middle of a collection is reported as a single add or remove, rather than
/// as a chain of positional modifications to every element that follows.
/// </summary>
internal static class SequenceAligner
{
    public static List<SequenceAlignmentOperation> Align(
        IReadOnlyList<object?> left,
        IReadOnlyList<object?> right,
        DiffContext context,
        int elementDepth)
    {
        var leftCount = left.Count;
        var rightCount = right.Count;
        var lengths = new int[leftCount + 1, rightCount + 1];

        // The DP loop below already evaluates every (i, j) cell's element equality exactly once
        // to fill `lengths`. Cache those results so the traceback pass that follows can reuse
        // them instead of re-running a full deep structural comparison (DiffEngine.ValuesAreEqual)
        // for cells it revisits, which for large or nested element graphs is the dominant cost of
        // aligning two sequences.
        var equalityCache = new bool[leftCount, rightCount];

        for (var i = leftCount - 1; i >= 0; i--)
        {
            for (var j = rightCount - 1; j >= 0; j--)
            {
                var elementsEqual = DiffEngine.ValuesAreEqual(left[i], right[j], context, elementDepth);
                equalityCache[i, j] = elementsEqual;
                lengths[i, j] = elementsEqual
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        var operations = new List<SequenceAlignmentOperation>();
        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < leftCount && rightIndex < rightCount)
        {
            if (equalityCache[leftIndex, rightIndex])
            {
                operations.Add(SequenceAlignmentOperation.Matched(leftIndex, rightIndex));
                leftIndex++;
                rightIndex++;
            }
            else if (lengths[leftIndex + 1, rightIndex] >= lengths[leftIndex, rightIndex + 1])
            {
                operations.Add(SequenceAlignmentOperation.Removed(leftIndex));
                leftIndex++;
            }
            else
            {
                operations.Add(SequenceAlignmentOperation.Added(rightIndex));
                rightIndex++;
            }
        }

        while (leftIndex < leftCount)
        {
            operations.Add(SequenceAlignmentOperation.Removed(leftIndex));
            leftIndex++;
        }

        while (rightIndex < rightCount)
        {
            operations.Add(SequenceAlignmentOperation.Added(rightIndex));
            rightIndex++;
        }

        return operations;
    }
}
