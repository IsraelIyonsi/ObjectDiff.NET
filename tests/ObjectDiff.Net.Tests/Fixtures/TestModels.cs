namespace ObjectDiff.Tests.Fixtures;

public sealed class Address
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

public sealed class Person
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public Address? Address { get; set; }

    public List<string> Tags { get; set; } = new();

    public string? Nickname { get; set; }
}

public sealed class Node
{
    public string Name { get; set; } = string.Empty;

    public Node? Next { get; set; }
}

public sealed class Order
{
    public int Id { get; set; }

    public decimal Total { get; set; }
}

public sealed class Container
{
    public List<Order> Orders { get; set; } = new();

    public Dictionary<string, string> Settings { get; set; } = new();

    public Dictionary<int, string> Scores { get; set; } = new();
}

public sealed class KeyedOrder
{
    public string? Ref { get; set; }

    public decimal Total { get; set; }

    public List<KeyedLine> Lines { get; set; } = new();
}

public sealed class KeyedLine
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

public sealed class OrderBook
{
    public List<KeyedOrder> Orders { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}

public readonly struct Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

public abstract class Shape
{
}

public sealed class Circle : Shape
{
    public int Radius { get; set; }
}

public sealed class Square : Shape
{
    public int Side { get; set; }
}

public enum Status
{
    Draft,
    Active,
    Archived,
}

public sealed class WithSecret
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class WithLogger
{
    public string Name { get; set; } = string.Empty;

    public LoggerHandle? Logger { get; set; }
}

public sealed class LoggerHandle
{
    public string Id { get; set; } = string.Empty;
}

public sealed class DeepA
{
    public DeepB? B { get; set; }

    public string Label { get; set; } = string.Empty;
}

public sealed class DeepB
{
    public DeepC? C { get; set; }

    public string Label { get; set; } = string.Empty;
}

public sealed class DeepC
{
    public string Label { get; set; } = string.Empty;
}

public sealed class YieldSequence : IEnumerable<int>
{
    private readonly int[] _values;

    public YieldSequence(params int[] values)
    {
        _values = values;
    }

    public IEnumerator<int> GetEnumerator()
    {
        foreach (var value in _values)
        {
            yield return value;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class WithFaultyProperty
{
    public string Name { get; set; } = string.Empty;

    public string Faulty => throw new InvalidOperationException("boom");
}

/// <summary>
/// A dictionary-like collection that implements only the generic
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> interface, not the non-generic
/// <see cref="System.Collections.IDictionary"/> that <c>Dictionary&lt;TKey,TValue&gt;</c>
/// implements. Mirrors the shape of types such as <c>ImmutableDictionary&lt;TKey,TValue&gt;</c>.
/// </summary>
public sealed class ReadOnlyDictionaryOnly<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _inner;

    public ReadOnlyDictionaryOnly(IDictionary<TKey, TValue> source)
    {
        _inner = new Dictionary<TKey, TValue>(source);
    }

    public TValue this[TKey key] => _inner[key];

    public IEnumerable<TKey> Keys => _inner.Keys;

    public IEnumerable<TValue> Values => _inner.Values;

    public int Count => _inner.Count;

    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value!);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
