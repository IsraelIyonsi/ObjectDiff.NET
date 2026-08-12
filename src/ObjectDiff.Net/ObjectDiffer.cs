using ObjectDiff.Internal;

namespace ObjectDiff;

/// <summary>
/// Compares two object graphs of the same type and produces a flat, typed list of the
/// differences between them.
/// </summary>
/// <remarks>
/// <para>
/// The comparison recurses into plain objects member by member, aligns collections and arrays
/// by content (so an insertion or removal in the middle of a list is reported as a single add
/// or remove, not a chain of positional modifications), walks dictionaries by key, and compares
/// value types and strings directly. Reference cycles in the input graph are detected and never
/// cause unbounded recursion.
/// </para>
/// <para>
/// The comparison is implemented with reflection over public instance properties and fields,
/// so it is not trimmer-safe or Native AOT-safe by default: reflected members can be removed by
/// the trimmer or fail to resolve under full AOT unless preserved.
/// </para>
/// </remarks>
public static class ObjectDiffer
{
    private const int RootDepth = 0;
    private const string RootPath = "";

    /// <summary>
    /// Compares two values of type <typeparamref name="T"/> using the default
    /// <see cref="DiffOptions"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values being compared.</typeparam>
    /// <param name="left">The old value.</param>
    /// <param name="right">The new value.</param>
    /// <returns>The differences found between <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static DiffResult Compare<T>(T? left, T? right) => Compare(left, right, new DiffOptions());

    /// <summary>
    /// Compares two values of type <typeparamref name="T"/> using the supplied
    /// <paramref name="options"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values being compared.</typeparam>
    /// <param name="left">The old value.</param>
    /// <param name="right">The new value.</param>
    /// <param name="options">Options controlling depth, ignore rules and custom comparers.</param>
    /// <returns>The differences found between <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static DiffResult Compare<T>(T? left, T? right, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var context = new DiffContext(options);
        var changes = new List<Change>();
        DiffEngine.CompareValues(left, right, RootPath, RootDepth, context, changes);
        return new DiffResult(changes);
    }
}
