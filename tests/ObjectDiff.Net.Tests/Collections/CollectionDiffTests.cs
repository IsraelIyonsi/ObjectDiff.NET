using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Collections;

public class CollectionDiffTests
{
    [Fact]
    public void Equal_lists_produce_no_changes()
    {
        var result = ObjectDiffer.Compare(new List<int> { 1, 2, 3 }, new List<int> { 1, 2, 3 });

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Insertion_in_the_middle_is_reported_as_a_single_add_not_a_chain_of_modifications()
    {
        var left = new List<int> { 1, 2, 3, 4 };
        var right = new List<int> { 1, 2, 99, 3, 4 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[2]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Null(change.OldValue);
        Assert.Equal(99, change.NewValue);
    }

    [Fact]
    public void Removal_from_the_middle_is_reported_as_a_single_remove_not_a_chain_of_modifications()
    {
        var left = new List<int> { 1, 2, 3, 4 };
        var right = new List<int> { 1, 3, 4 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[1]", change.Path);
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal(2, change.OldValue);
        Assert.Null(change.NewValue);
    }

    [Fact]
    public void Appending_an_element_reports_a_single_add_at_the_new_index()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2, 3, 4 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[3]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal(4, change.NewValue);
    }

    [Fact]
    public void Prepending_an_element_reports_a_single_add_at_index_zero()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 0, 1, 2, 3 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[0]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal(0, change.NewValue);
    }

    [Fact]
    public void All_elements_added_when_left_is_empty()
    {
        var left = new List<int>();
        var right = new List<int> { 1, 2 };

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(2, result.Changes.Count);
        Assert.All(result.Changes, c => Assert.Equal(ChangeKind.Added, c.Kind));
        Assert.Equal(new[] { "[0]", "[1]" }, result.Changes.Select(c => c.Path));
    }

    [Fact]
    public void All_elements_removed_when_right_is_empty()
    {
        var left = new List<int> { 1, 2 };
        var right = new List<int>();

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(2, result.Changes.Count);
        Assert.All(result.Changes, c => Assert.Equal(ChangeKind.Removed, c.Kind));
        Assert.Equal(new[] { "[0]", "[1]" }, result.Changes.Select(c => c.Path));
    }

    [Fact]
    public void Arrays_are_diffed_the_same_way_as_lists()
    {
        var left = new[] { 1, 2, 3, 4 };
        var right = new[] { 1, 3, 4 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[1]", change.Path);
        Assert.Equal(ChangeKind.Removed, change.Kind);
    }

    [Fact]
    public void Custom_ienumerable_implementations_are_diffed_generically()
    {
        var left = new YieldSequence(1, 2, 3);
        var right = new YieldSequence(1, 3);

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal(2, change.OldValue);
    }

    [Fact]
    public void A_changed_complex_element_recurses_into_a_nested_modified_change_at_its_aligned_index()
    {
        var left = new List<Order> { new() { Id = 1, Total = 10m }, new() { Id = 2, Total = 20m } };
        var right = new List<Order> { new() { Id = 1, Total = 10m }, new() { Id = 2, Total = 99m } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[1].Total", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(20m, change.OldValue);
        Assert.Equal(99m, change.NewValue);
    }

    [Fact]
    public void A_changed_complex_element_with_multiple_differing_members_reports_each_nested_change()
    {
        var left = new List<Order> { new() { Id = 1, Total = 10m } };
        var right = new List<Order> { new() { Id = 2, Total = 99m } };

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(2, result.Changes.Count);
        Assert.Contains(result.Changes, c => c.Path == "[0].Id" && c.Kind == ChangeKind.Modified);
        Assert.Contains(result.Changes, c => c.Path == "[0].Total" && c.Kind == ChangeKind.Modified);
    }

    [Fact]
    public void An_unbalanced_replace_block_pairs_what_it_can_and_reports_the_rest_as_plain_add_or_remove()
    {
        var left = new List<Order>
        {
            new() { Id = 1, Total = 10m },
            new() { Id = 2, Total = 20m },
            new() { Id = 3, Total = 30m },
        };
        var right = new List<Order>
        {
            new() { Id = 1, Total = 10m },
            new() { Id = 99, Total = 999m },
        };

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(3, result.Changes.Count);
        Assert.Contains(result.Changes, c => c.Path == "[1].Id" && c.Kind == ChangeKind.Modified);
        Assert.Contains(result.Changes, c => c.Path == "[1].Total" && c.Kind == ChangeKind.Modified);
        Assert.Contains(result.Changes, c => c.Path == "[2]" && c.Kind == ChangeKind.Removed && ((Order)c.OldValue!).Id == 3);
    }

    [Fact]
    public void Nested_collection_member_uses_member_prefixed_indexer_path()
    {
        var left = new Person { Name = "Ada", Tags = new List<string> { "vip" } };
        var right = new Person { Name = "Ada", Tags = new List<string> { "vip", "legacy" } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Tags[1]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal("legacy", change.NewValue);
    }

    [Fact]
    public void Null_collection_member_is_a_single_modified_change_not_a_collection_diff()
    {
        var left = new Person { Name = "Ada", Tags = null! };
        var right = new Person { Name = "Ada", Tags = new List<string> { "vip" } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Tags", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
    }
}
