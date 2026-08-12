namespace ObjectDiff;

/// <summary>
/// The outcome of comparing two object graphs with <see cref="ObjectDiffer"/>.
/// </summary>
public sealed class DiffResult
{
    internal DiffResult(IReadOnlyList<Change> changes)
    {
        Changes = changes;
        AreEqual = changes.Count == 0;
    }

    /// <summary>
    /// Gets a value indicating whether the two compared objects were structurally equal, that
    /// is, whether <see cref="Changes"/> is empty.
    /// </summary>
    public bool AreEqual { get; }

    /// <summary>
    /// Gets the flat list of differences found between the two compared objects, in a stable,
    /// deterministic order. Empty when <see cref="AreEqual"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<Change> Changes { get; }
}
