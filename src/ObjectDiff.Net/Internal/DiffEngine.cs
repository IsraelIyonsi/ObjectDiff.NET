using System.Collections;

namespace ObjectDiff.Internal;

internal static class DiffEngine
{
    private const char MemberPathSeparator = '.';
    private const char IndexOpen = '[';
    private const char IndexClose = ']';
    private const string StringQuote = "\"";
    private const string Backslash = "\\";
    private const string EscapedBackslash = "\\\\";
    private const string EscapedQuote = "\\\"";
    private const string EscapedIndexClose = "\\]";
    private const string DuplicateKeyRootLocation = "The root collection";
    private const string DuplicateKeyPathLocationPrefix = "The collection at path '";
    private const string DuplicateKeyPathLocationSuffix = "'";
    private const string DuplicateKeyMessageMiddle = " has more than one element mapping to key ";
    private const string DuplicateKeyMessageSuffix =
        ". Register a key selector that returns a unique key for each element, or remove the duplicate.";

    private static readonly object NullElementKey = new();

    public static void CompareValues(
        object? left,
        object? right,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        if (ReferenceEquals(left, right))
        {
            return;
        }

        if (left is null || right is null)
        {
            var presentType = (left ?? right)!.GetType();
            if (context.Options.IsTypeIgnored(presentType))
            {
                return;
            }

            changes.Add(new Change(path, ChangeKind.Modified, left, right));
            return;
        }

        var leftType = left.GetType();
        if (context.Options.IsTypeIgnored(leftType))
        {
            return;
        }

        var rightType = right.GetType();
        if (leftType != rightType)
        {
            changes.Add(new Change(path, ChangeKind.Modified, left, right));
            return;
        }

        if (context.Options.TryGetComparer(leftType, out var comparer))
        {
            if (!comparer.AreEqual(left, right))
            {
                changes.Add(new Change(path, ChangeKind.Modified, left, right));
            }

            return;
        }

        if (leftType.IsValueType || leftType == typeof(string))
        {
            if (!left.Equals(right))
            {
                changes.Add(new Change(path, ChangeKind.Modified, left, right));
            }

            return;
        }

        if (depth >= context.Options.MaxDepth)
        {
            return;
        }

        var pair = new VisitedPair(left, right);
        if (!context.Ancestors.Add(pair))
        {
            return;
        }

        try
        {
            if (left is IDictionary leftDictionary && right is IDictionary rightDictionary)
            {
                CompareDictionaries(leftDictionary, rightDictionary, path, depth, context, changes);
            }
            else if (left is IEnumerable leftKeyedSequence && right is IEnumerable rightKeyedSequence &&
                     CollectionShape.IsKeyed(leftType))
            {
                CompareKeyedSequences(leftKeyedSequence, rightKeyedSequence, path, depth, context, changes);
            }
            else if (left is IEnumerable leftUnorderedSequence && right is IEnumerable rightUnorderedSequence &&
                     CollectionShape.IsUnordered(leftType))
            {
                CompareUnorderedSequences(leftUnorderedSequence, rightUnorderedSequence, path, depth, context, changes);
            }
            else if (left is IEnumerable leftKeyedElements && right is IEnumerable rightKeyedElements &&
                     TryGetElementKeySelector(leftType, context.Options, out var elementKeySelector))
            {
                CompareKeyedElements(leftKeyedElements, rightKeyedElements, elementKeySelector, path, depth, context, changes);
            }
            else if (left is IEnumerable leftSequence && right is IEnumerable rightSequence)
            {
                CompareSequences(leftSequence, rightSequence, path, depth, context, changes);
            }
            else
            {
                CompareObjects(left, right, leftType, path, depth, context, changes);
            }
        }
        finally
        {
            context.Ancestors.Remove(pair);
        }
    }

    public static bool ValuesAreEqual(object? left, object? right, DiffContext context, int depth)
    {
        var scratch = new List<Change>();
        CompareValues(left, right, string.Empty, depth, context, scratch);
        return scratch.Count == 0;
    }

    private static void CompareObjects(
        object left,
        object right,
        Type type,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        foreach (var member in MemberCache.GetMembers(type))
        {
            if (context.Options.IsMemberIgnored(type, member.Name))
            {
                continue;
            }

            var memberPath = path.Length == 0 ? member.Name : path + MemberPathSeparator + member.Name;
            var leftValue = ReadMember(member, left, type, memberPath);
            var rightValue = ReadMember(member, right, type, memberPath);
            CompareValues(leftValue, rightValue, memberPath, depth + 1, context, changes);
        }
    }

