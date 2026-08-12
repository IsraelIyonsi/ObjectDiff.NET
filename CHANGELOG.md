# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
