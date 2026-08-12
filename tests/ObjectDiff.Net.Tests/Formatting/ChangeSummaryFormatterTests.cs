using System.Globalization;
using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Formatting;

public class ChangeSummaryFormatterTests
{
    [Fact]
    public void Added_change_is_formatted_with_the_added_label_and_new_value()
    {
        var change = new Change("Tags[1]", ChangeKind.Added, null, "legacy");

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Added Tags[1]: \"legacy\"", text);
    }

    [Fact]
    public void Removed_change_is_formatted_with_the_removed_label_and_old_value()
    {
        var change = new Change("Tags[0]", ChangeKind.Removed, "vip", null);

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Removed Tags[0]: \"vip\"", text);
    }

    [Fact]
    public void Modified_change_is_formatted_with_old_and_new_values()
    {
        var change = new Change("Address.City", ChangeKind.Modified, "Lagos", "Abuja");

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Modified Address.City: \"Lagos\" -> \"Abuja\"", text);
    }

    [Fact]
    public void Root_path_is_rendered_with_a_placeholder()
    {
        var change = new Change(string.Empty, ChangeKind.Modified, 1, 2);

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Modified (root): 1 -> 2", text);
    }

    [Fact]
    public void Non_string_values_are_not_quoted()
    {
        var change = new Change("Age", ChangeKind.Modified, 30, 31);

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Modified Age: 30 -> 31", text);
    }

    [Fact]
    public void Null_values_render_as_the_literal_null()
    {
        var change = new Change("Nickname", ChangeKind.Modified, null, "Ace");

        var text = ChangeSummaryFormatter.FormatChange(change);

        Assert.Equal("Modified Nickname: null -> \"Ace\"", text);
    }

    [Fact]
    public void Format_joins_every_change_with_newlines_in_order()
    {
        var left = new Person { Name = "Ada", Age = 30 };
        var right = new Person { Name = "Grace", Age = 31 };

        var result = ObjectDiffer.Compare(left, right);
        var text = ChangeSummaryFormatter.Format(result);

        var expectedLines = result.Changes.Select(ChangeSummaryFormatter.FormatChange);
        Assert.Equal(string.Join(Environment.NewLine, expectedLines), text);
    }

    [Fact]
    public void Empty_diff_formats_to_an_empty_string()
    {
        var result = ObjectDiffer.Compare(new Person { Name = "Ada" }, new Person { Name = "Ada" });

        var text = ChangeSummaryFormatter.Format(result);

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void Value_formatting_is_culture_invariant_regardless_of_current_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var change = new Change("Total", ChangeKind.Modified, 1234.5, 6789.25);

            var text = ChangeSummaryFormatter.FormatChange(change);

            Assert.Equal("Modified Total: 1234.5 -> 6789.25", text);
            Assert.DoesNotContain(',', text);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Format_throws_for_null_result()
    {
        Assert.Throws<ArgumentNullException>(() => ChangeSummaryFormatter.Format(null!));
    }

    [Fact]
    public void Format_change_throws_for_null_change()
    {
        Assert.Throws<ArgumentNullException>(() => ChangeSummaryFormatter.FormatChange(null!));
    }
}
