namespace ObjectDiff;

/// <summary>
/// Thrown when <see cref="ObjectDiffer.Compare{T}(T,T)"/> cannot complete a comparison because
/// reading a member of one of the compared objects threw an exception. The original exception is
/// preserved as <see cref="Exception.InnerException"/>.
/// </summary>
/// <remarks>
/// ObjectDiff.NET reads members via reflection and does not suppress exceptions thrown by a
/// property getter: a poisoned getter aborts the whole comparison with this exception rather than
/// silently omitting that member from the result, which would risk hiding a real difference from
/// an audit log.
/// </remarks>
public sealed class ObjectDiffException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectDiffException"/> class.
    /// </summary>
    /// <param name="message">A message identifying the member and path where the failure occurred.</param>
    /// <param name="innerException">The exception thrown by the member access.</param>
    public ObjectDiffException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
