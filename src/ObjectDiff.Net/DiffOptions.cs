using ObjectDiff.Internal;

namespace ObjectDiff;

/// <summary>
/// Configures how <see cref="ObjectDiffer"/> traverses and compares an object graph: how deep
/// it recurses, which types or members it skips, and which types get a custom equality rule
/// instead of the built-in structural comparison.
/// </summary>
public sealed class DiffOptions
{
    /// <summary>
    /// The <see cref="MaxDepth"/> value used when a <see cref="DiffOptions"/> instance is
    /// created without explicitly setting it. Comfortably deeper than any reasonable object
    /// graph while still bounding pathological or accidentally cyclic input.
    /// </summary>
    public const int DefaultMaxDepth = 64;

    /// <summary>
    /// The lowest value <see cref="MaxDepth"/> accepts. A depth of at least 1 is required for
    /// <see cref="ObjectDiffer.Compare{T}(T,T)"/> to ever recurse into a reference-typed root
    /// value; anything lower would silently report two structurally different objects as equal.
    /// </summary>
    public const int MinMaxDepth = 1;

    private readonly HashSet<Type> _ignoredTypes = new();
    private readonly Dictionary<Type, HashSet<string>> _ignoredMembers = new();
    private readonly Dictionary<Type, IEqualityComparerAdapter> _comparers = new();
    private int _maxDepth = DefaultMaxDepth;

    /// <summary>
    /// Gets or sets the maximum number of nested container or object levels traversed below the
    /// root comparison. Differences that would only appear deeper than this are not reported.
    /// Defaults to <see cref="DefaultMaxDepth"/>. Must be at least <see cref="MinMaxDepth"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value assigned is less than <see cref="MinMaxDepth"/>.
    /// </exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < MinMaxDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"{nameof(MaxDepth)} must be at least {MinMaxDepth}; a lower value would silently treat structurally different root objects as equal.");
            }

            _maxDepth = value;
        }
    }

    /// <summary>
    /// Excludes every value whose runtime type is <typeparamref name="T"/>, or a type derived
    /// from or implementing <typeparamref name="T"/>, from comparison, wherever it is
    /// encountered in the object graph.
    /// </summary>
    /// <typeparam name="T">The type (or base type, or interface) to ignore.</typeparam>
    /// <returns>This instance, to allow chained configuration calls.</returns>
    public DiffOptions IgnoreType<T>() => IgnoreType(typeof(T));

    /// <summary>
    /// Excludes every value whose runtime type is <paramref name="type"/>, or a type derived
    /// from or implementing <paramref name="type"/>, from comparison, wherever it is encountered
    /// in the object graph.
    /// </summary>
    /// <param name="type">The type (or base type, or interface) to ignore.</param>
    /// <returns>This instance, to allow chained configuration calls.</returns>
    public DiffOptions IgnoreType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _ignoredTypes.Add(type);
        return this;
    }

    /// <summary>
    /// Excludes a single named member (property or field) declared on <typeparamref name="T"/>,
    /// or on a type derived from it, from comparison.
    /// </summary>
    /// <typeparam name="T">The declaring type of the member to ignore.</typeparam>
    /// <param name="memberName">The property or field name to ignore.</param>
    /// <returns>This instance, to allow chained configuration calls.</returns>
    public DiffOptions IgnoreMember<T>(string memberName) => IgnoreMember(typeof(T), memberName);

    /// <summary>
    /// Excludes a single named member (property or field) declared on <paramref name="declaringType"/>,
    /// or on a type derived from it, from comparison.
    /// </summary>
    /// <param name="declaringType">The declaring type of the member to ignore.</param>
    /// <param name="memberName">The property or field name to ignore.</param>
    /// <returns>This instance, to allow chained configuration calls.</returns>
    public DiffOptions IgnoreMember(Type declaringType, string memberName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        if (!_ignoredMembers.TryGetValue(declaringType, out var members))
        {
            members = new HashSet<string>(StringComparer.Ordinal);
            _ignoredMembers[declaringType] = members;
        }

        members.Add(memberName);
        return this;
    }

    /// <summary>
    /// Registers a custom equality comparer used whenever both sides of a comparison have
    /// runtime type <typeparamref name="T"/>, in place of the library's built-in structural
    /// comparison. When the comparer reports the two values as equal nothing is recorded; when
    /// it reports them unequal a single <see cref="ChangeKind.Modified"/> change is recorded for
    /// the whole value, without recursing into it.
    /// </summary>
    /// <typeparam name="T">The runtime type the comparer applies to.</typeparam>
    /// <param name="comparer">The equality comparer to use for <typeparamref name="T"/>.</param>
    /// <returns>This instance, to allow chained configuration calls.</returns>
    public DiffOptions UseComparer<T>(IEqualityComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        _comparers[typeof(T)] = new ComparerAdapter<T>(comparer);
        return this;
    }

    internal bool IsTypeIgnored(Type runtimeType)
    {
        if (_ignoredTypes.Count == 0)
        {
            return false;
        }

        foreach (var ignored in _ignoredTypes)
        {
            if (ignored.IsAssignableFrom(runtimeType))
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsMemberIgnored(Type declaringType, string memberName)
    {
        if (_ignoredMembers.Count == 0)
        {
            return false;
        }

        for (var current = declaringType; current is not null; current = current.BaseType)
        {
            if (_ignoredMembers.TryGetValue(current, out var members) && members.Contains(memberName))
            {
                return true;
            }
        }

        return false;
    }

    internal bool TryGetComparer(Type runtimeType, out IEqualityComparerAdapter comparer)
    {
        if (_comparers.Count == 0)
        {
            comparer = null!;
            return false;
        }

        return _comparers.TryGetValue(runtimeType, out comparer!);
    }
}
