using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Collections;

public class DictionaryDiffTests
{
    [Fact]
    public void Equal_dictionaries_produce_no_changes_regardless_of_insertion_order()
    {
        var left = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var right = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Key_only_on_the_right_is_added()
    {
        var left = new Dictionary<string, string> { ["a"] = "1" };
        var right = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"b\"]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal("2", change.NewValue);
    }

    [Fact]
    public void Key_only_on_the_left_is_removed()
    {
        var left = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var right = new Dictionary<string, string> { ["a"] = "1" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"b\"]", change.Path);
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal("2", change.OldValue);
    }

    [Fact]
    public void Value_changed_for_a_shared_key_is_modified()
    {
        var left = new Dictionary<string, string> { ["theme"] = "dark" };
        var right = new Dictionary<string, string> { ["theme"] = "light" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"theme\"]", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal("dark", change.OldValue);
        Assert.Equal("light", change.NewValue);
    }

    [Fact]
    public void Integer_keys_are_rendered_without_quotes()
    {
        var left = new Dictionary<int, string> { [42] = "answer" };
        var right = new Dictionary<int, string> { [42] = "guess" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[42]", change.Path);
    }

    [Fact]
    public void Changes_are_ordered_deterministically_by_key_regardless_of_insertion_order()
    {
        var left = new Dictionary<string, string> { ["gamma"] = "1", ["alpha"] = "1", ["beta"] = "1" };
        var right = new Dictionary<string, string> { ["beta"] = "2", ["gamma"] = "2", ["alpha"] = "2" };

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(new[] { "[\"alpha\"]", "[\"beta\"]", "[\"gamma\"]" }, result.Changes.Select(c => c.Path));
    }

    [Fact]
    public void Dictionary_member_of_a_container_uses_member_prefixed_indexer_path()
    {
        var left = new Container();
        left.Settings["theme"] = "dark";
        var right = new Container();
        right.Settings["theme"] = "light";

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Settings[\"theme\"]", change.Path);
    }

    [Fact]
    public void Nested_object_values_in_a_dictionary_recurse_by_path()
    {
        var left = new Dictionary<string, Address> { ["home"] = new() { City = "Lagos" } };
        var right = new Dictionary<string, Address> { ["home"] = new() { City = "Abuja" } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"home\"].City", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
    }

    [Fact]
    public void A_string_key_containing_a_quote_is_escaped_in_the_rendered_path()
    {
        var left = new Dictionary<string, string> { ["a\"b"] = "1" };
        var right = new Dictionary<string, string> { ["a\"b"] = "2" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"a\\\"b\"]", change.Path);
    }

    [Fact]
    public void A_string_key_containing_a_backslash_is_escaped_in_the_rendered_path()
    {
        var left = new Dictionary<string, string> { ["a\\b"] = "1" };
        var right = new Dictionary<string, string> { ["a\\b"] = "2" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"a\\\\b\"]", change.Path);
    }

    [Fact]
    public void A_type_that_is_dictionary_like_only_through_the_generic_interface_is_compared_by_key()
    {
        var left = new ReadOnlyDictionaryOnly<string, string>(
            new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var right = new ReadOnlyDictionaryOnly<string, string>(
            new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" });

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void A_value_change_on_a_generic_only_dictionary_type_is_reported_by_key_not_by_position()
    {
        var left = new ReadOnlyDictionaryOnly<string, string>(
            new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var right = new ReadOnlyDictionaryOnly<string, string>(
            new Dictionary<string, string> { ["b"] = "9", ["a"] = "1" });

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"b\"]", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal("2", change.OldValue);
        Assert.Equal("9", change.NewValue);
    }
}
