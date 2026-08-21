# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-08-21

### Added

- `DiffOptions.MatchCollectionElementsByKey<T>(Func<T, object?> keySelector)`: opt-in, per-element-type key-based collection matching. When both compared collections have an element type with a registered key selector, elements are paired by key instead of by position, exactly as dictionary entries are. Matched keys recurse into the element diff, producing stable dictionary-style paths such as `Orders["ORD-9"].Total`; keys present on only one side are reported as a whole `Added` or `Removed`; reordering the collection no longer produces spurious changes. Nested keyed collections (a keyed list inside a keyed element) are supported, and existing `MaxDepth`, ignore rules and custom comparers are respected within the recursed element diffs.
- Duplicate keys within one collection (including two `null` keys) abort the comparison with an `ObjectDiffException` rather than silently discarding an element. A `null` element and an element whose selector returns a `null` key are treated identically, as a single null-key slot rendered `[null]` in the path.
- `ObjectDiffException` gained a message-only constructor for comparison failures that have no underlying exception, such as a duplicate key.

### Unchanged

- Collections whose element type has no registered selector keep the existing longest-common-subsequence positional comparison, so behavior with no selector registered is identical to 0.1.0.

## [0.1.0] - 2026-08-12

### Added

- `ObjectDiffer.Compare<T>` static API: compares two objects of the same type and returns a `DiffResult` with `AreEqual` and a flat, ordered `Changes` list.
- `Change` with `Path`, `Kind` (`Added`, `Removed`, `Modified`), `OldValue`, and `NewValue`. Paths are dotted for object members and indexer-qualified for collection and dictionary entries, for example `Orders[2].Total` or `Settings["theme"]`.
- Recursive comparison of nested plain objects (public properties and fields), visited in alphabetical order for deterministic output.
- Collection and array diffing by a longest-common-subsequence alignment: insertions and removals in the middle of a sequence are reported as a single `Added` or `Removed` change instead of a chain of positional `Modified` entries; an element that changes in place at its aligned position recurses into a nested `Modified` change (for example `Orders[1].Total`) instead of a whole-object `Removed`/`Added` pair.
- Set diffing by content (`ISet<T>` / `IReadOnlySet<T>`, covering `HashSet<T>` and `SortedSet<T>`): reordered elements produce no changes.
- Dictionary diffing by key, for types implementing either the non-generic `System.Collections.IDictionary` or the generic `IDictionary<TKey,TValue>` / `IReadOnlyDictionary<TKey,TValue>` interfaces, with keys visited in a deterministic sort order and string keys escaped in the rendered path.
- Direct `Equals`-based comparison for value types and strings.
- Reference-cycle detection so a self-referential object graph completes instead of recursing forever.
- `DiffOptions` with `MaxDepth` (default 64, validated to be at least 1), `IgnoreType`, `IgnoreMember`, and `UseComparer` for registering a custom per-type `IEqualityComparer<T>`.
- `ChangeSummaryFormatter` for rendering a `Change` or `DiffResult` as human-readable, audit-log-suitable text with culture-invariant value formatting.
- `ObjectDiffException`, thrown with the original exception attached when reading a compared object's member fails.
- Zero runtime dependencies; SourceLink (GitHub), deterministic CI builds and `.snupkg` symbol packages.
