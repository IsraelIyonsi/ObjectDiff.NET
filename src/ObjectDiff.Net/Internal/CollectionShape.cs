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
    private static readonly ConcurrentDictionary<Type, Type[]> ElementTypesCache = new();

    public static bool IsUnordered(Type type) => UnorderedCache.GetOrAdd(type, ComputeIsUnordered);

    public static bool IsKeyed(Type type) => KeyedCache.GetOrAdd(type, ComputeIsKeyed);

    /// <summary>
    /// Returns every element type <paramref name="type"/> enumerates as, taken from each closed
    /// <c>IEnumerable&lt;T&gt;</c> it implements, so a caller can look up a registered key selector by
    /// the collection's static element type. Results are cached per type.
    /// </summary>
    public static Type[] GetElementTypes(Type type) => ElementTypesCache.GetOrAdd(type, ComputeElementTypes);

    private static Type[] ComputeElementTypes(Type type)
    {
        var elementTypes = new List<Type>();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            elementTypes.Add(type.GetGenericArguments()[0]);
        }

        foreach (var candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementTypes.Add(candidate.GetGenericArguments()[0]);
            }
        }

        return elementTypes.ToArray();
    }

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
