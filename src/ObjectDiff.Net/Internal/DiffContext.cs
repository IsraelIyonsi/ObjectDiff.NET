namespace ObjectDiff.Internal;

internal sealed class DiffContext
{
    public DiffContext(DiffOptions options)
    {
        Options = options;
    }

    public DiffOptions Options { get; }

    public HashSet<VisitedPair> Ancestors { get; } = new();
}
