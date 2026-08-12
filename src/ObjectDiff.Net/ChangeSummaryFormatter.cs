using ObjectDiff.Internal;

namespace ObjectDiff;

/// <summary>
/// Renders <see cref="Change"/> and <see cref="DiffResult"/> values as human-readable,
/// audit-log-suitable text. All values are formatted with <see cref="System.Globalization.CultureInfo.InvariantCulture"/>,
/// regardless of the calling thread's current culture.
/// </summary>
public static class ChangeSummaryFormatter
{
    private const string RootPathDisplay = "(root)";
    private const string AddedLabel = "Added";
    private const string RemovedLabel = "Removed";
    private const string ModifiedLabel = "Modified";
    private const string ModifiedArrow = " -> ";
    private const char LabelSeparator = ' ';
    private const string PathValueSeparator = ": ";

    /// <summary>
    /// Formats every change in <paramref name="result"/> as one line each, joined with
    /// <see cref="Environment.NewLine"/>.
    /// </summary>
    /// <param name="result">The diff result to render.</param>
    /// <returns>A multi-line, audit-log-suitable summary. Empty when there are no changes.</returns>
    public static string Format(DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return string.Join(Environment.NewLine, FormatLines(result));
    }

    /// <summary>
    /// Formats every change in <paramref name="result"/> as an individual line of text.
    /// </summary>
    /// <param name="result">The diff result to render.</param>
    /// <returns>One formatted line per change, in the same order as <see cref="DiffResult.Changes"/>.</returns>
    public static IEnumerable<string> FormatLines(DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        foreach (var change in result.Changes)
        {
            yield return FormatChange(change);
        }
    }

    /// <summary>
    /// Formats a single <see cref="Change"/> as one human-readable line, for example
    /// <c>Modified Address.City: "Lagos" -&gt; "Abuja"</c>.
    /// </summary>
    /// <param name="change">The change to render.</param>
    /// <returns>A one-line, audit-log-suitable rendering of <paramref name="change"/>.</returns>
    public static string FormatChange(Change change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var displayPath = change.Path.Length == 0 ? RootPathDisplay : change.Path;

        return change.Kind switch
        {
            ChangeKind.Added =>
                AddedLabel + LabelSeparator + displayPath + PathValueSeparator + ValueText.Quoted(change.NewValue),
            ChangeKind.Removed =>
                RemovedLabel + LabelSeparator + displayPath + PathValueSeparator + ValueText.Quoted(change.OldValue),
            ChangeKind.Modified =>
                ModifiedLabel + LabelSeparator + displayPath + PathValueSeparator +
                ValueText.Quoted(change.OldValue) + ModifiedArrow + ValueText.Quoted(change.NewValue),
            _ => throw new ArgumentOutOfRangeException(nameof(change), change.Kind, message: null),
        };
    }
}
