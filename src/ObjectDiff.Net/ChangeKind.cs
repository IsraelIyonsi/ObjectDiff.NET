namespace ObjectDiff;

/// <summary>
/// Classifies how a single value changed between the left (old) and right (new) side of a
/// comparison.
/// </summary>
public enum ChangeKind
{
    /// <summary>
    /// The value is present only on the new side: a new collection element, a new dictionary
    /// key, or a member that changed from <see langword="null"/> is never classified as
    /// <see cref="Added"/> (that case is <see cref="Modified"/>); <see cref="Added"/> is used
    /// only where the slot itself did not previously exist, such as a collection index or a
    /// dictionary key.
    /// </summary>
    Added,

    /// <summary>
    /// The value is present only on the old side: a collection element or dictionary key that
    /// no longer exists on the new side.
    /// </summary>
    Removed,

    /// <summary>
    /// The same slot (an object member, or a dictionary key present on both sides) holds a
    /// different value on the new side than on the old side.
    /// </summary>
    Modified,
}