    private static object? ReadMember(MemberAccessor member, object instance, Type declaringType, string memberPath)
    {
        try
        {
            return member.GetValue(instance);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Reflection invocation (PropertyInfo.GetValue) wraps whatever the getter throws in a
            // System.Reflection.TargetInvocationException. Unwrap it so ObjectDiffException.InnerException
            // is the actual exception the getter raised, not a reflection plumbing wrapper around it.
            var underlying = ex is System.Reflection.TargetInvocationException { InnerException: { } inner }
                ? inner
                : ex;

            throw new ObjectDiffException(
                $"Reading member '{member.Name}' on type '{declaringType}' at path '{memberPath}' threw an exception. " +
                "See the inner exception for details.",
                underlying);
        }
    }

    /// <summary>
    /// Aligns two sequences by content and walks the resulting operations. A run of removed
    /// elements immediately adjacent to a run of added elements (a "replace" block, produced
    /// whenever an element at a given position differs from its counterpart with nothing in
    /// between to match against) is paired off positionally: the first removed item with the
    /// first added item, and so on, and each pair is recursed into via <see cref="CompareValues"/>
    /// instead of being reported as an unrelated whole-object removal plus addition. This is what
    /// lets an in-place edit to a complex list element, for example changing one field on a
    /// <c>List&lt;Order&gt;</c> entry, surface as a single <see cref="ChangeKind.Modified"/>
    /// change with a nested path such as <c>Orders[1].Total</c> rather than a
    /// <see cref="ChangeKind.Removed"/>/<see cref="ChangeKind.Added"/> pair rendered via
    /// <see cref="object.ToString"/>. Any leftover items on the longer side of an unbalanced
    /// replace block (more removals than additions, or vice versa) are reported as plain removals
    /// or additions.
    /// </summary>
    private static void CompareSequences(
        IEnumerable left,
        IEnumerable right,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        var leftItems = Materialize(left);
        var rightItems = Materialize(right);
        var elementDepth = depth + 1;
        var alignment = SequenceAligner.Align(leftItems, rightItems, context, elementDepth);

        var pendingRemoved = new List<int>();
        var pendingAdded = new List<int>();

        foreach (var operation in alignment)
        {
            switch (operation.Kind)
            {
                case SequenceAlignmentKind.Removed:
                    pendingRemoved.Add(operation.LeftIndex!.Value);
                    break;
                case SequenceAlignmentKind.Added:
                    pendingAdded.Add(operation.RightIndex!.Value);
                    break;
                case SequenceAlignmentKind.Matched:
                default:
                    FlushReplaceBlock(leftItems, rightItems, path, elementDepth, context, changes, pendingRemoved, pendingAdded);
                    break;
            }
        }

        FlushReplaceBlock(leftItems, rightItems, path, elementDepth, context, changes, pendingRemoved, pendingAdded);
    }

    private static void FlushReplaceBlock(
        IReadOnlyList<object?> leftItems,
        IReadOnlyList<object?> rightItems,
        string path,
        int elementDepth,
        DiffContext context,
        ICollection<Change> changes,
        List<int> pendingRemoved,
        List<int> pendingAdded)
    {
        var pairCount = Math.Min(pendingRemoved.Count, pendingAdded.Count);

        for (var i = 0; i < pairCount; i++)
        {
            var leftIndex = pendingRemoved[i];
            var rightIndex = pendingAdded[i];
            CompareValues(
                leftItems[leftIndex],
                rightItems[rightIndex],
                FormatIndexPath(path, leftIndex),
                elementDepth,
                context,
                changes);
        }

        for (var i = pairCount; i < pendingRemoved.Count; i++)
        {
            var leftIndex = pendingRemoved[i];
            changes.Add(new Change(FormatIndexPath(path, leftIndex), ChangeKind.Removed, leftItems[leftIndex], null));
        }

        for (var i = pairCount; i < pendingAdded.Count; i++)
        {
            var rightIndex = pendingAdded[i];
            changes.Add(new Change(FormatIndexPath(path, rightIndex), ChangeKind.Added, null, rightItems[rightIndex]));
        }

        pendingRemoved.Clear();
        pendingAdded.Clear();
    }

