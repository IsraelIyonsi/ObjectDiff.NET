namespace ObjectDiff;

/// <summary>
/// Thrown when <see cref="ObjectDiffer.Compare{T}(T,T)"/> cannot complete a comparison. This
/// happens when reading a member of one of the compared objects throws an exception (the original
/// exception is preserved as <see cref="Exception.InnerException"/>), or when a collection opted
/// into key-based matching via <see cref="DiffOptions.MatchCollectionElementsByKey{T}"/> contains
/// two elements mapping to the same key on one side.
/// </summary>
/// <remarks>
/// ObjectDiff.NET does not silently drop data that would compromise an audit log. It does not
/// suppress exceptions thrown by a property getter: a poisoned getter aborts the whole comparison
/// rather than omitting that member from the result. Likewise, a duplicate key in a key-matched
/// collection aborts the comparison rather than discarding one of the colliding elements, either of
/// which would risk hiding a real difference.
/// </remarks>
public sealed class ObjectDiffException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectDiffException"/> class with a message and
    /// the underlying exception that caused the comparison to fail.
    /// </summary>
    /// <param name="message">A message identifying the member and path where the failure occurred.</param>
    /// <param name="innerException">The exception thrown by the member access.</param>
    public ObjectDiffException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectDiffException"/> class with a message
    /// describing a comparison that could not complete, such as a duplicate key in a key-matched
    /// collection.
    /// </summary>
    /// <param name="message">A message describing why the comparison could not complete.</param>
    public ObjectDiffException(string message)
        : base(message)
    {
    }
}
