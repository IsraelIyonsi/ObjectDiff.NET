# ObjectDiff.NET

Structural object diffing for .NET. Compare two objects and get back a flat, typed list of what changed: added, removed, or modified, each with a dotted path, the old value, and the new value. Built for audit trails and change tracking. Zero external dependencies.

Most "compare two objects" code in production is a hand-rolled pile of `if (a.Name != b.Name)` checks that grows every time a model gains a field, or it reaches for the one well-known library in this space, Compare-NET-Objects, which has gone through long stretches with no releases while issues accumulate. ObjectDiff.NET is a small, focused, actively maintained alternative: it walks the object graph by reflection, handles nested objects, collections, arrays and dictionaries correctly, and gives you a result you can log, assert on, or render for a human.

## Install

```
dotnet add package ObjectDiff.Net
```

## Quickstart

```csharp
using ObjectDiff;

var before = new Customer
{
    Name = "Ada Lovelace",
    Address = new Address { City = "Lagos" },
    Tags = new List<string> { "vip" },
};

var after = new Customer
{
    Name = "Ada Lovelace",
    Address = new Address { City = "Abuja" },
    Tags = new List<string> { "vip", "beta" },
};

DiffResult result = ObjectDiffer.Compare(before, after);

foreach (var change in result.Changes)
{
    Console.WriteLine(change);
}
// Modified Address.City: "Lagos" -> "Abuja"
// Added Tags[1]: "beta"
```

## Writing an audit log entry

```csharp
using ObjectDiff;

DiffResult result = ObjectDiffer.Compare(previousInvoice, updatedInvoice);

if (!result.AreEqual)
{
    string summary = ChangeSummaryFormatter.Format(result);
    auditLog.Record(userId, "invoice.updated", summary);
}
```

`ChangeSummaryFormatter` renders every change as one readable line, for example:

```
Modified Total: 4500.00 -> 4750.00
Added LineItems[2]: LineItem { Sku = "SKU-9", Quantity = 1 }
Removed Recipients[0]: "old@example.com"
```

## Ignoring noisy members and comparing with tolerance

```csharp
using ObjectDiff;

var options = new DiffOptions()
    .IgnoreMember<AuditRecord>(nameof(AuditRecord.LastAccessedAt))
    .IgnoreType<InternalCacheHandle>()
    .UseComparer(new ApproximateDecimalComparer(tolerance: 0.01m));

DiffResult result = ObjectDiffer.Compare(before, after, options);
```

## How comparison works

- **Value types and strings** (`int`, `decimal`, `DateTime`, `Guid`, `enum`, custom structs, `string`, and so on) are compared directly with `Equals`.
- **Plain objects** are compared member by member (public properties and public fields), recursing into each one. Members are visited in alphabetical order, so the resulting change list is deterministic regardless of the runtime's reflection ordering. If a member's getter throws, the comparison stops and an `ObjectDiffException` is thrown identifying the member, its declaring type and the path reached so far, with the getter's own exception as `InnerException`; a poisoned property aborts the diff instead of silently omitting that member.
- **Lists and arrays** are aligned by content using a longest-common-subsequence algorithm, not by naive index-by-index comparison. Inserting or removing one element in the middle of a list produces one `Added` or `Removed` change, not a chain of `Modified` entries for every element after it. There is no key selector: when an element differs from the counterpart it aligns to positionally, the pair is recursed into just like any other value, so changing one field on a complex element, for example a `List<Order>` entry, produces a single `Modified` change with a nested path such as `Orders[1].Total` rather than a whole-object `Removed` plus `Added` rendered via `ToString()`. Only when a run of removals and a run of additions are unbalanced does the excess on the longer side fall back to plain `Removed`/`Added`. The index in a list path is the position an element aligned to, not a stable key, and can shift when unrelated insertions or removals happen elsewhere in the same list; an `Added` path and a `Removed` path can render identically since they each index their own side, so key changes by `(Path, Kind)`, not `Path` alone, if you need to correlate them.
- **Sets** (anything implementing `ISet<T>` or `IReadOnlySet<T>`, which covers `HashSet<T>`, `SortedSet<T>` and their immutable/frozen counterparts) are compared by content, not by enumeration order: each left element is matched against the first not-yet-matched right element it is structurally equal to, so two sets with the same members in a different order produce no changes. Unmatched elements are `Removed` or `Added`.
- **Dictionaries** are compared by key, whether they implement the non-generic `System.Collections.IDictionary` (`Dictionary<TKey,TValue>`, `ConcurrentDictionary<TKey,TValue>`, `SortedDictionary<TKey,TValue>`, `Hashtable`) or only the generic `IDictionary<TKey,TValue>` / `IReadOnlyDictionary<TKey,TValue>` interfaces (for example `ImmutableDictionary<TKey,TValue>`, which does not implement the non-generic interface). Keys present on only one side are `Added` or `Removed`; keys present on both with different values are `Modified`. Keys are visited in a deterministic sort order, not dictionary enumeration order. A string key is rendered quoted in the path, with any backslash or embedded quote escaped; a non-string key's text has any backslash or embedded `]` escaped, so a key's own content can never be mistaken for the end of the indexer segment.
- **Cycles** are detected. If the same pair of references is already being compared higher up the call stack, the comparison stops there instead of recursing forever, so a self-referential graph completes normally.
- **`DiffOptions.MaxDepth`** (default 64, minimum 1; setting it lower throws `ArgumentOutOfRangeException`) bounds how many nested container or object levels are traversed. Differences that would only surface deeper than that are silently not reported, which keeps pathological or accidentally deep graphs from doing unbounded work.

## API surface

| Type | Purpose |
|---|---|
| `ObjectDiffer.Compare<T>(left, right)` / `Compare<T>(left, right, DiffOptions)` | Runs the comparison and returns a `DiffResult`. |
| `DiffResult` | `AreEqual` plus the flat `Changes` list. |
| `Change` | `Path`, `Kind`, `OldValue`, `NewValue` for one difference. |
| `ChangeKind` | `Added`, `Removed`, `Modified`. |
| `DiffOptions` | `MaxDepth`, `IgnoreType`, `IgnoreMember`, `UseComparer`. |
| `ChangeSummaryFormatter` | Renders a `Change` or `DiffResult` as human-readable, audit-log-suitable text. |
| `ObjectDiffException` | Thrown when reading a compared object's member throws; wraps the original exception. |

## Dependencies and AOT

Zero runtime NuGet dependencies. The traversal is reflection-based (public properties and fields, read through `System.Reflection`), which means it is **not** trimmer-safe or Native AOT-safe by default: the trimmer can remove members that are only ever reached reflectively, and full AOT compilation is not guaranteed to preserve arbitrary reflected metadata unless you add explicit trimmer/AOT annotations or root descriptors for the types you diff. If you publish trimmed or NativeAOT, either exclude your model types from trimming or verify each type you compare still round-trips correctly after publish.

## Pairs with

[AuditChain.Net](https://github.com/IsraelIyonsi/AuditChain.NET) for tamper-evident audit trail storage: diff two versions of a record with ObjectDiff.NET, then append the resulting change summary to an AuditChain.Net ledger entry.

## License

MIT. See [LICENSE](LICENSE).