    /// <summary>
    /// Compares a set-like collection (anything implementing <c>ISet&lt;T&gt;</c> or
    /// <c>IReadOnlySet&lt;T&gt;</c>, per <see cref="CollectionShape.IsUnordered"/>) by content
    /// rather than by position: each left element is matched against the first not-yet-matched
    /// right element it is deeply equal to, so two sets with the same members enumerated in a
    /// different order produce no changes. Unmatched left elements are <see cref="ChangeKind.Removed"/>,
    /// unmatched right elements are <see cref="ChangeKind.Added"/>; the index in each one's path is
    /// only that element's position within its own side's enumeration, not a stable identity.
    /// </summary>
    private static void CompareUnorderedSequences(
        IEnumerable left,
        IEnumerable right,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        var leftItems = Materialize(left);
        var rightItems = Materialize(right);
        var elementDepth = depth + 1;
        var matchedRight = new bool[rightItems.Count];

        for (var i = 0; i < leftItems.Count; i++)
        {
            var matched = false;
            for (var j = 0; j < rightItems.Count; j++)
            {
                if (matchedRight[j])
                {
                    continue;
                }

                if (!ValuesAreEqual(leftItems[i], rightItems[j], context, elementDepth))
                {
                    continue;
                }

                matchedRight[j] = true;
                matched = true;
                break;
            }

            if (!matched)
            {
                changes.Add(new Change(FormatIndexPath(path, i), ChangeKind.Removed, leftItems[i], null));
            }
        }

        for (var j = 0; j < rightItems.Count; j++)
        {
            if (!matchedRight[j])
            {
                changes.Add(new Change(FormatIndexPath(path, j), ChangeKind.Added, null, rightItems[j]));
            }
        }
    }

    private static void CompareDictionaries(
        IDictionary left,
        IDictionary right,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        var keys = new HashSet<object>();
        foreach (var key in left.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in right.Keys)
        {
            keys.Add(key);
        }

        var orderedKeys = new List<object>(keys);
        orderedKeys.Sort(KeyOrderComparer.Instance);

        foreach (var key in orderedKeys)
        {
            var inLeft = left.Contains(key);
            var inRight = right.Contains(key);
            var keyPath = FormatKeyPath(path, key);

            if (inLeft && inRight)
            {
                CompareValues(left[key], right[key], keyPath, depth + 1, context, changes);
            }
            else if (inLeft)
            {
                changes.Add(new Change(keyPath, ChangeKind.Removed, left[key], null));
            }
            else
            {
                changes.Add(new Change(keyPath, ChangeKind.Added, null, right[key]));
            }
        }
    }

    /// <summary>
    /// Compares a collection that is dictionary-like only through the generic
    /// <c>IReadOnlyDictionary&lt;TKey,TValue&gt;</c> or <c>IDictionary&lt;TKey,TValue&gt;</c>
    /// interfaces (so it does not implement the non-generic <see cref="IDictionary"/> that
    /// <see cref="CompareDictionaries"/> requires, for example <c>ImmutableDictionary&lt;TKey,TValue&gt;</c>),
    /// by key rather than by enumeration position, using <see cref="KeyValuePairAccessor"/> to
    /// read each enumerated <see cref="KeyValuePair{TKey,TValue}"/> without a generic type
    /// parameter.
    /// </summary>
    private static void CompareKeyedSequences(
        IEnumerable left,
        IEnumerable right,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        var leftEntries = MaterializeKeyValuePairs(left);
        var rightEntries = MaterializeKeyValuePairs(right);

        var keys = new HashSet<object>();
        foreach (var key in leftEntries.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in rightEntries.Keys)
        {
            keys.Add(key);
        }

        var orderedKeys = new List<object>(keys);
        orderedKeys.Sort(KeyOrderComparer.Instance);

        foreach (var key in orderedKeys)
        {
            var inLeft = leftEntries.TryGetValue(key, out var leftValue);
            var inRight = rightEntries.TryGetValue(key, out var rightValue);
            var keyPath = FormatKeyPath(path, key);

            if (inLeft && inRight)
            {
                CompareValues(leftValue, rightValue, keyPath, depth + 1, context, changes);
            }
            else if (inLeft)
            {
                changes.Add(new Change(keyPath, ChangeKind.Removed, leftValue, null));
            }
            else
            {
                changes.Add(new Change(keyPath, ChangeKind.Added, null, rightValue));
            }
        }
    }

    private static bool TryGetElementKeySelector(Type collectionType, DiffOptions options, out Func<object, object?> keySelector)
    {
        if (!options.HasKeySelectors)
        {
            keySelector = null!;
            return false;
        }

        foreach (var elementType in CollectionShape.GetElementTypes(collectionType))
        {
            if (options.TryGetKeySelector(elementType, out keySelector))
            {
                return true;
            }
        }

        keySelector = null!;
        return false;
    }

