using System.Collections.Concurrent;
using System.Reflection;

namespace ObjectDiff.Internal;

/// <summary>
/// Reads the <c>Key</c> and <c>Value</c> of a boxed <see cref="KeyValuePair{TKey,TValue}"/>
/// without knowing <c>TKey</c>/<c>TValue</c> ahead of time, so a type that is dictionary-like
/// only through the generic <c>IReadOnlyDictionary&lt;TKey,TValue&gt;</c> or
/// <c>IDictionary&lt;TKey,TValue&gt;</c> interfaces (and therefore enumerates as
/// <see cref="KeyValuePair{TKey,TValue}"/> through the non-generic
/// <see cref="System.Collections.IEnumerable"/>) can still be compared by key rather than by
/// enumeration position. Accessors are cached per closed generic type.
/// </summary>
internal static class KeyValuePairAccessor
{
    private const string KeyPropertyName = "Key";
    private const string ValuePropertyName = "Value";

    private static readonly ConcurrentDictionary<Type, (Func<object, object?> GetKey, Func<object, object?> GetValue)> Cache = new();

    public static (object? Key, object? Value) Read(object boxedKeyValuePair)
    {
        var accessors = Cache.GetOrAdd(boxedKeyValuePair.GetType(), BuildAccessors);
        return (accessors.GetKey(boxedKeyValuePair), accessors.GetValue(boxedKeyValuePair));
    }

    private static (Func<object, object?>, Func<object, object?>) BuildAccessors(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        var keyProperty = type.GetProperty(KeyPropertyName, flags)
            ?? throw new InvalidOperationException($"Type '{type}' does not expose a public '{KeyPropertyName}' property.");
        var valueProperty = type.GetProperty(ValuePropertyName, flags)
            ?? throw new InvalidOperationException($"Type '{type}' does not expose a public '{ValuePropertyName}' property.");

        return (instance => keyProperty.GetValue(instance), instance => valueProperty.GetValue(instance));
    }
}
