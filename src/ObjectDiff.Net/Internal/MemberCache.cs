using System.Collections.Concurrent;
using System.Reflection;

namespace ObjectDiff.Internal;

internal static class MemberCache
{
    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.Instance;

    private static readonly ConcurrentDictionary<Type, MemberAccessor[]> Cache = new();

    public static MemberAccessor[] GetMembers(Type type) =>
        Cache.GetOrAdd(type, BuildMembers);

    private static MemberAccessor[] BuildMembers(Type type)
    {
        var accessors = new List<MemberAccessor>();

        foreach (var property in type.GetProperties(MemberFlags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var capturedProperty = property;
            accessors.Add(new MemberAccessor(property.Name, instance => capturedProperty.GetValue(instance)));
        }

        foreach (var field in type.GetFields(MemberFlags))
        {
            var capturedField = field;
            accessors.Add(new MemberAccessor(field.Name, instance => capturedField.GetValue(instance)));
        }

        accessors.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return accessors.ToArray();
    }
}
