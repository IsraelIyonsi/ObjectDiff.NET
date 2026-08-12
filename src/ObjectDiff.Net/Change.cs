namespace ObjectDiff;

/// <summary>
/// A single detected difference between two compared object graphs.
/// </summary>
public sealed class Change
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> class.
    /// </summary>
    /// <param name="path">The dotted, indexer-qualified path to the changed value.</param>
    /// <param name="kind">How the value changed.</param>
    /// <param name="oldValue">The value on the left (old) side, or <see langword="null"/> when
    /// <paramref name="kind"/> is <see cref="ChangeKind.Added"/>.</param>
    /// <param name="newValue">The value on the right (new) side, or <see langword="null"/> when
    /// <paramref name="kind"/> is <see cref="ChangeKind.Removed"/>.</param>
    public Change(string path, ChangeKind kind, object? oldValue, object? newValue)
    {
        ArgumentNullException.ThrowIfNull(path);

        Path = path;
        Kind = kind;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the dotted path to the changed value, relative to the two objects that were
    /// compared. Object members are separated by <c>.</c>, and collection or dictionary entries
    /// are appended as an indexer segment, for example <c>Orders[2].Total</c> (a member changed
    /// on the list element that aligned to position 2) or <c>Settings["theme"]</c>. The path is
    /// empty when the change is at the root of the comparison (the two top-level values
    /// themselves differ). For a list or array, the index is the position on the old (left) side
    /// that a changed element aligned to, not a stable key, so it can shift when unrelated
    /// insertions or removals happen elsewhere in the same collection.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets how the value at <see cref="Path"/> changed.
    /// </summary>
    public ChangeKind Kind { get; }

    /// <summary>
    /// Gets the value on the left (old) side of the comparison, or <see langword="null"/> when
    /// <see cref="Kind"/> is <see cref="ChangeKind.Added"/>.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the value on the right (new) side of the comparison, or <see langword="null"/> when
    /// <see cref="Kind"/> is <see cref="ChangeKind.Removed"/>.
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Returns the same human-readable summary produced by
    /// <see cref="ChangeSummaryFormatter.FormatChange(Change)"/>.
    /// </summary>
    /// <returns>A one-line, audit-log-suitable rendering of this change.</returns>
    public override string ToString() => ChangeSummaryFormatter.FormatChange(this);
}