    /// <summary>
    /// Compares two collections whose element type opted into key-based matching via
    /// <see cref="DiffOptions.MatchCollectionElementsByKey{T}"/>. Elements are indexed by the key the
    /// selector returns and matched by key rather than by position, so a reordered but otherwise
    /// unchanged collection produces no changes. Keys present on only one side are reported as whole
    /// <see cref="ChangeKind.Added"/> or <see cref="ChangeKind.Removed"/> elements; keys present on
    /// both sides recurse via <see cref="CompareValues"/>, producing stable dictionary-style paths
    /// such as <c>Orders["ORD-9"].Total</c>. A <see langword="null"/> element and an element with a
    /// <see langword="null"/> key both map to a single null-key slot; a key that occurs twice on
    /// either side aborts the comparison with an <see cref="ObjectDiffException"/>.
    /// </summary>
    private static void CompareKeyedElements(
        IEnumerable left,
        IEnumerable right,
        Func<object, object?> keySelector,
        string path,
        int depth,
        DiffContext context,
        ICollection<Change> changes)
    {
        var leftByKey = IndexByKey(left, keySelector, path);
        var rightByKey = IndexByKey(right, keySelector, path);

        var keys = new HashSet<object>();
        foreach (var key in leftByKey.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in rightByKey.Keys)
        {
            keys.Add(key);
        }

        var orderedKeys = new List<object>(keys);
        orderedKeys.Sort(KeyOrderComparer.Instance);

        foreach (var normalizedKey in orderedKeys)
        {
            var displayKey = ReferenceEquals(normalizedKey, NullElementKey) ? null : normalizedKey;
            var inLeft = leftByKey.TryGetValue(normalizedKey, out var leftValue);
            var inRight = rightByKey.TryGetValue(normalizedKey, out var rightValue);
            var keyPath = FormatKeyPath(path, displayKey);

            if (inLeft && inRight)
            {
                CompareValues(leftValue, rightValue, keyPath, depth + 1, context, changes);
            }
            else if (inLeft)
            {
                changes.Add(new Change(keyPath, ChangeKind.Removed, leftValue, null));
            }
            else
            {
                changes.Add(new Change(keyPath, ChangeKind.Added, null, rightValue));
            }
        }
    }

    private static Dictionary<object, object?> IndexByKey(
        IEnumerable source,
        Func<object, object?> keySelector,
        string path)
    {
        var map = new Dictionary<object, object?>();
        foreach (var element in source)
        {
            var rawKey = element is null ? null : keySelector(element);
            var normalizedKey = rawKey ?? NullElementKey;
            if (!map.TryAdd(normalizedKey, element))
            {
                throw new ObjectDiffException(BuildDuplicateKeyMessage(path, rawKey));
            }
        }

        return map;
    }

    private static string BuildDuplicateKeyMessage(string path, object? rawKey)
    {
        var location = path.Length == 0
            ? DuplicateKeyRootLocation
            : DuplicateKeyPathLocationPrefix + path + DuplicateKeyPathLocationSuffix;
        return location + DuplicateKeyMessageMiddle + ValueText.Quoted(rawKey) + DuplicateKeyMessageSuffix;
    }

    private static Dictionary<object, object?> MaterializeKeyValuePairs(IEnumerable source)
    {
        var map = new Dictionary<object, object?>();
        foreach (var item in source)
        {
            if (item is null)
            {
                continue;
            }

            var (key, value) = KeyValuePairAccessor.Read(item);
            if (key is not null)
            {
                map[key] = value;
            }
        }

        return map;
    }

    private static List<object?> Materialize(IEnumerable source)
    {
        var list = new List<object?>();
        foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }

    private static string FormatIndexPath(string parent, int index) =>
        parent + IndexOpen + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + IndexClose;

    /// <summary>
    /// Formats a dictionary or keyed-collection entry as an indexer segment appended to
    /// <paramref name="parent"/>. String keys are quoted and have backslashes and embedded quotes
    /// escaped; non-string keys are rendered via their <see cref="ValueText.Raw"/> text with
    /// backslashes and any embedded <c>]</c> escaped, so a key's own text can never be mistaken
    /// for the end of the indexer segment. This keeps the rendered <see cref="Change.Path"/>
    /// unambiguous for a given key's text, though it remains a display string, not a machine key:
    /// an <see cref="ChangeKind.Added"/> path and a <see cref="ChangeKind.Removed"/> path can
    /// still render identically when they address the same position on two different sides of a
    /// sequence (their old and new element are simply different values at that position), so
    /// consumers that need to correlate changes should key by <c>(Path, Kind)</c>, not by
    /// <see cref="Change.Path"/> alone.
    /// </summary>
    private static string FormatKeyPath(string parent, object? key)
    {
        var keyText = key is string text
            ? StringQuote + EscapeQuotedKey(text) + StringQuote
            : EscapeUnquotedKey(ValueText.Raw(key));

        return parent + IndexOpen + keyText + IndexClose;
    }

    private static string EscapeQuotedKey(string text) =>
        text.Replace(Backslash, EscapedBackslash).Replace(StringQuote, EscapedQuote);

    private static string EscapeUnquotedKey(string text) =>
        text.Replace(Backslash, EscapedBackslash).Replace(IndexClose.ToString(), EscapedIndexClose);
}
