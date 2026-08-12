using System.Collections.Concurrent;

namespace ObjectDiff.Internal;

/// <summary>
/// Classifies a runtime collection type as unordered (set-like, where enumeration order carries
/// no meaning and should not affect the diff) or keyed (dictionary-like via the generic
/// <c>IReadOnlyDictionary&lt;TKey,TValue&gt;</c> or <c>IDictionary&lt;TKey,TValue&gt;</c>
/// interfaces, for types that do not implement the non-generic <see cref="System.Collections.IDictionary"/>
/// that <see cref="DiffEngine"/> checks for first). Results are cached per type since
/// <see cref="Type.GetInterfaces"/> is not free and a given type is classified repeatedly across
/// a diff run.
/// </summary>
internal static class CollectionShape
{
    private static readonly ConcurrentDictionary<Type, bool> UnorderedCache = new();
    private static readonly ConcurrentDictionary<Type, bool> KeyedCache = new();

    public static bool IsUnordered(Type type) => UnorderedCache.GetOrAdd(type, ComputeIsUnordered);

    public static bool IsKeyed(Type type) => KeyedCache.GetOrAdd(type, ComputeIsKeyed);

    private static bool ComputeIsUnordered(Type type) =>
        ImplementsGenericInterface(type, typeof(ISet<>)) || ImplementsGenericInterface(type, typeof(IReadOnlySet<>));

    private static bool ComputeIsKeyed(Type type) =>
        ImplementsGenericInterface(type, typeof(IReadOnlyDictionary<,>)) ||
        ImplementsGenericInterface(type, typeof(IDictionary<,>));

    private static bool ImplementsGenericInterface(Type type, Type openGenericInterface)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == openGenericInterface)
        {
            return true;
        }

        foreach (var candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface)
            {
                return true;
            }
        }

        return false;
    }
}
